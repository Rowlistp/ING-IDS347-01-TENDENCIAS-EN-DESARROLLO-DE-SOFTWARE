import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import '../app/providers.dart';
import '../theme/app_theme.dart';
import '../widgets/status_badge.dart';

class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionProvider);
    final config = ref.read(configProvider);
    final user = session.user;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(title: const Text('Configuración')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [

          // ── Perfil ─────────────────────────────────────────────────────────
          _SectionLabel('PERFIL'),
          _Card(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: BoxDecoration(
                        color: AppColors.primaryLight,
                        borderRadius: BorderRadius.circular(4),
                        border: Border.all(color: AppColors.cardBorder),
                      ),
                      child: const Icon(
                        Icons.person_rounded,
                        color: AppColors.primary,
                        size: 24,
                      ),
                    ),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            user?.name ?? 'Usuario',
                            style: GoogleFonts.publicSans(
                              fontSize: 15,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          if (user != null && user.roles.isNotEmpty) ...[
                            const SizedBox(height: 6),
                            Wrap(
                              spacing: 6,
                              runSpacing: 4,
                              children: user.roles
                                  .map((r) => StatusBadge(status: r, label: r))
                                  .toList(),
                            ),
                          ],
                        ],
                      ),
                    ),
                  ],
                ),
                if (session.isBypass) ...[
                  const SizedBox(height: 12),
                  const Divider(height: 1),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Icon(Icons.info_outline_rounded, size: 14, color: AppColors.accent),
                      const SizedBox(width: 6),
                      Text(
                        'Modo de desarrollo — bypass activo',
                        style: GoogleFonts.publicSans(
                          fontSize: 12,
                          color: AppColors.accent,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),

          const SizedBox(height: 20),

          // ── Conexión ───────────────────────────────────────────────────────
          _SectionLabel('CONEXIÓN'),
          _Card(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      width: 10,
                      height: 10,
                      decoration: BoxDecoration(
                        color: session.isBypass ? AppColors.accent : AppColors.statusConsumed,
                        shape: BoxShape.circle,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text(
                      session.isBypass
                          ? 'Modo local (Sin conexión con Backend)'
                          : 'Conectado a FuelTrack API',
                      style: GoogleFonts.publicSans(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: session.isBypass ? AppColors.accent : AppColors.statusConsumed,
                      ),
                    ),
                  ],
                ),
                const Divider(height: 20),
                _InfoRow(
                  label: 'Servidor API',
                  value: session.activeBaseUrl ?? config.apiUrl,
                  mono: true,
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.sync_rounded, size: 16),
                        label: const Text('Reconectar'),
                        onPressed: () async {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('Comprobando conexión con el backend...')),
                          );
                          await session.loginWithBackend();
                          if (context.mounted) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(
                                content: Text(
                                  session.isBypass
                                      ? 'No se pudo conectar con el backend. Verifica que esté corriendo.'
                                      : '¡Conectado exitosamente a ${session.activeBaseUrl}!',
                                ),
                                backgroundColor: session.isBypass ? AppColors.statusExpired : AppColors.statusConsumed,
                              ),
                            );
                          }
                        },
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: FilledButton.icon(
                        icon: const Icon(Icons.edit_rounded, size: 16),
                        label: const Text('Cambiar IP'),
                        onPressed: () async {
                          final controller = TextEditingController(
                            text: session.activeBaseUrl ?? 'http://10.0.0.11:5298/api/v1',
                          );
                          final newUrl = await showDialog<String>(
                            context: context,
                            builder: (ctx) => AlertDialog(
                              title: const Text('Dirección del Servidor API'),
                              content: Column(
                                mainAxisSize: MainAxisSize.min,
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'Ingresa la dirección IP o URL del servidor FuelTrack:',
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
                                  child: const Text('Guardar y Probar'),
                                ),
                              ],
                            ),
                          );
                          if (newUrl != null && newUrl.isNotEmpty) {
                            await session.setServerUrl(newUrl);
                            await session.loginWithBackend();
                            if (context.mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                SnackBar(
                                  content: Text(
                                    session.isBypass
                                        ? 'No se pudo conectar a $newUrl'
                                        : '¡Conectado exitosamente a $newUrl!',
                                  ),
                                  backgroundColor: session.isBypass ? AppColors.statusExpired : AppColors.statusConsumed,
                                ),
                              );
                            }
                          }
                        },
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),

          const SizedBox(height: 20),

          // ── App ────────────────────────────────────────────────────────────
          _SectionLabel('APLICACIÓN'),
          _Card(
            child: Column(
              children: [
                const _InfoRow(label: 'Versión', value: '1.0.0'),
                const Divider(height: 20),
                const _InfoRow(label: 'Plataforma', value: 'Android'),
              ],
            ),
          ),

          const SizedBox(height: 28),

          // ── Cerrar sesión ──────────────────────────────────────────────────
          SizedBox(
            height: 48,
            child: OutlinedButton.icon(
              onPressed: () async {
                final confirmed = await showDialog<bool>(
                  context: context,
                  builder: (ctx) => AlertDialog(
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(4),
                    ),
                    title: Text(
                      'Cerrar sesión',
                      style: GoogleFonts.publicSans(
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    content: Text(
                      '¿Estás seguro de que deseas salir del sistema?',
                      style: GoogleFonts.publicSans(color: AppColors.textSecondary),
                    ),
                    actions: [
                      TextButton(
                        onPressed: () => Navigator.pop(ctx, false),
                        child: Text(
                          'Cancelar',
                          style: GoogleFonts.publicSans(color: AppColors.textSecondary),
                        ),
                      ),
                      FilledButton(
                        onPressed: () => Navigator.pop(ctx, true),
                        style: FilledButton.styleFrom(
                          backgroundColor: AppColors.statusExpired,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(4),
                          ),
                        ),
                        child: Text(
                          'Cerrar sesión',
                          style: GoogleFonts.publicSans(fontWeight: FontWeight.w600),
                        ),
                      ),
                    ],
                  ),
                );
                if (confirmed == true) {
                  await ref.read(sessionProvider).logout();
                }
              },
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.statusExpired,
                side: const BorderSide(color: AppColors.statusExpired, width: 1.5),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(4),
                ),
                textStyle: GoogleFonts.publicSans(fontWeight: FontWeight.w600, fontSize: 14),
              ),
              icon: const Icon(Icons.logout_rounded, size: 18),
              label: const Text('Cerrar sesión'),
            ),
          ),

          const SizedBox(height: 24),
        ],
      ),
    );
  }
}

// ─── Componentes internos ────────────────────────────────────────────────────

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.text);
  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text(
        text,
        style: GoogleFonts.publicSans(
          fontSize: 11,
          fontWeight: FontWeight.w600,
          color: AppColors.textSecondary,
          letterSpacing: 0.8,
        ),
      ),
    );
  }
}

class _Card extends StatelessWidget {
  const _Card({required this.child});
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(4),
        border: Border.all(color: AppColors.cardBorder),
      ),
      child: child,
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({
    required this.label,
    required this.value,
    this.mono = false,
  });
  final String label;
  final String value;
  final bool mono;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 110,
          child: Text(
            label,
            style: GoogleFonts.publicSans(
              fontSize: 13,
              color: AppColors.textSecondary,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: mono
                ? AppTheme.mono(fontSize: 12, color: AppColors.textPrimary)
                : GoogleFonts.publicSans(
                    fontSize: 13,
                    color: AppColors.textPrimary,
                    fontWeight: FontWeight.w600,
                  ),
          ),
        ),
      ],
    );
  }
}
