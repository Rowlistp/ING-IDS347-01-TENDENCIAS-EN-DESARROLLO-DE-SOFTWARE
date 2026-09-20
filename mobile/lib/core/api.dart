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
    final activeBase = session.activeBaseUrl;
    if (activeBase != null && dio.options.baseUrl != activeBase) {
      dio.options.baseUrl = activeBase;
    }
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
          if (_isBypass) {
            throw ApiFailure.fromDio(error);
          }
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

  bool get _isBypass => session.isBypass;

  @override
  Future<LocalUser> me() async {
    if (session.user != null) return session.user!;
    if (_isBypass) {
      return const LocalUser('Despachador Local', ['Despachador', 'Administrador']);
    }
    return LocalUser.fromJson(await request('/auth/me') as Map<String, dynamic>);
  }

  static final Map<String, Ticket> _registry = {
    'TCK-2026-001008': Ticket(
      id: 'a8888888-8888-8888-8888-888888888888',
      code: 'TCK-2026-001008',
      employee: 'Ramón Antonio Gómez',
      department: 'Transporte y Distribución',
      vehicle: 'L345002',
      fuel: 'Gasoil Regular',
      fuelId: 1,
      quantity: 50,
      expires: DateTime.now().add(const Duration(days: 4)),
      state: 'Enviado',
    ),
    'TCK-2026-001009': Ticket(
      id: 'a9999999-9999-9999-9999-999999999999',
      code: 'TCK-2026-001009',
      employee: 'Ana Luisa Rodríguez',
      department: 'Operaciones',
      vehicle: 'G123001',
      fuel: 'Gasoil Óptimo',
      fuelId: 2,
      quantity: 22,
      expires: DateTime.now().add(const Duration(days: 3)),
      state: 'Pendiente',
    ),
    'COM-2026-000001': Ticket(
      id: 'dev-ticket-01',
      code: 'COM-2026-000001',
      employee: 'Carlos Rodríguez',
      department: 'Transporte y Logística',
      vehicle: 'CAM-01 (Toyota Hilux)',
      fuel: 'Gasoil Regular',
      fuelId: 1,
      quantity: 20,
      expires: DateTime.now().add(const Duration(days: 2)),
      state: 'Creado',
    ),
    'COM-2026-000002': Ticket(
      id: 'dev-ticket-02',
      code: 'COM-2026-000002',
      employee: 'Ana Martínez',
      department: 'Operaciones',
      vehicle: 'GEN-02 (Generador Eléctrico)',
      fuel: 'Diésel Óptimo',
      fuelId: 2,
      quantity: 35,
      expires: DateTime.now().add(const Duration(days: 1)),
      state: 'Creado',
    ),
    'COM-2026-000003': Ticket(
      id: 'dev-ticket-03',
      code: 'COM-2026-000003',
      employee: 'Juan Pérez',
      department: 'Seguridad',
      vehicle: 'PAT-04 (Nissan Frontier)',
      fuel: 'Gasolina Regular',
      fuelId: 4,
      quantity: 15,
      expires: DateTime.now().add(const Duration(hours: 4)),
      state: 'ProximoAVencer',
    ),
  };

  @override
  Future<List<Ticket>> tickets() async {
    if (_isBypass) {
      await session.loginWithBackend();
    }
    if (!_isBypass) {
      try {
        final list = (await request('/tickets') as List)
            .map((j) => Ticket.fromJson(j as Map<String, dynamic>))
            .toList();
        for (final t in list) {
          _registry[t.code] = t;
          _registry[t.id] = t;
        }
        return list;
      } catch (_) {
        return _registry.values.toSet().toList();
      }
    }
    return _registry.values.toSet().toList();
  }

  @override
  Future<Ticket> validate(String payload) async {
    final cleanPayload = payload.trim();

    // 1. Si está en bypass, intentar conectar a la API real
    if (_isBypass) {
      await session.loginWithBackend();
    }

    // 2. Validación oficial con el Backend API
    if (!_isBypass) {
      try {
        final data = await request(
          '/tickets/validar',
          data: {'qrPayload': cleanPayload},
        ) as Map<String, dynamic>;
        if (data['valido'] != true) {
          throw ApiFailure.fromCode(data['codigo'] as String? ?? 'QR_INVALIDO');
        }
        final t = Ticket.fromJson(data['ticket'] as Map<String, dynamic>);
        _registry[t.code] = t;
        _registry[t.id] = t;
        return t;
      } catch (e) {
        if (e is ApiFailure && e.code != 'CONNECTION_TIMEOUT' && e.code != 'SESSION_EXPIRED') {
          rethrow;
        }
        // Reintentar login y llamada si fue falla de red o token expirado
        try {
          await session.loginWithBackend();
          if (!_isBypass) {
            final data = await request(
              '/tickets/validar',
              data: {'qrPayload': cleanPayload},
            ) as Map<String, dynamic>;
            if (data['valido'] != true) {
              throw ApiFailure.fromCode(data['codigo'] as String? ?? 'QR_INVALIDO');
            }
            final t = Ticket.fromJson(data['ticket'] as Map<String, dynamic>);
            _registry[t.code] = t;
            _registry[t.id] = t;
            return t;
          }
        } catch (retryError) {
          if (retryError is ApiFailure && retryError.code != 'CONNECTION_TIMEOUT') {
            rethrow;
          }
        }
      }
    }

    // 3. Si no se pudo conectar con el backend real, reportar el error claramente
    final serverUrl = session.activeBaseUrl ?? '10.0.0.11:5298';
    throw ApiFailure(
      'SIN_CONEXION_API',
      'No se pudo conectar al servidor central FuelTrack ($serverUrl).\nVerifica la conexión Wi-Fi o USB en Ajustes.',
    );
  }

  @override
  Future<List<DispatchOption>> tanks(int fuelId) async {
    if (_isBypass) {
      await session.loginWithBackend();
    }
    if (!_isBypass) {
      try {
        final list = (await request('/tanques') as List)
            .where((j) => j['activo'] == true && j['tipoCombustibleId'] == fuelId)
            .map(
              (j) => DispatchOption(
                j['id'] as int,
                '${j['identificacion']} (${j['nivelActual'] ?? 0} gal)',
                fuelId: fuelId,
              ),
            )
            .toList();
        if (list.isNotEmpty) return list;
      } catch (_) {}
    }
    return [
      DispatchOption(1, 'Tanque T-01 (Principal)', fuelId: fuelId),
      DispatchOption(2, 'Tanque T-02 (Reserva)', fuelId: fuelId),
    ];
  }

  @override
  Future<List<DispatchOption>> stations() async {
    if (_isBypass) {
      await session.loginWithBackend();
    }
    if (!_isBypass) {
      try {
        final list = (await request('/estaciones?soloActivas=true') as List)
            .where((j) => j['activo'] == true)
            .map((j) => DispatchOption(j['id'] as int, j['nombre'] as String))
            .toList();
        if (list.isNotEmpty) return list;
      } catch (_) {}
    }
    return const [
      DispatchOption(1, 'Estación Central San Isidro'),
      DispatchOption(2, 'Estación Terminal Haina'),
    ];
  }

  @override
  Future<DispatchResult> dispatch(
    Ticket ticket,
    String payload,
    int tank,
    int station,
    double gallons,
    String notes,
  ) async {
    if (_isBypass) {
      await session.loginWithBackend();
    }

    if (_isBypass) {
      final serverUrl = session.activeBaseUrl ?? '10.0.0.11:5298';
      throw ApiFailure(
        'ERROR_DESPACHO_SIN_CONEXION',
        'No hay conexión con el servidor central ($serverUrl) para procesar el despacho.',
      );
    }

    try {
      final res = await request(
        '/despachos',
        data: {
          'ticketId': ticket.id,
          'qrPayload': payload,
          'tanqueId': tank,
          'estacionId': station,
          'galonesServidos': gallons,
          'observaciones': notes,
        },
      ) as Map<String, dynamic>;
      final result = DispatchResult.fromJson(res);

      final consumedTicket = Ticket(
        id: ticket.id,
        code: ticket.code,
        employee: ticket.employee,
        department: ticket.department,
        vehicle: ticket.vehicle,
        fuel: ticket.fuel,
        fuelId: ticket.fuelId,
        quantity: ticket.quantity,
        expires: ticket.expires,
        state: 'Consumido',
      );
      _registry[ticket.code] = consumedTicket;
      _registry[ticket.id] = consumedTicket;
      return result;
    } catch (e) {
      if (e is ApiFailure) rethrow;
      throw ApiFailure('ERROR_DESPACHO', 'Error al registrar el despacho en el servidor: $e');
    }
  }

  @override
  Future<List<DispatchResult>> dispatches(String ticketId) async {
    if (_isBypass) return [];
    return (await request('/despachos', query: {'ticketId': ticketId}) as List)
        .map((j) => DispatchResult.fromJson(j as Map<String, dynamic>))
        .toList();
  }
}

