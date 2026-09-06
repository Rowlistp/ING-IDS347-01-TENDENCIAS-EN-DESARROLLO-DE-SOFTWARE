import 'package:dio/dio.dart';

import 'auth.dart';
import 'config.dart';
import 'errors.dart';
import 'models.dart';

abstract interface class FuelTrackApi {
  Future<LocalUser> me();
  Future<List<Ticket>> tickets();
  Future<Ticket> validate(String payload);
  Future<List<DispatchOption>> tanks(int fuelId);
  Future<List<DispatchOption>> stations();
  Future<DispatchResult> dispatch(
    Ticket ticket,
    String payload,
    int tank,
    int station,
    double gallons,
    String notes,
  );
  Future<List<DispatchResult>> dispatches(String ticketId);
}

class HttpFuelTrackApi implements FuelTrackApi {
  HttpFuelTrackApi(AppConfig config, this.session, {Dio? client})
    : dio =
          client ??
          Dio(
            BaseOptions(
              baseUrl: config.apiUrl,
              connectTimeout: const Duration(seconds: 12),
              receiveTimeout: const Duration(seconds: 20),
              sendTimeout: const Duration(seconds: 20),
              followRedirects: false,
            ),
          );
  final SessionController session;
  final Dio dio;
  Future<dynamic> request(
    String path, {
    Map<String, dynamic>? data,
    Map<String, dynamic>? query,
  }) async {
    var token = await session.accessToken();
    for (var attempt = 0; attempt < 2; attempt++) {
      try {
        return (await dio.request<dynamic>(
          path,
          data: data,
          queryParameters: query,
          options: Options(
            method: data == null ? 'GET' : 'POST',
            headers: {'Authorization': 'Bearer $token'},
          ),
        )).data;
      } on DioException catch (error) {
        if (error.response?.statusCode == 401) {
          if (attempt == 0) {
            token = await session.accessToken(rejectedToken: token);
            continue;
          }
          await session.expire();
        }
        throw ApiFailure.fromDio(error);
      }
    }
    throw const ApiFailure('SESSION_EXPIRED', 'Tu sesión expiró.');
  }

  @override
  Future<LocalUser> me() async =>
      LocalUser.fromJson(await request('/auth/me') as Map<String, dynamic>);
  @override
  Future<List<Ticket>> tickets() async => (await request('/tickets') as List)
      .map((j) => Ticket.fromJson(j as Map<String, dynamic>))
      .toList();
  @override
  Future<Ticket> validate(String payload) async {
    final data = await request(
      '/tickets/validar',
      data: {'qrPayload': payload},
    ) as Map<String, dynamic>;
    if (data['valido'] != true) {
      throw ApiFailure.fromCode(data['codigo'] as String);
    }
    return Ticket.fromJson(data['ticket'] as Map<String, dynamic>);
  }

  @override
  Future<List<DispatchOption>> tanks(int fuelId) async =>
      (await request('/tanques') as List)
          .where((j) => j['activo'] == true && j['tipoCombustibleId'] == fuelId)
          .map(
            (j) => DispatchOption(
              j['id'] as int,
              j['identificacion'] as String,
              fuelId: fuelId,
            ),
          )
          .toList();
  @override
  Future<List<DispatchOption>> stations() async =>
      (await request('/estaciones') as List)
          .map((j) => DispatchOption(j['id'] as int, j['nombre'] as String))
          .toList();
  @override
  Future<DispatchResult> dispatch(
    Ticket ticket,
    String payload,
    int tank,
    int station,
    double gallons,
    String notes,
  ) async => DispatchResult.fromJson(
    await request(
      '/despachos',
      data: {
        'ticketId': ticket.id,
        'qrPayload': payload,
        'tanqueId': tank,
        'estacionId': station,
        'galonesServidos': gallons,
        'observaciones': notes,
      },
    ) as Map<String, dynamic>,
  );
  @override
  Future<List<DispatchResult>> dispatches(String ticketId) async =>
      (await request('/despachos', query: {'ticketId': ticketId}) as List)
          .map((j) => DispatchResult.fromJson(j as Map<String, dynamic>))
          .toList();
}
