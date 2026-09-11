import 'dart:convert';
import 'dart:async';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fueltrack_mobile/app/app.dart';
import 'package:fueltrack_mobile/app/providers.dart';
import 'package:fueltrack_mobile/core/api.dart';
import 'package:fueltrack_mobile/core/auth.dart';
import 'package:fueltrack_mobile/core/config.dart';
import 'package:fueltrack_mobile/core/errors.dart';

import '../test/fakes.dart';

class FixtureIdentity extends FakeIdentity {
  FixtureIdentity(this.access);
  final String access;
  @override
  Future<SessionTokens> login() async => SessionTokens(
    access,
    'fixture-refresh-not-used',
    null,
    DateTime.now().toUtc().add(const Duration(minutes: 10)),
  );
}

// initState also starts requests; keep ALL real IO out of the fake widget clock.
class LiveTestApi extends HttpFuelTrackApi {
  LiveTestApi(super.config, super.session, this.networkZone);
  final Zone networkZone;
  @override
  Future<dynamic> request(
    String path, {
    Map<String, dynamic>? data,
    Map<String, dynamic>? query,
  }) => networkZone.run(() => super.request(path, data: data, query: query));
}

void main() {
  testWidgets(
    'Live Flutter UI to PostgreSQL dispatch; replay rejected; rollback intact',
    (tester) async {
      final fixture = jsonDecode(
        File(const String.fromEnvironment('E2E_FIXTURE')).readAsStringSync(),
      ) as Map<String, dynamic>;
      // Restablece IO real en este test explícito; las suites unitarias siguen aisladas.
      HttpOverrides.global = null;
      final session = SessionController(
        FixtureIdentity(fixture['token'] as String),
        MemoryStore(),
      );
      late Zone networkZone;
      await tester.runAsync(() async {
        networkZone = Zone.current;
      });
      final api = LiveTestApi(
        AppConfig(
          apiUrl: fixture['apiUrl'] as String,
          authority: 'https://unused.example.test',
        ),
        session,
        networkZone,
      );
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            sessionProvider.overrideWithValue(session),
            apiProvider.overrideWithValue(api),
            scannerProvider.overrideWithValue(
              (onScan) => Center(
                child: FilledButton(
                  onPressed: () => onScan(fixture['qrPayload'] as String),
                  child: const Text('QR del ticket emitido'),
                ),
              ),
            ),
          ],
          child: const FuelTrackApp(),
        ),
      );
      await tester.pumpAndSettle();
      await tester.runAsync(() async {
        await tester.tap(find.text('Iniciar sesión'));
        await Future<void>.delayed(const Duration(seconds: 1));
      });
      await tester.pumpAndSettle();
      await tester.runAsync(() async {
        final checked = await api.validate(fixture['qrPayload'] as String);
        expect(
          (await api.tanks(checked.fuelId)).map((t) => t.label),
          contains('Tanque E2E'),
        );
        expect(
          (await api.stations()).map((s) => s.label),
          contains('Estación E2E'),
        );
      });
      await tester.tap(find.text('Escanear QR'));
      await tester.pumpAndSettle();
      await tester.runAsync(() async {
        await tester.tap(find.text('QR del ticket emitido'));
        await Future<void>.delayed(const Duration(seconds: 1));
      });
      await tester.pump();
      await tester.runAsync(
        () => Future<void>.delayed(const Duration(seconds: 1)),
      );
      await tester.pumpAndSettle();
      expect(find.text('Ticket válido'), findsOneWidget);
      await tester.runAsync(() async {
        await tester.tap(find.text('Continuar al despacho'));
        await Future<void>.delayed(const Duration(seconds: 1));
      });
      await tester.pump();
      await tester.runAsync(
        () => Future<void>.delayed(const Duration(seconds: 1)),
      );
      await tester.pumpAndSettle();
      expect(
        find.byType(ErrorNotice),
        findsNothing,
        reason: tester
            .widgetList<ErrorNotice>(find.byType(ErrorNotice))
            .map((e) => e.message)
            .join('; '),
      );
      await tester.tap(find.byType(DropdownButtonFormField<int>).at(0));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Tanque E2E').last);
      await tester.pumpAndSettle();
      await tester.ensureVisible(
        find.byType(DropdownButtonFormField<int>).at(1),
      );
      await tester.tap(find.byType(DropdownButtonFormField<int>).at(1));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Estación E2E').last);
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextFormField).first, '5');
      await tester.ensureVisible(find.byType(CheckboxListTile));
      await tester.tap(find.byType(CheckboxListTile));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Confirmar despacho'));
      await tester.runAsync(() async {
        await tester.tap(find.text('Confirmar despacho'));
        await Future<void>.delayed(const Duration(seconds: 1));
      });
      await tester.pumpAndSettle();
      expect(find.text('Despacho registrado'), findsOneWidget);
      expect(find.text('Ticket Consumido'), findsOneWidget);
      await tester.runAsync(() async {
        await expectLater(
          api.validate(fixture['qrPayload'] as String),
          throwsA(
            isA<ApiFailure>().having((e) => e.code, 'code', 'TICKET_CONSUMIDO'),
          ),
        );
        final rollback = await api.validate(fixture['rollbackQr'] as String);
        await expectLater(
          api.dispatch(
            rollback,
            fixture['rollbackQr'] as String,
            fixture['tanqueId'] as int,
            fixture['estacionId'] as int,
            5,
            'rollback',
          ),
          throwsA(isA<ApiFailure>()),
        );
        expect((await api.validate(fixture['rollbackQr'] as String)).state, 'Creado');
      });
    },
  );
}
