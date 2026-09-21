import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../core/models.dart';
import '../theme/app_theme.dart';

class DispatchReceiptDialog extends StatelessWidget {
  const DispatchReceiptDialog({
    super.key,
    required this.result,
    required this.onFinish,
  });

  final DispatchResult result;
  final VoidCallback onFinish;

  @override
  Widget build(BuildContext context) {
    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
      elevation: 0,
      backgroundColor: Colors.transparent,
      child: Container(
        padding: const EdgeInsets.all(22),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(4),
          border: Border.all(color: AppColors.cardBorder),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.14),
              blurRadius: 28,
              offset: const Offset(0, 8),
            ),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // ── Encabezado ─────────────────────────────────────────────────
            Row(
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: AppColors.statusConsumedBg,
                    borderRadius: BorderRadius.circular(4),
                    border: Border.all(
                      color: AppColors.statusConsumed.withValues(alpha: 0.4),
                    ),
                  ),
                  child: const Icon(
                    Icons.check_rounded,
                    color: AppColors.statusConsumed,
                    size: 24,
                  ),
                ),
                const SizedBox(width: 12),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Despacho registrado',
                      style: GoogleFonts.publicSans(
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      'Ticket Consumido',
                      style: GoogleFonts.publicSans(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: AppColors.statusConsumed,
                      ),
                    ),
                  ],
                ),
              ],
            ),

            const SizedBox(height: 18),
            const Divider(height: 1, color: AppColors.cardBorder),
            const SizedBox(height: 16),

            // ── Comprobante ────────────────────────────────────────────────
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: AppColors.background,
                borderRadius: BorderRadius.circular(4),
                border: Border.all(color: AppColors.cardBorder),
              ),
              child: Column(
                children: [
                  _ReceiptRow(label: 'Ticket', value: result.code, mono: true),
                  const SizedBox(height: 8),
                  _ReceiptRow(
                    label: 'No. Despacho',
                    value: '#${result.id}',
                    mono: true,
                  ),
                  const Divider(height: 18, color: AppColors.cardBorder),
                  _ReceiptRow(
                    label: 'Galones servidos',
                    value: '${result.gallons} gal',
                    mono: true,
                    highlight: true,
                    highlightColor: AppColors.primary,
                  ),
                  const SizedBox(height: 8),
                  _ReceiptRow(
                    label: 'Remanente',
                    value: '${result.remaining ?? 0} gal',
                    mono: true,
                    highlight: false,
                    highlightColor: AppColors.textSecondary,
                  ),
                ],
              ),
            ),

            const SizedBox(height: 18),

            SizedBox(
              height: 46,
              child: FilledButton(
                onPressed: onFinish,
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
                child: const Text('Finalizar'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ReceiptRow extends StatelessWidget {
  const _ReceiptRow({
    required this.label,
    required this.value,
    this.highlight = false,
    this.highlightColor,
    this.mono = false,
  });

  final String label;
  final String value;
  final bool highlight;
  final Color? highlightColor;
  final bool mono;

  @override
  Widget build(BuildContext context) {
    final effectiveColor = highlightColor ?? AppColors.textPrimary;
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: GoogleFonts.publicSans(
            fontSize: 13,
            color: AppColors.textSecondary,
            fontWeight: FontWeight.w500,
          ),
        ),
        Text(
          value,
          style: mono
              ? AppTheme.mono(
                  fontSize: highlight ? 15 : 13,
                  fontWeight: highlight ? FontWeight.w700 : FontWeight.w600,
                  color: effectiveColor,
                )
              : GoogleFonts.publicSans(
                  fontSize: highlight ? 15 : 13,
                  color: effectiveColor,
                  fontWeight: highlight ? FontWeight.w700 : FontWeight.w600,
                ),
        ),
      ],
    );
  }
}
