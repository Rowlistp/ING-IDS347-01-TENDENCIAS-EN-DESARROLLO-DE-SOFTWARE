import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fueltrack_mobile/core/api.dart';
import 'package:fueltrack_mobile/core/auth.dart';
import 'package:fueltrack_mobile/core/config.dart';
import 'package:fueltrack_mobile/core/errors.dart';
import 'fakes.dart';

const config = AppConfig(
  apiUrl: 'https://fuel.example.test/api/v1',
  authority: '',
  authMode: 'local',
);

void main() {
  test(
    'Local login requires explicit credentials and server-provided expiry',
    () async {
      final dio = Dio();
      final requests = <RequestOptions>[];
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (o, h) {
            requests.add(o);
            h.resolve(
              Response(
                requestOptions: o,
                statusCode: 200,
                data: {
                  'accessToken': 'access-test',
                  'refreshToken': 'refresh-test',
                  'accessTokenExpiresAtUtc': '2099-01-01T01:02:03Z',
                },
              ),
            );
          },
        ),
      );
      final provider = LocalIdentityProvider(config, client: dio);
      await expectLater(provider.login(), throwsA(isA<ApiFailure>()));
      expect(requests, isEmpty);
      final session = SessionController(provider, MemoryStore());
      await session.login(username: ' Ana ', password: 'fixture-only');
      expect(requests.single.data, {
        'nombreUsuario': 'Ana',
        'contrasena': 'fixture-only',
      });
      expect(await session.accessToken(), 'access-test');
      expect(session.user, isNull); // El perfil requiere /auth/me.
      await session.logout();
      expect(requests.last.path, '/auth/logout');
      expect(requests.last.data, {'refreshToken': 'refresh-test'});
      session.dispose();
    },
  );

  for (final status in [401, 503]) {
    test(
      'Failed local login $status never creates a privileged local session',
      () async {
        final dio = Dio();
        var calls = 0;
        dio.interceptors.add(
          InterceptorsWrapper(
            onRequest: (o, h) {
              calls++;
              h.reject(
                DioException(
                  requestOptions: o,
                  response: Response(requestOptions: o, statusCode: status),
                ),
              );
            },
          ),
        );
        final store = MemoryStore();
        final session = SessionController(
          LocalIdentityProvider(config, client: dio),
          store,
        );
        await expectLater(
          session.login(username: 'Ana', password: 'fixture-only'),
          throwsA(isA<ApiFailure>()),
        );
        expect(calls, 1);
        expect(store.value, isNull);
        expect(session.user, isNull);
        await expectLater(session.accessToken(), throwsA(isA<ApiFailure>()));
        session.dispose();
      },
    );
  }

  test('Old development bypass sessions are discarded', () async {
    final store = MemoryStore()
      ..value = SessionTokens(
        'dev-bypass-token',
        'dev-refresh',
        null,
        DateTime.utc(2099),
      );
    final session = SessionController(FakeIdentity(), store);
    await session.restore();
    expect(store.value, isNull);
    expect(session.user, isNull);
    await expectLater(session.accessToken(), throwsA(isA<ApiFailure>()));
    session.dispose();
  });

  for (final offline in [false, true]) {
    test(
      'Catalogs ${offline ? 'offline' : 'empty'} never produce invented records',
      () async {
        final session = SessionController(FakeIdentity(), MemoryStore());
        await session.login();
        final dio = Dio();
        dio.interceptors.add(
          InterceptorsWrapper(
            onRequest: (o, h) {
              if (offline) {
                h.reject(
                  DioException(
                    requestOptions: o,
                    type: DioExceptionType.connectionError,
                  ),
                );
              } else {
                h.resolve(
                  Response(requestOptions: o, statusCode: 200, data: []),
                );
              }
            },
          ),
        );
        final api = HttpFuelTrackApi(config, session, client: dio);
        for (final result in [
          () => api.tickets(),
          () => api.tanks(1),
          () => api.stations(),
        ]) {
          if (offline) {
            await expectLater(result(), throwsA(isA<ApiFailure>()));
          } else {
            expect(await result(), isEmpty);
          }
        }
        session.dispose();
      },
    );
  }

  test(
    'Inactive stations and incompatible tanks are filtered from actual API data',
    () async {
      final session = SessionController(FakeIdentity(), MemoryStore());
      await session.login();
      final dio = Dio();
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (o, h) {
            h.resolve(
              Response(
                requestOptions: o,
                statusCode: 200,
                data: o.path.startsWith('/estaciones')
                    ? [
                        {'id': 1, 'nombre': 'Cerrada', 'activo': false},
                        {'id': 8, 'nombre': 'Abierta', 'activo': true},
                      ]
                    : [
                        {
                          'id': 1,
                          'identificacion': 'Otro combustible',
                          'activo': true,
                          'tipoCombustibleId': 2,
                        },
                        {
                          'id': 2,
                          'identificacion': 'Inactivo',
                          'activo': false,
                          'tipoCombustibleId': 1,
                        },
                        {
                          'id': 7,
                          'identificacion': 'Combustible inactivo',
                          'activo': true,
                          'tipoCombustibleActivo': false,
                          'tipoCombustibleId': 1,
                        },
                        {
                          'id': 9,
                          'identificacion': 'Compatible',
                          'activo': true,
                          'tipoCombustibleId': 1,
                        },
                      ],
              ),
            );
          },
        ),
      );
      final api = HttpFuelTrackApi(config, session, client: dio);
      expect((await api.stations()).single.id, 8);
      expect((await api.tanks(1)).single.id, 9);
      session.dispose();
    },
  );

  test('Session storage is separated by API and authentication mode', () {
    config.validate();
    const other = AppConfig(
      apiUrl: 'https://other.example.test/api/v1',
      authority: '',
      authMode: 'local',
    );
    expect(config.storageKey, isNot(other.storageKey));
    expect(
      () => const AppConfig(
        apiUrl: 'http://fuel.example.test',
        authority: '',
        authMode: 'local',
      ).validate(),
      throwsFormatException,
    );
  });
}
