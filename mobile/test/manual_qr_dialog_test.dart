import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:fueltrack_mobile/features/scanner.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = false;

  testWidgets(
    'Manual QR closes while scanner is replaced without disposing an active field',
    (tester) async {
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(
            const MethodChannel(
              'dev.steenbakker.mobile_scanner/scanner/method',
            ),
            (call) async {
              if (call.method == 'state') {
                return 1;
              }
              if (call.method == 'start') {
                return {
                  'textureId': 1,
                  'size': {'width': 640.0, 'height': 480.0},
                };
              }
              return null;
            },
          );
      String? payload;
      await tester.pumpWidget(
        MaterialApp(
          home: StatefulBuilder(
            builder: (context, setState) => Scaffold(
              body: payload == null
                  ? CameraScanner(
                      onScan: (value) => setState(() => payload = value),
                    )
                  : const Text('Validación recibida'),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('Ingreso manual del QR firmado'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), 'COM-2026-000001');
      await tester.tap(find.text('Validar'));
      await tester.pumpAndSettle();
      expect(payload, 'COM-2026-000001');
      expect(find.text('Validación recibida'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}
