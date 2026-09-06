import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_appauth/flutter_appauth.dart';

import '../core/errors.dart';
import '../core/models.dart';
import 'providers.dart';

class FuelTrackApp extends StatelessWidget {
  const FuelTrackApp({super.key});
  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'FuelTrack',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF126659)),
      scaffoldBackgroundColor: const Color(0xFFF4F7F6),
      inputDecorationTheme: const InputDecorationTheme(
        border: OutlineInputBorder(),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)),
      ),
    ),
    home: const SessionScreen(),
  );
}

String friendlyError(Object error) => error is ApiFailure
    ? error.message
    : 'No se pudo completar la operación. Inténtalo nuevamente.';

class SessionScreen extends ConsumerStatefulWidget {
  const SessionScreen({super.key});
  @override
  ConsumerState<SessionScreen> createState() => _SessionScreenState();
}

class _SessionScreenState extends ConsumerState<SessionScreen> {
  bool busy = true;
  String? error;
  @override
  void initState() {
    super.initState();
    restore();
  }

  Future<void> restore() async {
    try {
      final session = ref.read(sessionProvider);
      await session.restore();
      await session.accessToken();
      final user = await ref.read(apiProvider).me();
      if (mounted) session.setUser(user);
    } catch (_) {
      /* Primera apertura o sesión no recuperable: mostrar login. */
    }
    if (mounted) setState(() => busy = false);
  }

  Future<void> login() async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final session = ref.read(sessionProvider);
      await session.login();
      final user = await ref.read(apiProvider).me();
      if (mounted) session.setUser(user);
    } on FlutterAppAuthUserCancelledException {
      /* Cancelar vuelve a login. */
    } catch (e) {
      if (mounted) setState(() => error = friendlyError(e));
    }
    if (mounted) setState(() => busy = false);
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(sessionProvider);
    return ListenableBuilder(
      listenable: session,
      builder: (context, _) {
        if (session.user != null) return const HomeScreen();
        return Scaffold(
          body: SafeArea(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(28),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 460),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(
                        Icons.local_gas_station_rounded,
                        size: 64,
                        color: Color(0xFF126659),
                      ),
                      const SizedBox(height: 24),
                      Text(
                        'FuelTrack',
                        style: Theme.of(context).textTheme.displaySmall,
                      ),
                      const SizedBox(height: 12),
                      const Text(
                        'Control de combustible\nTickets y despachos en un solo lugar.',
                      ),
                      const SizedBox(height: 32),
                      if (error != null) ErrorNotice(error!),
                      FilledButton(
                        onPressed: busy ? null : login,
                        child: Text(busy ? 'Conectando…' : 'Iniciar sesión'),
                      ),
                      const SizedBox(height: 12),
                      const Text('Accede con tu cuenta institucional.'),
                    ],
                  ),
                ),
              ),
            ),
          ),
        );
      },
    );
  }
}

class ErrorNotice extends StatelessWidget {
  const ErrorNotice(this.message, {super.key});
  final String message;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 12),
    child: Semantics(
      liveRegion: true,
      child: Text(
        message,
        style: TextStyle(color: Theme.of(context).colorScheme.error),
      ),
    ),
  );
}

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});
  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  int tab = 0;
  @override
  Widget build(BuildContext context) {
    final user = ref.read(sessionProvider).user!;
    return Scaffold(
      appBar: AppBar(
        title: const Text('FuelTrack'),
        actions: [
          IconButton(
            tooltip: 'Cerrar sesión',
            onPressed: () async {
              final messenger = ScaffoldMessenger.of(context);
              try {
                await ref.read(sessionProvider).logout();
              } catch (e) {
                messenger.showSnackBar(
                  SnackBar(content: Text(friendlyError(e))),
                );
              }
            },
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: SafeArea(
        child: tab == 0
            ? ListView(
                padding: const EdgeInsets.all(24),
                children: [
                  Text(
                    'Hola, ${user.name}',
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: 8),
                  Text(user.roles.join(' · ')),
                  const SizedBox(height: 32),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Icon(Icons.verified_user_outlined, size: 40),
                          const SizedBox(height: 16),
                          Text(
                            'Verifica antes de despachar',
                            style: Theme.of(context).textTheme.titleLarge,
                          ),
                          const SizedBox(height: 12),
                          const Text(
                            'Escanea el ticket, confirma la identidad y registra los galones servidos.',
                          ),
                          const SizedBox(height: 20),
                          if (user.canValidate)
                            FilledButton.icon(
                              onPressed: () => setState(() => tab = 1),
                              icon: const Icon(Icons.qr_code_scanner),
                              label: const Text('Escanear QR'),
                            ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  OutlinedButton(
                    onPressed: () => setState(() => tab = 2),
                    child: const Text('Consultar tickets'),
                  ),
                ],
              )
            : tab == 1
            ? const ScanFlow()
            : const TicketList(),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: tab,
        onDestinationSelected: (value) => setState(() => tab = value),
        destinations: [
          const NavigationDestination(
            icon: Icon(Icons.home_outlined),
            label: 'Inicio',
          ),
          NavigationDestination(
            icon: const Icon(Icons.qr_code_scanner),
            label: 'Escanear',
            enabled: user.canValidate,
          ),
          const NavigationDestination(
            icon: Icon(Icons.receipt_long_outlined),
            label: 'Tickets',
          ),
        ],
      ),
    );
  }
}

class TicketDetails extends StatelessWidget {
  const TicketDetails(this.ticket, {super.key});
  final Ticket ticket;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(ticket.code, style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 12),
          Text('Empleado: ${ticket.employee}'),
          Text('Departamento: ${ticket.department}'),
          Text('Vehículo: ${ticket.vehicle}'),
          Text('Combustible: ${ticket.fuel}'),
          Text('Autorizado: ${ticket.quantity} galones'),
          Text('Vence: ${ticket.expires.toLocal()}'),
          Text('Estado: ${ticket.stateLabel}'),
        ],
      ),
    ),
  );
}

class TicketList extends ConsumerStatefulWidget {
  const TicketList({super.key});
  @override
  ConsumerState<TicketList> createState() => _TicketListState();
}

class _TicketListState extends ConsumerState<TicketList> {
  late Future<List<Ticket>> pending;
  @override
  void initState() {
    super.initState();
    pending = ref.read(apiProvider).tickets();
  }

  @override
  Widget build(BuildContext context) => FutureBuilder(
    future: pending,
    builder: (context, snapshot) {
      return ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  'Tus consultas',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ),
              IconButton(
                tooltip: 'Actualizar tickets',
                onPressed: () =>
                    setState(() => pending = ref.read(apiProvider).tickets()),
                icon: const Icon(Icons.refresh),
              ),
            ],
          ),
          if (snapshot.connectionState != ConnectionState.done)
            const Center(child: CircularProgressIndicator())
          else if (snapshot.hasError)
            ErrorNotice(friendlyError(snapshot.error!))
          else if (snapshot.data!.isEmpty)
            const Text('No hay tickets disponibles para tu usuario.')
          else
            ...snapshot.data!.map(TicketDetails.new),
        ],
      );
    },
  );
}

class ScanFlow extends ConsumerStatefulWidget {
  const ScanFlow({super.key});
  @override
  ConsumerState<ScanFlow> createState() => _ScanFlowState();
}

class _ScanFlowState extends ConsumerState<ScanFlow> {
  final gate = ScanGate();
  Ticket? ticket;
  String? error;
  bool busy = false;
  bool proceed = false;
  @override
  Widget build(BuildContext context) {
    if (gate.payload == null) return ref.watch(scannerProvider)(scan);
    if (busy) {
      return const Center(
        child: CircularProgressIndicator(semanticsLabel: 'Validando ticket'),
      );
    }
    if (ticket == null) {
      return Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            ErrorNotice(error ?? 'No se pudo validar el ticket.'),
            TextButton(onPressed: reset, child: const Text('Escanear otro')),
          ],
        ),
      );
    }
    if (!proceed) {
      return ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Text(
            'Ticket válido',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          TicketDetails(ticket!),
          if (ref.read(sessionProvider).user!.canDispatch)
            FilledButton(
              onPressed: () => setState(() => proceed = true),
              child: const Text('Continuar al despacho'),
            ),
          TextButton(onPressed: reset, child: const Text('Escanear otro')),
        ],
      );
    }
    return DispatchForm(
      key: ValueKey(ticket!.id),
      ticket: ticket!,
      payload: gate.payload!,
      onReset: reset,
    );
  }

  Future<void> scan(String payload) async {
    if (!gate.accept(payload)) return;
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final result = await ref.read(apiProvider).validate(payload);
      if (mounted) setState(() => ticket = result);
    } catch (e) {
      if (mounted) setState(() => error = friendlyError(e));
    }
    if (mounted) setState(() => busy = false);
  }

  void reset() => setState(() {
    gate.reset();
    ticket = null;
    proceed = false;
    error = null;
  });
}

class DispatchForm extends ConsumerStatefulWidget {
  const DispatchForm({
    super.key,
    required this.ticket,
    required this.payload,
    required this.onReset,
  });
  final Ticket ticket;
  final String payload;
  final VoidCallback onReset;
  @override
  ConsumerState<DispatchForm> createState() => _DispatchFormState();
}

class _DispatchFormState extends ConsumerState<DispatchForm> {
  final form = GlobalKey<FormState>();
  final gallons = TextEditingController();
  final notes = TextEditingController();
  List<DispatchOption> tanks = [], stations = [];
  int? tank, station;
  bool loading = true, busy = false, confirmed = false, uncertain = false;
  String? error;
  DispatchResult? result;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    if (!ref.read(sessionProvider).user!.canDispatch) {
      setState(() => loading = false);
      return;
    }
    try {
      final api = ref.read(apiProvider);
      final values = await Future.wait([
        api.tanks(widget.ticket.fuelId),
        api.stations(),
      ]);
      if (mounted) {
        setState(() {
          tanks = values[0];
          stations = values[1];
        });
      }
    } catch (e) {
      if (mounted) setState(() => error = friendlyError(e));
    }
    if (mounted) setState(() => loading = false);
  }

  @override
  void dispose() {
    gallons.dispose();
    notes.dispose();
    super.dispose();
  }

  Future<void> submit() async {
    if (busy || uncertain || !confirmed || !form.currentState!.validate()) {
      return;
    }
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final dispatched = await ref
          .read(apiProvider)
          .dispatch(
            widget.ticket,
            widget.payload,
            tank!,
            station!,
            double.parse(gallons.text.trim().replaceAll(',', '.')),
            notes.text,
          );
      if (mounted) setState(() => result = dispatched);
    } catch (e) {
      if (mounted) {
        setState(() {
          error = friendlyError(e);
          uncertain = e is ApiFailure && e.uncertain;
        });
      }
    }
    if (mounted) setState(() => busy = false);
  }

  Future<void> reconcile() async {
    setState(() => busy = true);
    try {
      final found = await ref.read(apiProvider).dispatches(widget.ticket.id);
      if (found.isNotEmpty) {
        if (mounted) setState(() => result = found.first);
      } else {
        await ref.read(apiProvider).validate(widget.payload);
        if (mounted) {
          setState(() {
            uncertain = false;
            error = 'No hay despacho registrado. Verifica los datos antes de confirmar.';
          });
        }
      }
    } catch (e) {
      if (mounted) setState(() => error = friendlyError(e));
    }
    if (mounted) setState(() => busy = false);
  }

  @override
  Widget build(BuildContext context) {
    if (result != null) {
      return ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const Icon(Icons.check_circle, color: Color(0xFF126659), size: 72),
          const SizedBox(height: 20),
          Text(
            'Despacho registrado',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          Text(result!.code),
          Text('Galones servidos: ${result!.gallons}'),
          Text('Estación: ${result!.station}'),
          Text('Tanque: ${result!.tank}'),
          Text('Inventario restante: ${result!.remaining ?? 'No disponible'}'),
          Text('Disponibilidad restante: ${result!.available}'),
          const Text('Ticket Consumido'),
          const SizedBox(height: 24),
          FilledButton(
            onPressed: widget.onReset,
            child: const Text('Finalizar'),
          ),
        ],
      );
    }
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text('Ticket válido', style: Theme.of(context).textTheme.headlineSmall),
        TicketDetails(widget.ticket),
        if (error != null) ErrorNotice(error!),
        if (loading)
          const Center(child: CircularProgressIndicator())
        else if (ref.read(sessionProvider).user!.canDispatch)
          Form(
            key: form,
            child: Column(
              children: [
                const SizedBox(height: 16),
                DropdownButtonFormField<int>(
                  decoration: const InputDecoration(labelText: 'Tanque'),
                  initialValue: tank,
                  items: tanks
                      .map(
                        (t) =>
                            DropdownMenuItem(value: t.id, child: Text(t.label)),
                      )
                      .toList(),
                  onChanged: busy
                      ? null
                      : (value) => setState(() => tank = value),
                  validator: (value) =>
                      value == null ? 'Seleccione un tanque.' : null,
                ),
                const SizedBox(height: 16),
                DropdownButtonFormField<int>(
                  decoration: const InputDecoration(labelText: 'Estación'),
                  initialValue: station,
                  items: stations
                      .map(
                        (s) =>
                            DropdownMenuItem(value: s.id, child: Text(s.label)),
                      )
                      .toList(),
                  onChanged: busy
                      ? null
                      : (value) => setState(() => station = value),
                  validator: (value) =>
                      value == null ? 'Seleccione una estación.' : null,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: gallons,
                  enabled: !busy,
                  decoration: const InputDecoration(
                    labelText: 'Galones servidos',
                  ),
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                  validator: (value) =>
                      validateGallons(value, widget.ticket.quantity),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: notes,
                  enabled: !busy,
                  maxLength: 500,
                  decoration: const InputDecoration(
                    labelText: 'Observaciones (opcional)',
                  ),
                ),
                CheckboxListTile(
                  contentPadding: EdgeInsets.zero,
                  value: confirmed,
                  onChanged: busy
                      ? null
                      : (value) => setState(() => confirmed = value ?? false),
                  title: const Text('Confirmo la identidad y el vehículo.'),
                ),
                const Text(
                  'Un despacho parcial también consume el ticket completo.',
                ),
                const SizedBox(height: 16),
                if (tanks.isEmpty || stations.isEmpty)
                  const ErrorNotice(
                    'No hay tanques o estaciones disponibles. Contacta al administrador.',
                  ),
                if (uncertain)
                  FilledButton(
                    onPressed: busy ? null : reconcile,
                    child: const Text('Consultar resultado'),
                  )
                else
                  FilledButton(
                    onPressed:
                        busy || !confirmed || tanks.isEmpty || stations.isEmpty
                        ? null
                        : submit,
                    child: Text(busy ? 'Registrando…' : 'Confirmar despacho'),
                  ),
              ],
            ),
          ),
        TextButton(
          onPressed: busy ? null : widget.onReset,
          child: const Text('Escanear otro'),
        ),
      ],
    );
  }
}
