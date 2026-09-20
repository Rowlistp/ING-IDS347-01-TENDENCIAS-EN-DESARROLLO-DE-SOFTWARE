import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:google_fonts/google_fonts.dart';
import '../app/providers.dart';
import '../core/errors.dart';
import '../theme/app_theme.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  bool busy = false;
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
      // Primera apertura o sesión no recuperable
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
      if (session.isKeycloak) {
        await session.loginWithBackend();
      } else {
        await session.login();
      }
      final user = await ref.read(apiProvider).me();
      if (mounted) session.setUser(user);
    } on FlutterAppAuthUserCancelledException {
      // Cancelar vuelve a login
    } catch (e) {
      if (mounted) setState(() => error = friendlyError(e));
    }
    if (mounted) setState(() => busy = false);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.primary, // fondo tanque oscuro, igual que la web
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 400),
              child: Container(
                padding: const EdgeInsets.all(28),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(4),
                  border: Border.all(
                    color: AppColors.textSecondary.withValues(alpha: 0.3),
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.18),
                      blurRadius: 32,
                      offset: const Offset(0, 8),
                    ),
                  ],
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Label eyebrow estilo web: "FUELTRACK" uppercase tracking
                    Text(
                      'FUELTRACK',
                      style: GoogleFonts.publicSans(
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                        letterSpacing: 2.5,
                        color: AppColors.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 6),

                    Text(
                      'Acceso al sistema',
                      style: GoogleFonts.publicSans(
                        fontSize: 22,
                        fontWeight: FontWeight.w600,
                        color: AppColors.primary,
                        height: 1.2,
                      ),
                    ),

                    const SizedBox(height: 24),

                    if (error != null) ...[
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: AppColors.statusExpiredBg,
                          borderRadius: BorderRadius.circular(4),
                          border: Border.all(
                            color: AppColors.statusExpired.withValues(alpha: 0.4),
                          ),
                        ),
                        child: Row(
                          children: [
                            const Icon(
                              Icons.error_outline_rounded,
                              color: AppColors.statusExpired,
                              size: 18,
                            ),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                error!,
                                style: GoogleFonts.publicSans(
                                  fontSize: 13,
                                  color: AppColors.statusExpired,
                                  fontWeight: FontWeight.w500,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                    ],

                    // Botón de ingreso
                    SizedBox(
                      height: 44,
                      child: FilledButton(
                        onPressed: busy ? null : login,
                        style: FilledButton.styleFrom(
                          backgroundColor: AppColors.primary,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(4),
                          ),
                          textStyle: GoogleFonts.publicSans(
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        child: busy
                            ? const SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                                ),
                              )
                            : const Text('Iniciar sesión'),

                      ),
                    ),
                    const SizedBox(height: 10),

                    TextButton.icon(
                      onPressed: () async {
                        final session = ref.read(sessionProvider);
                        final controller = TextEditingController(
                          text: session.activeBaseUrl ?? 'http://10.0.0.11:5298/api/v1',
                        );
                        final newUrl = await showDialog<String>(
                          context: context,
                          builder: (ctx) => AlertDialog(
                            title: const Text('Configurar IP del Servidor'),
                            content: Column(
                              mainAxisSize: MainAxisSize.min,
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Ingresa la dirección o IP del servidor API:',
                                  style: TextStyle(fontSize: 13),
                                ),
                                const SizedBox(height: 12),
                                TextField(
                                  controller: controller,
                                  decoration: const InputDecoration(
                                    border: OutlineInputBorder(),
                                    labelText: 'URL de la API',
                                    hintText: 'http://10.0.0.11:5298/api/v1',
                                  ),
                                ),
                              ],
                            ),
                            actions: [
                              TextButton(
                                onPressed: () => Navigator.pop(ctx),
                                child: const Text('Cancelar'),
                              ),
                              FilledButton(
                                onPressed: () => Navigator.pop(ctx, controller.text),
                                child: const Text('Guardar'),
                              ),
                            ],
                          ),
                        );
                        if (newUrl != null && newUrl.isNotEmpty) {
                          await session.setServerUrl(newUrl);
                          if (context.mounted) {
                            setState(() {});
                          }
                        }
                      },
                      icon: const Icon(Icons.settings_ethernet_rounded, size: 14, color: AppColors.textMuted),
                      label: Text(
                        'Servidor: ${ref.watch(sessionProvider).activeBaseUrl ?? 'Automático'}',
                        style: GoogleFonts.publicSans(fontSize: 11, color: AppColors.textMuted),
                      ),
                    ),

                    const SizedBox(height: 8),

                    Text(
                      'Acceso restringido · Solo personal autorizado',
                      textAlign: TextAlign.center,
                      style: GoogleFonts.publicSans(
                        fontSize: 11,
                        color: AppColors.textMuted,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
