import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app/app.dart';
import 'core/config.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  try {
    AppConfig.fromEnvironment().validate();
    runApp(const ProviderScope(child: FuelTrackApp()));
  } catch (_) {
    runApp(
      const MaterialApp(
        home: Scaffold(
          body: Center(
            child: Padding(
              padding: EdgeInsets.all(24),
              child: Text(
                'Configuración incompleta. Solicita una versión de FuelTrack configurada para tu entorno.',
              ),
            ),
          ),
        ),
      ),
    );
  }
}
