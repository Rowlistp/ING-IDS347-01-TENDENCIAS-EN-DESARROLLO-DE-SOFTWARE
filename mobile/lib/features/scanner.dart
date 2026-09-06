import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../core/models.dart';

class CameraScanner extends StatefulWidget {
  const CameraScanner({super.key, required this.onScan});
  final ValueChanged<String> onScan;
  @override
  State<CameraScanner> createState() => _CameraScannerState();
}

class _CameraScannerState extends State<CameraScanner> {
  final controller = MobileScannerController(formats: [BarcodeFormat.qrCode]);
  final gate = ScanGate();
  @override
  void dispose() {
    controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Column(
    children: [
      const Padding(
        padding: EdgeInsets.all(16),
        child: Text(
          'Coloca el QR dentro de la cámara. Escanear no consume el ticket.',
        ),
      ),
      Expanded(
        child: MobileScanner(
          controller: controller,
          errorBuilder: (context, error) => Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.no_photography_outlined, size: 48),
                const Text(
                  'No se pudo abrir la cámara. Revisa el permiso en Ajustes.',
                ),
                TextButton(
                  onPressed: () => controller.start(),
                  child: const Text('Reintentar cámara'),
                ),
              ],
            ),
          ),
          onDetect: (capture) async {
            for (final barcode in capture.barcodes) {
              if (gate.accept(barcode.rawValue)) {
                await controller.stop();
                if (mounted) widget.onScan(gate.payload!);
                break;
              }
            }
          },
        ),
      ),
    ],
  );
}
