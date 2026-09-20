import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import '../app/providers.dart';
import '../core/errors.dart';
import '../core/models.dart';
import '../theme/app_theme.dart';
import '../widgets/dispatch_receipt_dialog.dart';


class DispatchFormScreen extends ConsumerStatefulWidget {
  const DispatchFormScreen({
    super.key,
    required this.ticket,
    required this.payload,
    required this.onReset,
  });

  final Ticket ticket;
  final String payload;
  final VoidCallback onReset;

  @override
  ConsumerState<DispatchFormScreen> createState() => _DispatchFormScreenState();
}

class _DispatchFormScreenState extends ConsumerState<DispatchFormScreen> {
  final form = GlobalKey<FormState>();
  final gallons = TextEditingController();
  final notes = TextEditingController();
  List<DispatchOption> tanks = [], stations = [];
  int? tank, station;
  bool loading = true, busy = false, confirmed = false, uncertain = false;
  String? error;
  DispatchResult? result;
  double _remaining = 0;

  @override
  void initState() {
    super.initState();
    _remaining = widget.ticket.quantity;
    gallons.addListener(_updateRemaining);
    load();
  }

  @override
  void dispose() {
    gallons.removeListener(_updateRemaining);
    gallons.dispose();
    notes.dispose();
    super.dispose();
  }

  void _updateRemaining() {
    final entered = double.tryParse(gallons.text.trim()) ?? 0;
    setState(() {
      _remaining = (widget.ticket.quantity - entered).clamp(0, widget.ticket.quantity);
    });
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
          tank = tanks.isNotEmpty ? tanks.first.id : null;
          station = stations.isNotEmpty ? stations.first.id : null;
          loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          error = friendlyError(e);
          loading = false;
        });
      }
    }
  }

  Future<void> submit() async {
    if (busy || uncertain || !confirmed || !form.currentState!.validate()) return;
    setState(() {
      busy = true;
      error = null;
    });

    final amount = double.parse(gallons.text.trim().replaceAll(',', '.'));
    try {
      final outcome = await ref.read(apiProvider).dispatch(
            widget.ticket,
            widget.payload,
            tank!,
            station!,
            amount,
            notes.text.trim(),
          );
      if (mounted) {
        setState(() {
          result = outcome;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          error = friendlyError(e);
          uncertain = e is ApiFailure && e.uncertain;
        });
      }
    }
    if (mounted) {
      setState(() => busy = false);
    }
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
    if (!ref.read(sessionProvider).user!.canDispatch) {
      return Scaffold(
        appBar: AppBar(title: const Text('Despacho')),
        body: const Center(
          child: Padding(
            padding: EdgeInsets.all(24),
            child: Text(
              'No tienes permiso de despachador.',
              style: TextStyle(fontSize: 16, color: AppColors.textSecondary),
            ),
          ),
        ),
      );
    }

    if (result != null) {
      return Scaffold(
        backgroundColor: AppColors.background,
        appBar: AppBar(title: const Text('Comprobante de despacho')),
        body: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: DispatchReceiptDialog(
              result: result!,
              onFinish: widget.onReset,
            ),
          ),
        ),
      );
    }

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Registrar despacho'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: widget.onReset,
        ),
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator(strokeWidth: 2.5))
          : SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Form(
                key: form,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Resumen del ticket validado
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              crossAxisAlignment: CrossAxisAlignment.center,
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        'TICKET AUTORIZADO',
                                        style: GoogleFonts.publicSans(
                                          fontSize: 10,
                                          fontWeight: FontWeight.w600,
                                          color: AppColors.textMuted,
                                          letterSpacing: 0.8,
                                        ),
                                      ),
                                      const SizedBox(height: 2),
                                      Text(
                                        widget.ticket.code,
                                        overflow: TextOverflow.ellipsis,
                                        style: AppTheme.mono(
                                          fontSize: 15,
                                          fontWeight: FontWeight.w600,
                                          color: AppColors.textPrimary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                  decoration: BoxDecoration(
                                    color: AppColors.primaryLight,
                                    borderRadius: BorderRadius.circular(3),
                                    border: Border.all(
                                      color: AppColors.primary.withValues(alpha: 0.3),
                                    ),
                                  ),
                                  child: Text(
                                    widget.ticket.fuel.toUpperCase(),
                                    style: GoogleFonts.publicSans(
                                      fontSize: 10,
                                      fontWeight: FontWeight.w600,
                                      color: AppColors.primary,
                                      letterSpacing: 0.5,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 12),
                            const Divider(height: 1, color: AppColors.cardBorder),
                            const SizedBox(height: 10),
                            Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        'CONDUCTOR',
                                        style: GoogleFonts.publicSans(
                                          fontSize: 10,
                                          fontWeight: FontWeight.w600,
                                          color: AppColors.textMuted,
                                          letterSpacing: 0.7,
                                        ),
                                      ),
                                      const SizedBox(height: 2),
                                      Text(
                                        widget.ticket.employee,
                                        overflow: TextOverflow.ellipsis,
                                        style: GoogleFonts.publicSans(
                                          fontSize: 13,
                                          fontWeight: FontWeight.w500,
                                          color: AppColors.textPrimary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        'VEHÍCULO / PLACA',
                                        style: GoogleFonts.publicSans(
                                          fontSize: 10,
                                          fontWeight: FontWeight.w600,
                                          color: AppColors.textMuted,
                                          letterSpacing: 0.7,
                                        ),
                                      ),
                                      const SizedBox(height: 2),
                                      Text(
                                        widget.ticket.vehicle,
                                        overflow: TextOverflow.ellipsis,
                                        style: GoogleFonts.publicSans(
                                          fontSize: 13,
                                          fontWeight: FontWeight.w600,
                                          color: AppColors.textPrimary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 12),
                            // Indicador de combustible
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                              decoration: BoxDecoration(
                                color: AppColors.background,
                                borderRadius: BorderRadius.circular(4),
                                border: Border.all(color: AppColors.cardBorder),
                              ),
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        'AUTORIZADO',
                                        style: GoogleFonts.publicSans(
                                          fontSize: 10,
                                          fontWeight: FontWeight.w600,
                                          color: AppColors.textMuted,
                                          letterSpacing: 0.7,
                                        ),
                                      ),
                                      const SizedBox(height: 2),
                                      Text(
                                        '${widget.ticket.quantity} gal',
                                        style: AppTheme.mono(
                                          fontSize: 15,
                                          fontWeight: FontWeight.w700,
                                          color: AppColors.textPrimary,
                                        ),
                                      ),
                                    ],
                                  ),
                                  const Icon(Icons.arrow_forward_rounded, size: 16, color: AppColors.textMuted),
                                  Column(
                                    crossAxisAlignment: CrossAxisAlignment.end,
                                    children: [
                                      Text(
                                        'REMANENTE ESTIMADO',
                                        style: GoogleFonts.publicSans(
                                          fontSize: 10,
                                          fontWeight: FontWeight.w600,
                                          color: AppColors.textMuted,
                                          letterSpacing: 0.7,
                                        ),
                                      ),
                                      const SizedBox(height: 2),
                                      Text(
                                        '$_remaining gal',
                                        style: AppTheme.mono(
                                          fontSize: 15,
                                          fontWeight: FontWeight.w700,
                                          color: _remaining == 0 ? AppColors.statusConsumed : AppColors.accent,
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 16),

                    // Selector de Tanque
                    DropdownButtonFormField<int>(
                      initialValue: tank,
                      decoration: const InputDecoration(
                        labelText: 'Tanque de combustible',
                        prefixIcon: Icon(Icons.storage_rounded, color: AppColors.textSecondary),
                      ),
                      items: tanks
                          .map((t) => DropdownMenuItem(value: t.id, child: Text(t.label)))
                          .toList(),
                      onChanged: (val) => setState(() => tank = val),
                      validator: (val) => val == null ? 'Selecciona un tanque compatible' : null,
                    ),
                    const SizedBox(height: 16),

                    // Selector de Estación
                    DropdownButtonFormField<int>(
                      initialValue: station,
                      decoration: const InputDecoration(
                        labelText: 'Estación de servicio',
                        prefixIcon: Icon(Icons.ev_station_rounded, color: AppColors.textSecondary),
                      ),
                      items: stations
                          .map((s) => DropdownMenuItem(value: s.id, child: Text(s.label)))
                          .toList(),
                      onChanged: (val) => setState(() => station = val),
                      validator: (val) => val == null ? 'Selecciona una estación activa' : null,
                    ),
                    const SizedBox(height: 16),

                    // Campo de Galones
                    TextFormField(
                      controller: gallons,
                      enabled: !busy,
                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                      decoration: const InputDecoration(
                        labelText: 'Galones a despachar',
                        hintText: '0.00',
                        prefixIcon: Icon(Icons.speed_rounded, color: AppColors.textSecondary),
                        suffixText: 'gal',
                      ),
                      validator: (val) => validateGallons(val, widget.ticket.quantity),
                    ),
                    const SizedBox(height: 16),

                    // Observaciones
                    TextFormField(
                      controller: notes,
                      enabled: !busy,
                      maxLength: 500,
                      decoration: const InputDecoration(
                        labelText: 'Observaciones (opcional)',
                        hintText: 'Detalles del suministro…',
                        prefixIcon: Icon(Icons.notes_rounded, color: AppColors.textSecondary),
                      ),
                      maxLines: 2,
                    ),
                    const SizedBox(height: 16),

                    // Checkbox de confirmación
                    Card(
                      child: CheckboxListTile(
                        value: confirmed,
                        onChanged: busy ? null : (val) => setState(() => confirmed = val ?? false),
                        activeColor: AppColors.primary,
                        title: const Text(
                          'Confirmo la identidad del conductor y la placa del vehículo',
                          style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
                        ),
                        controlAffinity: ListTileControlAffinity.leading,
                        contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                      ),
                    ),
                    const SizedBox(height: 16),

                    if (tanks.isEmpty || stations.isEmpty)
                      Container(
                        padding: const EdgeInsets.all(12),
                        margin: const EdgeInsets.only(bottom: 16),
                        decoration: BoxDecoration(
                          color: AppColors.statusExpiredBg,
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: const Text(
                          'No hay tanques o estaciones disponibles. Contacta al administrador.',
                          style: TextStyle(color: AppColors.statusExpired, fontSize: 13, fontWeight: FontWeight.w600),
                        ),
                      ),

                    if (error != null)
                      Container(
                        padding: const EdgeInsets.all(12),
                        margin: const EdgeInsets.only(bottom: 16),
                        decoration: BoxDecoration(
                          color: AppColors.statusExpiredBg,
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          error!,
                          style: const TextStyle(color: AppColors.statusExpired, fontSize: 13, fontWeight: FontWeight.w600),
                        ),
                      ),

                    // Botón de Acción
                    if (uncertain)
                      FilledButton(
                        onPressed: busy ? null : reconcile,
                        style: FilledButton.styleFrom(
                          backgroundColor: AppColors.accent,
                          minimumSize: const Size.fromHeight(54),
                        ),
                        child: busy
                            ? const SizedBox(
                                width: 22,
                                height: 22,
                                child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white),
                              )
                            : const Text(
                                'Consultar resultado',
                                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                              ),
                      )
                    else
                      FilledButton(
                        onPressed: (!confirmed || busy || tanks.isEmpty || stations.isEmpty) ? null : submit,
                        style: FilledButton.styleFrom(
                          backgroundColor: AppColors.primary,
                          minimumSize: const Size.fromHeight(54),
                        ),
                        child: busy
                            ? const SizedBox(
                                width: 22,
                                height: 22,
                                child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white),
                              )
                            : const Text(
                                'Confirmar despacho',
                                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                              ),
                      ),
                  ],
                ),
              ),
            ),
    );
  }
}
