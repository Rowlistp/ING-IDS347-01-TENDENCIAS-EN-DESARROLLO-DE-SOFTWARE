import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';

/// Badge de estado estilo "bandas de medidor" — igual que StatusBadge.jsx de la web.
/// Borde definido + fondo semitransparente + texto uppercase + letter-spacing.
class StatusBadge extends StatelessWidget {
  const StatusBadge({
    super.key,
    required this.status,
    this.label,
  });

  final String status;
  final String? label;

  @override
  Widget build(BuildContext context) {
    final colors = AppColors.getStatusColors(status);
    final displayLabel = (label ?? status).toUpperCase();

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
      decoration: BoxDecoration(
        color: colors.bg,
        borderRadius: BorderRadius.circular(3),
        border: Border.all(color: colors.fg.withValues(alpha: 0.4)),
      ),
      child: Text(
        displayLabel,
        style: GoogleFonts.publicSans(
          fontSize: 10,
          fontWeight: FontWeight.w600,
          color: colors.fg,
          letterSpacing: 0.6,
          height: 1,
        ),
      ),
    );
  }
}
