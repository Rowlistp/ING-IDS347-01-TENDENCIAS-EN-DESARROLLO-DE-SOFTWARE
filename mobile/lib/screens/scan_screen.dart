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
    final session = ref.watch(sessionProvider);
    final isConnected = !session.isBypass && session.user != null;
    final activeUrl = session.activeBaseUrl ?? 'http://10.0.0.11:5298/api/v1';

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
              tooltip: 'Configurar Servidor',
              onPressed: _mostrarConfigServidor,
            ),
          ],
        ),
        body: Stack(
          children: [
            ref.watch(scannerProvider)(scan),
            Positioned(
              top: 12,
              left: 16,
              right: 16,
              child: Material(
                color: Colors.transparent,
                child: InkWell(
                  onTap: _mostrarConfigServidor,
                  borderRadius: BorderRadius.circular(8),
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    decoration: BoxDecoration(
                      color: isConnected ? const Color(0xDD064E3B) : const Color(0xDD7C2D12),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(
                        color: isConnected ? const Color(0xFF10B981) : const Color(0xFFF97316),
                        width: 1,
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.35),
                          blurRadius: 8,
                          offset: const Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Row(
                      children: [
                        Icon(
                          isConnected ? Icons.wifi_rounded : Icons.wifi_off_rounded,
                          color: isConnected ? const Color(0xFF34D399) : const Color(0xFFFDBA74),
                          size: 18,
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(
                                isConnected ? 'Conectado a FuelTrack API' : 'Sin conexión con el Servidor',
                                style: TextStyle(
                                  color: isConnected ? const Color(0xFFD1FAE5) : const Color(0xFFFFEDD5),
                                  fontWeight: FontWeight.w700,
                                  fontSize: 12,
                                ),
                              ),
                              Text(
                                activeUrl,
                                style: const TextStyle(
                                  color: Colors.white70,
                                  fontSize: 10,
                                  fontFamily: 'monospace',
                                ),
                                overflow: TextOverflow.ellipsis,
                              ),
                            ],
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 3),
                          decoration: BoxDecoration(
                            color: Colors.white.withValues(alpha: 0.15),
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: const Text(
                            'Configurar',
                            style: TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
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
                const Icon(Icons.check_circle_rounded, color: AppColors.statusConsumed, size: 28),
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
    final session = ref.read(sessionProvider);
    final controller = TextEditingController(
      text: session.activeBaseUrl ?? 'http://10.0.0.11:5298/api/v1',
    );
    var probando = false;

    showDialog<void>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (dialogCtx, setDlgState) => AlertDialog(
          title: const Row(
            children: [
              Icon(Icons.dns_rounded, color: AppColors.primary),
              SizedBox(width: 8),
              Text('Servidor FuelTrack', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            ],
          ),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Ingresa la dirección del servidor central FuelTrack:',
                style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: controller,
                decoration: const InputDecoration(
                  labelText: 'URL de la API',
                  hintText: 'http://10.0.0.11:5298/api/v1',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.link_rounded),
                ),
                style: const TextStyle(fontFamily: 'monospace', fontSize: 13),
              ),
              if (probando) ...[
                const SizedBox(height: 16),
                const Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)),
                    SizedBox(width: 10),
                    Text('Conectando al servidor...', style: TextStyle(fontSize: 13)),
                  ],
                ),
              ],
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Cerrar'),
            ),
            FilledButton.icon(
              icon: const Icon(Icons.check_circle_outline, size: 18),
              label: const Text('Guardar y Conectar'),
              onPressed: probando
                  ? null
                  : () async {
                      setDlgState(() => probando = true);
                      final url = controller.text.trim();
                      await session.setServerUrl(url);
                      await session.loginWithBackend();
                      setDlgState(() => probando = false);
                      if (ctx.mounted) Navigator.pop(ctx);
                      if (!mounted) return;
                      final ok = !session.isBypass;
                      final messenger = ScaffoldMessenger.of(context);
                      messenger.showSnackBar(
                        SnackBar(
                          content: Text(
                            ok
                                ? '✓ Conexión establecida con éxito: ${session.activeBaseUrl}'
                                : '⚠️ No se pudo contactar el servidor en esa dirección. Verifica tu red Wi-Fi.',
                          ),
                          backgroundColor: ok ? AppColors.statusConsumed : AppColors.statusExpired,
                        ),
                      );
                    },
            ),
          ],
        ),
      ),
    );
  }
}
