import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../core/models.dart';
import '../theme/app_theme.dart';
import '../widgets/scanner_overlay.dart';

class CameraScanner extends StatefulWidget {
  const CameraScanner({super.key, required this.onScan});
  final ValueChanged<String> onScan;

  @override
  State<CameraScanner> createState() => _CameraScannerState();
}

class _CameraScannerState extends State<CameraScanner> {
  late final MobileScannerController controller;
  final gate = ScanGate();
  bool torchOn = false;

  @override
  void initState() {
    super.initState();
    controller = MobileScannerController(
      formats: const [BarcodeFormat.qrCode],
      detectionSpeed: DetectionSpeed.normal,
    );
  }

  @override
  void dispose() {
    controller.dispose();
    super.dispose();
  }

  void _showManualInputDialog() {
    final textController = TextEditingController(text: 'COM-2026-000001');
    showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
        title: Text(
          'Ingreso manual de código',
          style: GoogleFonts.publicSans(
            fontSize: 16,
            fontWeight: FontWeight.w600,
            color: AppColors.primary,
          ),
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Escribe o pega el código del ticket QR para validarlo:',
              style: GoogleFonts.publicSans(fontSize: 13, color: AppColors.textSecondary),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: textController,
              autofocus: true,
              style: AppTheme.mono(fontSize: 14),
              decoration: InputDecoration(
                hintText: 'COM-2026-XXXXXX',
                prefixIcon: const Icon(Icons.qr_code_rounded, size: 20),
                contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(4)),
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              final code = textController.text.trim();
              if (code.isNotEmpty) {
                Navigator.of(ctx).pop();
                widget.onScan(code);
              }
            },
            style: FilledButton.styleFrom(
              backgroundColor: AppColors.primary,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
            ),
            child: const Text('Validar'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Instrucción superior
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          color: AppColors.primary,
          width: double.infinity,
          child: Row(
            children: [
              const Icon(Icons.camera_alt_outlined, size: 18, color: Colors.white70),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Apunta la cámara al código QR oficial para validarlo.',
                  style: GoogleFonts.publicSans(
                    fontSize: 12,
                    color: Colors.white,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
            ],
          ),
        ),

        // Visor de Cámara con Retícula y Controles
        Expanded(
          child: Stack(
            fit: StackFit.expand,
            children: [
              MobileScanner(
                controller: controller,
                errorBuilder: (context, error) => Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.no_photography_outlined, size: 48, color: Colors.white70),
                        const SizedBox(height: 12),
                        Text(
                          'No se pudo abrir la cámara. Revisa el permiso de la cámara en Ajustes de tu teléfono.',
                          textAlign: TextAlign.center,
                          style: GoogleFonts.publicSans(color: Colors.white, fontSize: 13),
                        ),
                        const SizedBox(height: 16),
                        FilledButton.icon(
                          onPressed: () => controller.start(),
                          icon: const Icon(Icons.refresh_rounded, size: 18),
                          label: const Text('Reintentar cámara'),
                          style: FilledButton.styleFrom(
                            backgroundColor: AppColors.accent,
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                onDetect: (capture) async {
                  for (final barcode in capture.barcodes) {
                    final value = barcode.rawValue ?? barcode.displayValue;
                    if (value != null && value.trim().isNotEmpty && gate.accept(value.trim())) {
                      await controller.stop();
                      if (mounted) widget.onScan(value.trim());
                      break;
                    }
                  }
                },
              ),

              // Retícula de encuadre
              const ScannerOverlay(
                borderColor: AppColors.accent,
                scanWindowSize: 240,
              ),

              // Controles flotantes sobre la cámara (Linterna y Cambio de Cámara)
              Positioned(
                top: 16,
                right: 16,
                child: Row(
                  children: [
                    Container(
                      decoration: BoxDecoration(
                        color: Colors.black.withValues(alpha: 0.6),
                        shape: BoxShape.circle,
                      ),
                      child: IconButton(
                        icon: Icon(
                          torchOn ? Icons.flash_on_rounded : Icons.flash_off_rounded,
                          color: torchOn ? AppColors.accent : Colors.white,
                          size: 20,
                        ),
                        tooltip: 'Linterna',
                        onPressed: () async {
                          await controller.toggleTorch();
                          setState(() => torchOn = !torchOn);
                        },
                      ),
                    ),
                    const SizedBox(width: 8),
                    Container(
                      decoration: BoxDecoration(
                        color: Colors.black.withValues(alpha: 0.6),
                        shape: BoxShape.circle,
                      ),
                      child: IconButton(
                        icon: const Icon(Icons.flip_camera_ios_rounded, color: Colors.white, size: 20),
                        tooltip: 'Cambiar cámara',
                        onPressed: () => controller.switchCamera(),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),

        // Barra inferior de opciones alternativas
        Container(
          color: Colors.white,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: SizedBox(
            width: double.infinity,
            height: 44,
            child: OutlinedButton.icon(
              onPressed: _showManualInputDialog,
              icon: const Icon(Icons.keyboard_outlined, size: 18),
              label: Text(
                'Ingreso manual del código de ticket',
                style: GoogleFonts.publicSans(fontSize: 13, fontWeight: FontWeight.w600),
              ),
              style: OutlinedButton.styleFrom(
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
                side: const BorderSide(color: AppColors.cardBorder),
                foregroundColor: AppColors.textPrimary,
              ),
            ),
          ),
        ),
      ],
    );
  }
}
