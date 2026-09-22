import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../app/providers.dart';
import '../core/errors.dart';
import '../core/models.dart';
import '../theme/app_theme.dart';
import '../widgets/ticket_card.dart';
import 'dispatch_screen.dart';

class ScanScreen extends ConsumerStatefulWidget {
  const ScanScreen({super.key});

  @override
  ConsumerState<ScanScreen> createState() => _ScanScreenState();
}

class _ScanScreenState extends ConsumerState<ScanScreen> {
  final gate = ScanGate();
  Ticket? ticket;
  String? error;
  bool busy = false;
  bool proceed = false;

  @override
  Widget build(BuildContext context) {
    if (gate.payload == null) {
      return Scaffold(
        backgroundColor: Colors.black,
        appBar: AppBar(
          title: const Text('Escanear ticket QR'),
          backgroundColor: Colors.transparent,
          foregroundColor: Colors.white,
          elevation: 0,
          actions: [
            IconButton(
              icon: const Icon(Icons.settings_outlined),
              tooltip: 'Servidor configurado',
              onPressed: _mostrarConfigServidor,
            ),
          ],
        ),
        body: ref.watch(scannerProvider)(scan),
      );
    }

    if (busy) {
      return const Scaffold(
        body: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              CircularProgressIndicator(
                strokeWidth: 3,
                valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
                semanticsLabel: 'Validando ticket',
              ),
              SizedBox(height: 16),
              Text(
                'Validando autenticidad del ticket…',
                style: TextStyle(fontSize: 15, color: AppColors.textSecondary),
              ),
            ],
          ),
        ),
      );
    }

    if (ticket == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Validación de ticket')),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(28),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: const BoxDecoration(
                    color: AppColors.statusExpiredBg,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.warning_amber_rounded,
                    size: 48,
                    color: AppColors.statusExpired,
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  error ?? 'No se pudo validar el ticket.',
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 24),
                FilledButton.icon(
                  onPressed: reset,
                  icon: const Icon(Icons.qr_code_scanner),
                  label: const Text('Escanear otro'),
                ),
              ],
            ),
          ),
        ),
      );
    }

    if (!proceed) {
      final user = ref.read(sessionProvider).user;
      final canDispatch = user != null && user.canDispatch;

      return Scaffold(
        backgroundColor: AppColors.background,
        appBar: AppBar(
          title: const Text('Validación completada'),
          leading: IconButton(
            icon: const Icon(Icons.close_rounded),
            onPressed: reset,
          ),
        ),
        body: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Row(
              children: [
                const Icon(
                  Icons.check_circle_rounded,
                  color: AppColors.statusConsumed,
                  size: 28,
                ),
                const SizedBox(width: 10),
                const Text(
                  'Ticket válido',
                  style: TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.w800,
                    color: AppColors.textPrimary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            TicketCard(ticket: ticket!),
            const SizedBox(height: 20),

            if (canDispatch) ...[
              FilledButton(
                onPressed: () => setState(() => proceed = true),
                child: const Text('Continuar al despacho'),
              ),
              const SizedBox(height: 12),
            ],

            OutlinedButton(
              onPressed: reset,
              child: const Text('Escanear otro'),
            ),
          ],
        ),
      );
    }

    return DispatchFormScreen(
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

  void _mostrarConfigServidor() {
    showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Servidor FuelTrack'),
        content: Text(ref.read(configProvider).apiUrl),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cerrar'),
          ),
        ],
      ),
    );
  }
}
