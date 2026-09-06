import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fueltrack_mobile/app/app.dart';
import 'package:fueltrack_mobile/app/providers.dart';
import 'package:fueltrack_mobile/core/auth.dart';
import 'package:fueltrack_mobile/core/errors.dart';

import 'fakes.dart';

void main() {
  Future<void> boot(WidgetTester tester, FakeApi api) async {
    final session = SessionController(FakeIdentity(), MemoryStore());
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          sessionProvider.overrideWithValue(session),
          apiProvider.overrideWithValue(api),
          scannerProvider.overrideWithValue(
            (onScan) => Center(
              child: FilledButton(
                onPressed: () {
                  onScan('TEST-QR');
                  onScan('TEST-QR');
                },
                child: const Text('Simular QR'),
              ),
            ),
          ),
        ],
        child: const FuelTrackApp(),
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Iniciar sesión'));
    await tester.pumpAndSettle();
  }

  Future<void> scan(WidgetTester tester) async {
    await tester.tap(find.text('Escanear QR'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Simular QR'));
    await tester.pumpAndSettle();
  }

  Future<void> fill(WidgetTester tester, String gallons) async {
    await tester.tap(find.text('Continuar al despacho'));
    await tester.pumpAndSettle();
    await tester.tap(find.byType(DropdownButtonFormField<int>).at(0));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Tanque A').last);
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byType(DropdownButtonFormField<int>).at(1));
    await tester.tap(find.byType(DropdownButtonFormField<int>).at(1));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Estación Central').last);
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextFormField).first, gallons);
    await tester.ensureVisible(find.byType(CheckboxListTile));
    await tester.tap(find.byType(CheckboxListTile));
    await tester.pumpAndSettle();
  }

  testWidgets('Mock login to scan validation dispatch success without camera', (
    tester,
  ) async {
    final api = FakeApi();
    await boot(tester, api);
    await scan(tester);
    expect(api.validations, 1);
    expect(api.dispatchCount, 0);
    await fill(tester, '5');
    await tester.ensureVisible(find.text('Confirmar despacho'));
    await tester.tap(find.text('Confirmar despacho'));
    await tester.pumpAndSettle();
    expect(find.text('Despacho registrado'), findsOneWidget);
    expect(find.text('Ticket Consumido'), findsOneWidget);
    expect(api.dispatchCount, 1);
  });
  testWidgets('Invalid QR never exposes confirm form', (tester) async {
    final api = FakeApi()
      ..validateError = ApiFailure.fromCode('TICKET_VENCIDO');
    await boot(tester, api);
    await scan(tester);
    expect(find.text('Este ticket está vencido.'), findsOneWidget);
    expect(find.text('Confirmar despacho'), findsNothing);
    expect(api.dispatchCount, 0);
  });
  testWidgets('Consulta validates without dispatch permission', (tester) async {
    final api = FakeApi()..roles = ['Consulta'];
    await boot(tester, api);
    await scan(tester);
    expect(find.text('Ticket válido'), findsOneWidget);
    expect(find.text('Continuar al despacho'), findsNothing);
    expect(api.dispatchCount, 0);
  });
  testWidgets('Double confirmation produces only one API request', (
    tester,
  ) async {
    final pending = Completer<void>();
    final api = FakeApi()..dispatchWait = pending.future;
    await boot(tester, api);
    await scan(tester);
    await fill(tester, '5');
    await tester.ensureVisible(find.text('Confirmar despacho'));
    await tester.tap(find.text('Confirmar despacho'));
    await tester.tap(find.text('Confirmar despacho'));
    await tester.pump();
    expect(api.dispatchCount, 1);
    pending.complete();
    await tester.pumpAndSettle();
    expect(find.text('Despacho registrado'), findsOneWidget);
  });
  testWidgets('Form rejects gallons above authorized amount', (tester) async {
    final api = FakeApi();
    await boot(tester, api);
    await scan(tester);
    await fill(tester, '11');
    await tester.ensureVisible(find.text('Confirmar despacho'));
    await tester.tap(find.text('Confirmar despacho'));
    await tester.pumpAndSettle();
    expect(
      find.text('La cantidad supera los galones autorizados.'),
      findsOneWidget,
    );
    expect(api.dispatchCount, 0);
  });
  testWidgets('Network uncertainty requires reconciliation before retry', (
    tester,
  ) async {
    final api = FakeApi()
      ..dispatchError = const ApiFailure(
        'CONNECTION',
        'Consulta el resultado.',
        uncertain: true,
      );
    await boot(tester, api);
    await scan(tester);
    await fill(tester, '5');
    await tester.ensureVisible(find.text('Confirmar despacho'));
    await tester.tap(find.text('Confirmar despacho'));
    await tester.pumpAndSettle();
    expect(find.text('Consultar resultado'), findsOneWidget);
    expect(find.text('Confirmar despacho'), findsNothing);
    expect(api.dispatchCount, 1);
  });
}
