import 'package:flutter/foundation.dart';

class AppConfig {
  const AppConfig({
    required this.apiUrl,
    required this.authority,
    this.environment = 'production',
    this.authMode = 'keycloak',
  });
  final String apiUrl, authority, environment, authMode;
  bool get localAuth => authMode == 'local';
  static const clientId = String.fromEnvironment(
    'OIDC_CLIENT_ID',
    defaultValue: 'fueltrack-mobile',
  );
  static const redirectUri = String.fromEnvironment(
    'OIDC_REDIRECT_URI',
    defaultValue: 'fueltrack://callback',
  );
  bool get allowHttp => !kReleaseMode && environment == 'development';
  String get discoveryUrl => '$authority/.well-known/openid-configuration';
  String get storageKey =>
      'fueltrack.session.$environment.$authMode.$apiUrl.$authority';
  void validate() {
    if (clientId != 'fueltrack-mobile' ||
        redirectUri != 'fueltrack://callback') {
      throw const FormatException(
        'El cliente y callback deben coincidir con el registro móvil de Keycloak.',
      );
    }
    if (authMode != 'local' && authMode != 'keycloak') {
      throw const FormatException('AUTH_MODE debe ser local o keycloak.');
    }
    for (final value in [apiUrl, if (!localAuth) authority]) {
      final uri = Uri.tryParse(value);
      if (uri == null ||
          uri.host.isEmpty ||
          uri.userInfo.isNotEmpty ||
          uri.hasQuery ||
          uri.hasFragment ||
          (uri.scheme != 'https' && !(allowHttp && uri.scheme == 'http'))) {
        throw const FormatException(
          'Configure API_BASE_URL y OIDC_AUTHORITY con HTTPS.',
        );
      }
    }
  }

  factory AppConfig.fromEnvironment() => const AppConfig(
    apiUrl: String.fromEnvironment('API_BASE_URL'),
    authority: String.fromEnvironment('OIDC_AUTHORITY'),
    environment: String.fromEnvironment('APP_ENV', defaultValue: 'production'),
    authMode: String.fromEnvironment('AUTH_MODE', defaultValue: 'keycloak'),
  );
}
