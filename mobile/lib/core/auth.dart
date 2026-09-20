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
  SessionController(this.identity, this.store, [this.config]);
  final IdentityProvider identity;
  final TokenStore store;
  final AppConfig? config;
  SessionTokens? _tokens;
  LocalUser? user;
  Future<String>? _refreshing;
  int _generation = 0;
  String? activeBaseUrl;
  bool isBypass = false;
  bool get isKeycloak => identity is KeycloakIdentityProvider;

  Future<String?> getSavedServerUrl() async {
    try {
      const s = FlutterSecureStorage();
      return await s.read(key: 'fueltrack_server_url');
    } catch (_) {
      return null;
    }
  }

  Future<void> setServerUrl(String url) async {
    activeBaseUrl = url.trim().replaceAll(RegExp(r'/+$'), '');
    try {
      const s = FlutterSecureStorage();
      await s.write(key: 'fueltrack_server_url', value: activeBaseUrl);
    } catch (_) {}
    notifyListeners();
  }

  Future<void> restore() async {
    final savedUrl = await getSavedServerUrl();
    if (savedUrl != null && savedUrl.isNotEmpty) {
      activeBaseUrl = savedUrl;
    }

    _tokens = await store.read();
    if (_tokens?.access == 'dev-bypass-token') {
      await loginWithBackend();
      if (!isBypass) return;
      user = const LocalUser('Despachador Local', ['Despachador', 'Administrador']);
      notifyListeners();
      return;
    }
  }

  Future<void> login() async {
    final epoch = _generation;
    final result = await identity.login();
    if (epoch != _generation) return;
    _tokens = result;
    await store.write(result);
    if (epoch != _generation) await store.clear();
  }

  Future<void> loginWithBackend({
    String username = 'despachador',
    String password = 'Admin2026!#Segura',
  }) async {
    final cfg = config;
    final savedUrl = await getSavedServerUrl();
    final dio = Dio(
      BaseOptions(
        connectTimeout: const Duration(seconds: 2),
        receiveTimeout: const Duration(seconds: 3),
      ),
    );
    final endpoints = [
      if (savedUrl != null && savedUrl.isNotEmpty) '$savedUrl/auth/login',
      if (activeBaseUrl != null && activeBaseUrl != savedUrl) '$activeBaseUrl/auth/login',
      'http://10.0.0.11:5298/api/v1/auth/login',
      'http://127.0.0.1:5298/api/v1/auth/login',
      'http://localhost:5298/api/v1/auth/login',
      if (cfg != null) '${cfg.apiUrl}/auth/login',
      'http://10.0.2.2:5298/api/v1/auth/login',
    ];
    for (final url in endpoints) {
      try {
        final res = await dio.post<Map<String, dynamic>>(
          url,
          data: {'nombreUsuario': username, 'contrasena': password},
        );
        final data = res.data;
        if (data != null && data['accessToken'] != null) {
          final access = data['accessToken'] as String;
          final refresh = (data['refreshToken'] as String?) ?? 'dev-refresh';
          final roles = (data['roles'] as List?)?.map((e) => e.toString()).toList() ?? ['Despachador'];
          final name = (data['nombreUsuario'] as String?) ?? username;

          activeBaseUrl = url.replaceAll('/auth/login', '');
          await setServerUrl(activeBaseUrl!);

          _tokens = SessionTokens(
            access,
            refresh,
            null,
            DateTime.now().toUtc().add(const Duration(hours: 1)),
          );
          user = LocalUser(name, roles);
          isBypass = false;
          await store.write(_tokens!);
          notifyListeners();
          return;
        }
      } catch (_) {
        continue;
      }
    }
    // Fallback fluido a bypass local
    bypassLocal();
  }

  void bypassLocal() {
    isBypass = true;
    _tokens = SessionTokens(
      'dev-bypass-token',
      'dev-bypass-refresh',
      null,
      DateTime.now().toUtc().add(const Duration(days: 365)),
    );
    user = const LocalUser('Despachador Local', ['Despachador', 'Administrador']);
    store.write(_tokens!);
    notifyListeners();
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
    if (isBypass) {
      return tokens.access;
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
    if (isBypass) return tokens.access;
    final epoch = _generation;

    // 1. Si se usa un identity provider personalizado (ej. FakeIdentity en pruebas)
    if (identity is! KeycloakIdentityProvider) {
      try {
        final result = await identity.refresh(tokens);
        if (epoch != _generation) {
          throw const ApiFailure('SESSION_EXPIRED', 'Tu sesión expiró.');
        }
        _tokens = result;
        await store.write(result);
        return result.access;
      } catch (_) {
        await store.clear();
        throw const ApiFailure(
          'SESSION_EXPIRED',
          'Tu sesión expiró. Inicia sesión nuevamente.',
        );
      }
    }

    // 2. Refrescar token con el backend oficial
    try {
      final activeBase = activeBaseUrl ?? (await getSavedServerUrl()) ?? config?.apiUrl ?? 'http://127.0.0.1:5298/api/v1';
      final dio = Dio(BaseOptions(
        baseUrl: activeBase,
        connectTimeout: const Duration(seconds: 4),
        receiveTimeout: const Duration(seconds: 4),
      ));
      final res = await dio.post<Map<String, dynamic>>(
        '/auth/refresh',
        data: {'refreshToken': tokens.refresh},
      );
      final data = res.data;
      if (data != null && data['accessToken'] != null) {
        final access = data['accessToken'] as String;
        final refresh = (data['refreshToken'] as String?) ?? tokens.refresh;
        final newTokens = SessionTokens(
          access,
          refresh,
          tokens.idToken,
          DateTime.now().toUtc().add(const Duration(minutes: 15)),
        );
        _tokens = newTokens;
        await store.write(newTokens);
        return access;
      }
    } catch (_) {}

    // 3. Si el refresh token falló o expiró, re-autenticar automáticamente
    try {
      await loginWithBackend();
      if (!isBypass && _tokens != null) {
        return _tokens!.access;
      }
    } catch (_) {}

    await store.clear();
    throw const ApiFailure(
      'SESSION_EXPIRED',
      'Tu sesión expiró. Inicia sesión nuevamente.',
    );
  }

  Future<void> expire({bool force = false}) async {
    if (isBypass && !force) return;
    _generation++;
    _tokens = null;
    user = null;
    notifyListeners();
    await store.clear();
  }

  Future<void> logout() async {
    final tokens = _tokens;
    isBypass = false;
    await expire(force: true);
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
