class LocalUser {
  const LocalUser(this.name, this.roles);
  final String name;
  final List<String> roles;
  bool get canDispatch => roles.contains('Despachador');
  bool get canValidate => roles.any(
    [
      'Administrador',
      'Supervisor',
      'Despachador',
      'Auditor',
      'Consulta',
    ].contains,
  );
  factory LocalUser.fromJson(Map<String, dynamic> j) => LocalUser(
    j['nombreUsuario'] as String? ?? 'Usuario',
    List<String>.from(j['roles'] as List),
  );
}

class Ticket {
  const Ticket({
    required this.id,
    required this.code,
    required this.employee,
    this.department = '',
    required this.vehicle,
    required this.fuel,
    required this.fuelId,
    required this.quantity,
    required this.expires,
    required this.state,
  });
  final String id, code, employee, department, vehicle, fuel;
  final int fuelId, state;
  final double quantity;
  final DateTime expires;
  String get stateLabel => const [
    'Creado',
    'Enviado',
    'Pendiente',
    'Próximo a vencer',
    'Vencido',
    'Consumido',
    'Anulado',
  ][state];
  factory Ticket.fromJson(Map<String, dynamic> j) => Ticket(
    id: j['id'] as String,
    code: j['codigo'] as String,
    employee: j['empleadoNombre'] as String,
    department: j['departamentoNombre'] as String? ?? '',
    vehicle: j['vehiculoPlaca'] as String,
    fuel: j['tipoCombustibleNombre'] as String,
    fuelId: j['tipoCombustibleId'] as int,
    quantity: (j['cantidadAutorizada'] as num).toDouble(),
    expires: DateTime.parse(j['fechaVencimiento'] as String),
    state: j['estado'] as int,
  );
}

class DispatchOption {
  const DispatchOption(this.id, this.label, {this.fuelId});
  final int id;
  final String label;
  final int? fuelId;
}

class DispatchResult {
  const DispatchResult(
    this.id,
    this.code,
    this.gallons,
    this.remaining, {
    this.available = 0,
    this.tank = '',
    this.station = '',
  });
  final int id;
  final String code;
  final double gallons;
  final double? remaining;
  final double available;
  final String tank, station;
  factory DispatchResult.fromJson(Map<String, dynamic> j) => DispatchResult(
    j['despachoId'] as int,
    j['codigoTicket'] as String,
    (j['galonesServidos'] as num).toDouble(),
    (j['inventarioRestante'] as num?)?.toDouble(),
    available: (j['disponibilidadRestante'] as num).toDouble(),
    tank: j['tanqueIdentificacion'] as String,
    station: j['estacionNombre'] as String,
  );
}

String? validateGallons(String? text, double authorized) {
  final normalized = (text ?? '').trim().replaceAll(',', '.');
  final value = double.tryParse(normalized);
  if (value == null || !value.isFinite || value <= 0) {
    return 'Indique una cantidad mayor que cero.';
  }
  if (value > authorized) return 'La cantidad supera los galones autorizados.';
  if (!RegExp(r'^\d+(\.\d{1,4})?$').hasMatch(normalized)) {
    return 'Use hasta cuatro decimales.';
  }
  return null;
}

class ScanGate {
  String? payload;
  bool accept(String? value) {
    if (payload != null ||
        value == null ||
        value.trim().isEmpty ||
        value.length > 8192) {
      return false;
    }
    payload = value;
    return true;
  }

  void reset() => payload = null;
}
