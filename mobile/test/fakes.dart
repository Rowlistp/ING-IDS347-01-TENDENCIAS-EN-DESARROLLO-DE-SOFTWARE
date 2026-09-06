import 'package:fueltrack_mobile/core/api.dart';
import 'package:fueltrack_mobile/core/auth.dart';
import 'package:fueltrack_mobile/core/models.dart';
import 'package:fueltrack_mobile/core/errors.dart';

class MemoryStore implements TokenStore {
  SessionTokens? value;
  @override
  Future<SessionTokens?> read() async => value;
  @override
  Future<void> write(SessionTokens tokens) async {
    value = tokens;
  }

  @override
  Future<void> clear() async {
    value = null;
  }
}

class FakeIdentity implements IdentityProvider {
  int refreshes = 0, logouts = 0;
  bool failRefresh = false;
  @override
  Future<SessionTokens> login() async => SessionTokens(
    'test-access',
    'test-refresh',
    null,
    DateTime.now().toUtc().add(const Duration(hours: 1)),
  );
  @override
  Future<SessionTokens> refresh(SessionTokens tokens) async {
    refreshes++;
    await Future<void>.delayed(const Duration(milliseconds: 2));
    if (failRefresh) throw Exception('test failure');
    return login();
  }

  @override
  Future<void> logout(SessionTokens tokens) async {
    logouts++;
  }
}

final sampleTicket = Ticket(
  id: '7e1f3f2c-357f-44b9-bb67-85bb5ef290dd',
  code: 'COM-2026-000001',
  employee: 'Empleado prueba',
  vehicle: 'TEST-01',
  fuel: 'Diesel',
  fuelId: 3,
  quantity: 10,
  expires: DateTime.utc(2099),
  state: 0,
);

class FakeApi implements FuelTrackApi {
  int validations = 0, dispatchCount = 0;
  ApiFailure? validateError, dispatchError;
  DispatchResult? recorded;
  List<String> roles = ['Despachador'];
  Future<void>? dispatchWait;
  @override
  Future<LocalUser> me() async => LocalUser('Operador prueba', roles);
  @override
  Future<List<Ticket>> tickets() async => [sampleTicket];
  @override
  Future<Ticket> validate(String payload) async {
    validations++;
    if (validateError != null) throw validateError!;
    return sampleTicket;
  }

  @override
  Future<List<DispatchOption>> tanks(int fuelId) async => [
    const DispatchOption(7, 'Tanque A', fuelId: 3),
  ];
  @override
  Future<List<DispatchOption>> stations() async => [
    const DispatchOption(9, 'Estación Central'),
  ];
  @override
  Future<DispatchResult> dispatch(
    Ticket ticket,
    String payload,
    int tank,
    int station,
    double gallons,
    String notes,
  ) async {
    dispatchCount++;
    if (dispatchWait != null) await dispatchWait;
    if (dispatchError != null) throw dispatchError!;
    return recorded = DispatchResult(1, ticket.code, gallons, 100 - gallons);
  }

  @override
  Future<List<DispatchResult>> dispatches(String ticketId) async =>
      recorded == null ? [] : [recorded!];
}
