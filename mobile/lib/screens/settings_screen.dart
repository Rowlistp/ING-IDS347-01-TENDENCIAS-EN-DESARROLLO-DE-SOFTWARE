import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../app/providers.dart';
import '../core/errors.dart';
import '../theme/app_theme.dart';

class SettingsScreen extends ConsumerStatefulWidget {
  const SettingsScreen({super.key});
  @override
  ConsumerState<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends ConsumerState<SettingsScreen> {
  bool checking = false;
  String? connection;
  @override
  Widget build(BuildContext context) {
    final session = ref.watch(sessionProvider);
    final config = ref.watch(configProvider);
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(title: const Text('Configuración')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const Text(
            'Perfil',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          ListTile(
            leading: const Icon(Icons.person_outline),
            title: Text(session.user?.name ?? ''),
            subtitle: Text(session.user?.roles.join(', ') ?? ''),
          ),
          const Divider(),
          const Text(
            'Servidor configurado',
            style: TextStyle(fontWeight: FontWeight.bold),
          ),
          SelectableText(config.apiUrl),
          const SizedBox(height: 12),
          const Text(
            'Los datos y los despachos requieren conexión con el servidor.',
          ),
          if (connection != null)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 12),
              child: Text(connection!),
            ),
          OutlinedButton.icon(
            icon: const Icon(Icons.sync),
            label: const Text('Comprobar conexión'),
            onPressed: checking
                ? null
                : () async {
                    setState(() {
                      checking = true;
                      connection = null;
                    });
                    try {
                      await ref.read(apiProvider).me();
                      if (mounted) {
                        setState(() => connection = 'Conexión comprobada.');
                      }
                    } catch (e) {
                      if (mounted) {
                        setState(() => connection = friendlyError(e));
                      }
                    } finally {
                      if (mounted) setState(() => checking = false);
                    }
                  },
          ),
          const SizedBox(height: 24),
          OutlinedButton.icon(
            icon: const Icon(Icons.logout),
            label: const Text('Cerrar sesión'),
            onPressed: () async {
              final confirmed = await showDialog<bool>(
                context: context,
                builder: (ctx) => AlertDialog(
                  title: const Text('Cerrar sesión'),
                  content: const Text('¿Deseas salir del sistema?'),
                  actions: [
                    TextButton(
                      onPressed: () => Navigator.pop(ctx, false),
                      child: const Text('Cancelar'),
                    ),
                    FilledButton(
                      onPressed: () => Navigator.pop(ctx, true),
                      child: const Text('Cerrar sesión'),
                    ),
                  ],
                ),
              );
              if (confirmed != true || !mounted) return;
              try {
                await ref.read(sessionProvider).logout();
              } catch (e) {
                if (context.mounted) {
                  ScaffoldMessenger.of(
                    context,
                  ).showSnackBar(SnackBar(content: Text(friendlyError(e))));
                }
              }
            },
          ),
        ],
      ),
    );
  }
}
