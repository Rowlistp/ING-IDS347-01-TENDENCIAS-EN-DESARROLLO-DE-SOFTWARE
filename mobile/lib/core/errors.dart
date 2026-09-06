import 'package:dio/dio.dart';

class ApiFailure implements Exception {
  const ApiFailure(this.code, this.message, {this.uncertain = false});
  final String code, message;
  final bool uncertain;
  static ApiFailure fromDio(DioException error) {
    final status = error.response?.statusCode;
    if (status == 401) {
      return const ApiFailure(
        'SESSION_EXPIRED',
        'Tu sesión expiró. Inicia sesión nuevamente.',
      );
    }
    if (status == 403) {
      return const ApiFailure(
        'FORBIDDEN',
        'No tienes permiso para realizar esta operación.',
      );
    }
    final data = error.response?.data;
    if (data is Map && data['code'] is String) {
      return fromCode(data['code'] as String);
    }
    if (status == 400) {
      return const ApiFailure(
        'INVALID_FORM',
        'Revisa los datos del formulario.',
      );
    }
    if (status == 404) {
      return const ApiFailure(
        'NOT_FOUND',
        'No se encontró el recurso solicitado.',
      );
    }
    final uncertain =
        error.requestOptions.method == 'POST' &&
        error.requestOptions.path == '/despachos';
    return ApiFailure(
      'CONNECTION',
      uncertain
          ? 'No se pudo confirmar el resultado. Consulta el despacho antes de volver a intentarlo.'
          : 'No hay conexión disponible. No se puede confirmar el despacho sin conexión.',
      uncertain: uncertain,
    );
  }

  static ApiFailure fromCode(String code) => ApiFailure(
    code,
    const {
          'QR_INVALIDO': 'Código QR inválido.',
          'QR_NO_COINCIDE': 'El QR no corresponde al ticket.',
          'TICKET_VENCIDO': 'Este ticket está vencido.',
          'TICKET_CONSUMIDO': 'Este ticket ya fue utilizado.',
          'TICKET_ANULADO': 'Este ticket fue anulado.',
          'INVENTARIO_INSUFICIENTE': 'No hay combustible suficiente.',
          'COMBUSTIBLE_INCORRECTO':
              'El tanque no corresponde al combustible autorizado.',
          'GALONES_EXCEDEN_AUTORIZACION':
              'La cantidad supera los galones autorizados.',
          'GALONES_INVALIDOS':
              'Indique galones positivos con hasta cuatro decimales.',
          'TANQUE_INACTIVO': 'El tanque seleccionado está inactivo.',
          'ESTACION_INACTIVA': 'La estación está inactiva.',
          'CONCURRENCIA_CONFLICTO': 'Los datos cambiaron. Consulta el estado antes de confirmar nuevamente.',
        }[code] ??
        'No se pudo completar la operación. Consulta el estado y revisa los datos.',
  );
  @override
  String toString() => message;
}
