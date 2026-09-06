import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'config.dart';
import 'models.dart';
import 'errors.dart';

class SessionTokens {
  const SessionTokens(this.access, this.refresh, this.idToken, this.expires);
  final String access, refresh;
  final String? idToken;
  final DateTime expires;
  Map<String, dynamic> toJson() => {
    'access': access,
    'refresh': refresh,
    'idToken': idToken,
    'expires': expires.toIso8601String(),
  };
  factory SessionTokens.fromJson(Map<String, dynamic> j) => SessionTokens(
    j['access'] as String,
    j['refresh'] as String,
    j['idToken'] as String?,
    DateTime.parse(j['expires'] as String),
  );
}

abstract interface class TokenStore {
  Future<SessionTokens?> read();
  Future<void> write(SessionTokens tokens);
  Future<void> clear();
}

class SecureTokenStore implements TokenStore {
  SecureTokenStore(this.key);
  final String key;
  final FlutterSecureStorage storage = const FlutterSecureStorage();
  @override
  Future<SessionTokens?> read() async {
    final value = await storage.read(key: key);
    if (value == null) return null;
    try {
      return SessionTokens.fromJson(jsonDecode(value) as Map<String, dynamic>);
    } catch (_) {
      await clear();
      return null;
    }
  }

  @override
  Future<void> write(SessionTokens tokens) =>
      storage.write(key: key, value: jsonEncode(tokens.toJson()));
  @override
  Future<void> clear() => storage.delete(key: key);
}

abstract interface class IdentityProvider {
  Future<SessionTokens> login();
  Future<SessionTokens> refresh(SessionTokens tokens);
  Future<void> logout(SessionTokens tokens);
}

class KeycloakIdentityProvider implements IdentityProvider {
  KeycloakIdentityProvider(this.config);
  final AppConfig config;
  final FlutterAppAuth appAuth = const FlutterAppAuth();
  SessionTokens tokens(TokenResponse response, [SessionTokens? old]) {
    if (response.accessToken == null ||
        (response.refreshToken ?? old?.refresh) == null ||
        response.accessTokenExpirationDateTime == null) {
      throw const ApiFailure(
        'LOGIN_FAILED',
        'No se recibió una sesión válida.',
      );
    }
    return SessionTokens(
      response.accessToken!,
      response.refreshToken ?? old!.refresh,
      response.idToken ?? old?.idToken,
      response.accessTokenExpirationDateTime!.toUtc(),
    );
  }

  @override
  Future<SessionTokens> login() async => tokens(
    await appAuth.authorizeAndExchangeCode(
      AuthorizationTokenRequest(
        AppConfig.clientId,
        AppConfig.redirectUri,
        discoveryUrl: config.discoveryUrl,
        scopes: ['openid', 'profile', 'email'],
        promptValues: ['login'],
        allowInsecureConnections: config.allowHttp,
      ),
    ),
  );
  @override
  Future<SessionTokens> refresh(SessionTokens old) async => tokens(
    await appAuth.token(
      TokenRequest(
        AppConfig.clientId,
        AppConfig.redirectUri,
        discoveryUrl: config.discoveryUrl,
        refreshToken: old.refresh,
        scopes: ['openid', 'profile', 'email'],
        allowInsecureConnections: config.allowHttp,
      ),
    ),
    old,
  );
  @override
  Future<void> logout(SessionTokens tokens) async {
    await Dio(
      BaseOptions(
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 10),
      ),
    ).post(
      '${config.authority}/protocol/openid-connect/logout',
      data: {'client_id': AppConfig.clientId, 'refresh_token': tokens.refresh},
      options: Options(contentType: Headers.formUrlEncodedContentType),
    );
  }
}

class SessionController extends ChangeNotifier {
  SessionController(this.identity, this.store);
  final IdentityProvider identity;
  final TokenStore store;
  SessionTokens? _tokens;
  LocalUser? user;
  Future<String>? _refreshing;
  int _generation = 0;
  Future<void> restore() async {
    _tokens = await store.read();
  }

  Future<void> login() async {
    final epoch = _generation;
    final result = await identity.login();
    if (epoch != _generation) return;
    _tokens = result;
    await store.write(result);
    if (epoch != _generation) await store.clear();
  }

  void setUser(LocalUser value) {
    user = value;
    notifyListeners();
  }

  Future<String> accessToken({String? rejectedToken}) async {
    final tokens = _tokens;
    if (tokens == null) {
      throw const ApiFailure('SESSION_EXPIRED', 'Tu sesión expiró.');
    }
    // A concurrent request may already have replaced the rejected token.
    if ((rejectedToken == null || tokens.access != rejectedToken) &&
        tokens.expires.isAfter(
          DateTime.now().toUtc().add(const Duration(seconds: 30)),
        )) {
      return tokens.access;
    }
    if (_refreshing != null) return _refreshing!;
    final pending = _refresh(tokens);
    _refreshing = pending;
    try {
      return await pending;
    } finally {
      if (identical(_refreshing, pending)) _refreshing = null;
    }
  }

  Future<String> _refresh(SessionTokens tokens) async {
    final epoch = _generation;
    try {
      final result = await identity.refresh(tokens);
      if (epoch != _generation) {
        throw const ApiFailure('SESSION_EXPIRED', 'Tu sesión expiró.');
      }
      _tokens = result;
      await store.write(result);
      if (epoch != _generation) {
        await store.clear();
        throw const ApiFailure('SESSION_EXPIRED', 'Tu sesión expiró.');
      }
      return result.access;
    } catch (_) {
      if (epoch == _generation) await expire();
      throw const ApiFailure(
        'SESSION_EXPIRED',
        'Tu sesión expiró. Inicia sesión nuevamente.',
      );
    }
  }

  Future<void> expire() async {
    _generation++;
    _tokens = null;
    user = null;
    notifyListeners();
    await store.clear();
  }

  Future<void> logout() async {
    final tokens = _tokens;
    await expire();
    if (tokens != null) {
      try {
        await identity.logout(tokens);
      } catch (_) {
        throw const ApiFailure(
          'LOGOUT_OFFLINE',
          'La sesión local se cerró. No se pudo confirmar el cierre remoto.',
        );
      }
    }
  }
}
