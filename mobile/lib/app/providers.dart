import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/config.dart';
import '../core/auth.dart';
import '../core/api.dart';
import '../features/scanner.dart';

final configProvider = Provider<AppConfig>(
  (ref) => AppConfig.fromEnvironment(),
);
final sessionProvider = Provider<SessionController>((ref) {
  final config = ref.watch(configProvider);
  final session = SessionController(
    KeycloakIdentityProvider(config),
    SecureTokenStore(config.storageKey),
  );
  ref.onDispose(session.dispose);
  return session;
});
final apiProvider = Provider<FuelTrackApi>(
  (ref) =>
      HttpFuelTrackApi(ref.watch(configProvider), ref.watch(sessionProvider)),
);
typedef ScannerBuilder = Widget Function(ValueChanged<String> onScan);
final scannerProvider = Provider<ScannerBuilder>(
  (ref) =>
      (onScan) => CameraScanner(onScan: onScan),
);
