import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../screens/home_screen.dart';
import '../screens/login_screen.dart';
import '../theme/app_theme.dart';
import 'providers.dart';

export '../screens/home_screen.dart';
export '../screens/login_screen.dart';
export '../screens/scan_screen.dart';
export '../screens/tickets_screen.dart';
export '../screens/dispatch_screen.dart';
export '../widgets/ticket_card.dart';
export '../widgets/status_badge.dart';

class FuelTrackApp extends StatelessWidget {
  const FuelTrackApp({super.key});

  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'FuelTrack',
    debugShowCheckedModeBanner: false,
    theme: AppTheme.lightTheme,
    home: const SessionScreen(),
  );
}

class SessionScreen extends ConsumerWidget {
  const SessionScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionProvider);
    return ListenableBuilder(
      listenable: session,
      builder: (context, _) {
        if (session.user != null) return const HomeScreen();
        return const LoginScreen();
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
    child: Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.statusExpiredBg,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: AppColors.statusExpired.withValues(alpha: 0.2),
        ),
      ),
      child: Row(
        children: [
          const Icon(
            Icons.error_outline_rounded,
            color: AppColors.statusExpired,
            size: 20,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Semantics(
              liveRegion: true,
              child: Text(
                message,
                style: const TextStyle(
                  color: AppColors.statusExpired,
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ),
        ],
      ),
    ),
  );
}
