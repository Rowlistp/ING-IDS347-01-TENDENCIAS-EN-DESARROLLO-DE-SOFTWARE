import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../core/models.dart';
import '../theme/app_theme.dart';
import 'status_badge.dart';

/// Tarjeta de ticket — estilo "rounded-sm border" alineado con la web.
class TicketCard extends StatelessWidget {
  const TicketCard({super.key, required this.ticket, this.onTap});

  final Ticket ticket;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final expiryFormatted =
        '${ticket.expires.toLocal().day.toString().padLeft(2, '0')}/'
        '${ticket.expires.toLocal().month.toString().padLeft(2, '0')}/'
        '${ticket.expires.toLocal().year} '
        '${ticket.expires.toLocal().hour.toString().padLeft(2, '0')}:'
        '${ticket.expires.toLocal().minute.toString().padLeft(2, '0')}';

    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(4),
        border: Border.all(color: AppColors.cardBorder),
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(4),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // ── Cabecera: código + badge ──────────────────────────────
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'TICKET',
                            style: GoogleFonts.publicSans(
                              fontSize: 10,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textMuted,
                              letterSpacing: 0.8,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            ticket.code,
                            style: AppTheme.mono(
                              fontSize: 15,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textPrimary,
                            ),
                          ),
                        ],
                      ),
                    ),
                    StatusBadge(status: ticket.state, label: ticket.stateLabel),
                  ],
                ),

                const SizedBox(height: 12),
                const Divider(height: 1, color: AppColors.cardBorder),
                const SizedBox(height: 10),

                // ── Detalles en grid 2×2 ─────────────────────────────────
                Row(
                  children: [
                    Expanded(
                      child: _DetailItem(
                        label: 'CONDUCTOR',
                        value: ticket.employee,
                      ),
                    ),
                    Expanded(
                      child: _DetailItem(
                        label: 'VEHÍCULO',
                        value: ticket.vehicle,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    Expanded(
                      child: _DetailItem(
                        label: 'COMBUSTIBLE',
                        value: ticket.fuel,
                        valueStyle: AppTheme.mono(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.primary,
                        ),
                      ),
                    ),
                    Expanded(
                      child: _DetailItem(
                        label: 'GALONES',
                        value: '${ticket.quantity} gal',
                        valueStyle: AppTheme.mono(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                _DetailItem(
                  label: 'VENCE',
                  value: expiryFormatted,
                  valueStyle: AppTheme.mono(
                    fontSize: 12,
                    fontWeight: FontWeight.w500,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _DetailItem extends StatelessWidget {
  const _DetailItem({
    required this.label,
    required this.value,
    this.valueStyle,
  });

  final String label;
  final String value;
  final TextStyle? valueStyle;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: GoogleFonts.publicSans(
            fontSize: 10,
            fontWeight: FontWeight.w600,
            color: AppColors.textMuted,
            letterSpacing: 0.7,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style:
              valueStyle ??
              GoogleFonts.publicSans(
                fontSize: 13,
                fontWeight: FontWeight.w500,
                color: AppColors.textPrimary,
              ),
        ),
      ],
    );
  }
}
