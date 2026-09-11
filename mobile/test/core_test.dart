import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fueltrack_mobile/core/api.dart';
import 'package:fueltrack_mobile/core/auth.dart';
import 'package:fueltrack_mobile/core/config.dart';
import 'package:fueltrack_mobile/core/errors.dart';
import 'package:fueltrack_mobile/core/models.dart';

import 'fakes.dart';

void main() {
  test('Dispatch parses the complete Phase 5 response', () {
    final result = DispatchResult.fromJson({
      'despachoId': 7,
      'codigoTicket': 'COM-1',
      'galonesServidos': 2,
      'inventarioRestante': 8,
      'disponibilidadRestante': 7.5,
      'tanqueIdentificacion': 'T1',
      'estacionNombre': 'Central',
    });
    expect(result.id, 7);
    expect(result.remaining, 8);
    expect(result.available, 7.5);
    expect(result.tank, 'T1');
    expect(result.station, 'Central');
  });
  test(
    '401 recovery replays the same request once and preserves a valid session',
    () async {
      final identity = FakeIdentity();
      final session = SessionController(identity, MemoryStore());
      await session.login();
      final dio = Dio();
      var calls = 0;
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (options, handler) {
            calls++;
            if (calls == 1) {
              handler.reject(
                DioException(
                  requestOptions: options,
                  response: Response(requestOptions: options, statusCode: 401),
                ),
              );
            } else {
              handler.resolve(
                Response(
                  requestOptions: options,
                  statusCode: 200,
                  data: {
                    'nombreUsuario': 'Operador',
                    'roles': ['Despachador'],
                  },
                ),
              );
            }
          },
        ),
      );
      final api = HttpFuelTrackApi(
        const AppConfig(
          apiUrl: 'https://example.test',
          authority: 'https://example.test',
        ),
        session,
        client: dio,
      );
      expect((await api.me()).canDispatch, isTrue);
      expect(calls, 2);
      expect(identity.refreshes, 1);
      expect(await session.accessToken(), 'test-access');
      session.dispose();
    },
  );
  test('Dispatch timeout does not retry or refresh automatically', () async {
    final identity = FakeIdentity();
    final session = SessionController(identity, MemoryStore());
    await session.login();
    final dio = Dio();
    var calls = 0;
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          calls++;
          handler.reject(
            DioException(
              requestOptions: options,
              type: DioExceptionType.receiveTimeout,
            ),
          );
        },
      ),
    );
    final api = HttpFuelTrackApi(
      const AppConfig(
        apiUrl: 'https://example.test',
        authority: 'https://example.test',
      ),
      session,
      client: dio,
    );
    await expectLater(
      api.dispatch(sampleTicket, 'QR', 7, 9, 2, ''),
      throwsA(isA<ApiFailure>().having((e) => e.uncertain, 'uncertain', true)),
    );
    expect(calls, 1);
    expect(identity.refreshes, 0);
    session.dispose();
  });
  test('Ticket parses server enum, quantity and UTC expiry', () {
    final ticket = Ticket.fromJson({
      'id': 'ticket',
      'codigo': 'COM-1',
      'empleadoNombre': 'Ana',
      'vehiculoPlaca': 'ABC',
      'tipoCombustibleNombre': 'Diesel',
      'tipoCombustibleId': 1,
      'cantidadAutorizada': 5.25,
      'fechaVencimiento': '2099-01-01T00:00:00Z',
      'estado': 'Consumido',
    });
    expect(ticket.quantity, 5.25);
    expect(ticket.stateLabel, 'Consumido');
    expect(ticket.expires.isUtc, isTrue);
  });
  for (final text in ['0', '-1', '11', 'NaN', 'Infinity', '', '1.00001']) {
    test(
      'Reject gallons $text',
      () => expect(validateGallons(text, 10), isNotNull),
    );
  }
  test('Accept partial and exact authorized gallons', () {
    expect(validateGallons('2,5', 10), isNull);
    expect(validateGallons('10', 10), isNull);
  });
  test('Scanner accepts once until explicit reset', () {
    final gate = ScanGate();
    expect(gate.accept(''), isFalse);
    expect(gate.accept('QR'), isTrue);
    expect(gate.accept('QR'), isFalse);
    expect(gate.accept('OTHER'), isFalse);
    gate.reset();
    expect(gate.accept('OTHER'), isTrue);
  });
  test('Sensitive backend details are not displayed', () {
    final error = DioException(
      requestOptions: RequestOptions(path: '/despachos'),
      response: Response(
        requestOptions: RequestOptions(path: '/despachos'),
        statusCode: 409,
        data: {'code': 'TICKET_CONSUMIDO', 'message': 'SQL secret stack trace'},
      ),
    );
    expect(ApiFailure.fromDio(error).message, 'Este ticket ya fue utilizado.');
  });
  test('Timeout on dispatch is an uncertain result', () {
    final error = DioException(
      requestOptions: RequestOptions(path: '/despachos', method: 'POST'),
      type: DioExceptionType.receiveTimeout,
    );
    expect(ApiFailure.fromDio(error).uncertain, isTrue);
  });
  test('Production rejects HTTP configuration', () {
    expect(
      () => const AppConfig(
        apiUrl: 'http://example.test',
        authority: 'https://example.test',
      ).validate(),
      throwsFormatException,
    );
  });
  test('Refresh is single flight; logout clears storage', () async {
    final identity = FakeIdentity();
    final store = MemoryStore();
    store.value = SessionTokens(
      'expired',
      'refresh-test',
      null,
      DateTime.utc(2000),
    );
    final session = SessionController(identity, store);
    await session.restore();
    await Future.wait(List.generate(5, (_) => session.accessToken()));
    expect(identity.refreshes, 1);
    expect(store.value!.access, 'test-access');
    await session.logout();
    expect(store.value, isNull);
    expect(identity.logouts, 1);
    await expectLater(session.accessToken(), throwsA(isA<ApiFailure>()));
    session.dispose();
  });
  test('Refresh failure clears session', () async {
    final identity = FakeIdentity()..failRefresh = true;
    final store = MemoryStore();
    store.value = SessionTokens(
      'expired',
      'refresh-test',
      null,
      DateTime.utc(2000),
    );
    final session = SessionController(identity, store);
    await session.restore();
    await expectLater(session.accessToken(), throwsA(isA<ApiFailure>()));
    expect(store.value, isNull);
    session.dispose();
  });
  test('Logout during refresh cannot resurrect tokens', () async {
    final store = MemoryStore()
      ..value = SessionTokens('old', 'refresh-test', null, DateTime.utc(2000));
    final session = SessionController(FakeIdentity(), store);
    await session.restore();
    final pending = session.accessToken();
    final check = expectLater(pending, throwsA(isA<ApiFailure>()));
    await session.logout();
    await check;
    expect(store.value, isNull);
    session.dispose();
  });
  test('API 401 refreshes once, replays once, then expires session', () async {
    final identity = FakeIdentity();
    final session = SessionController(identity, MemoryStore());
    await session.login();
    final dio = Dio();
    var requests = 0;
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          requests++;
          handler.reject(
            DioException(
              requestOptions: options,
              response: Response(requestOptions: options, statusCode: 401),
            ),
          );
        },
      ),
    );
    final api = HttpFuelTrackApi(
      const AppConfig(
        apiUrl: 'https://example.test/api/v1',
        authority: 'https://example.test',
      ),
      session,
      client: dio,
    );
    await expectLater(
      api.dispatch(sampleTicket, 'QR', 1, 1, 2, ''),
      throwsA(isA<ApiFailure>()),
    );
    expect(requests, 2);
    expect(identity.refreshes, 1);
    await expectLater(session.accessToken(), throwsA(isA<ApiFailure>()));
    session.dispose();
  });
}
