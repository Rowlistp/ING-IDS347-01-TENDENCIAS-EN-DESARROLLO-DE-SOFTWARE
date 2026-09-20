import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

class ScannerOverlay extends StatelessWidget {
  const ScannerOverlay({
    super.key,
    this.scanWindowSize = 260.0,
    this.borderColor = AppColors.primary,
  });

  final double scanWindowSize;
  final Color borderColor;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final left = (constraints.maxWidth - scanWindowSize) / 2;
        final top = (constraints.maxHeight - scanWindowSize) / 2 - 30;
        final rect = Rect.fromLTWH(left, top, scanWindowSize, scanWindowSize);

        return Stack(
          children: [
            ColorFiltered(
              colorFilter: ColorFilter.mode(
                Colors.black.withValues(alpha: 0.55),
                BlendMode.srcOut,
              ),
              child: Stack(
                children: [
                  Container(
                    decoration: const BoxDecoration(
                      color: Colors.transparent,
                      backgroundBlendMode: BlendMode.dstOut,
                    ),
                  ),
                  Positioned(
                    left: rect.left,
                    top: rect.top,
                    child: Container(
                      width: rect.width,
                      height: rect.height,
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(20),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            Positioned(
              left: rect.left,
              top: rect.top,
              child: CustomPaint(
                size: Size(rect.width, rect.height),
                painter: _CornerBorderPainter(
                  color: borderColor,
                  strokeWidth: 4,
                  cornerLength: 28,
                  borderRadius: 20,
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}

class _CornerBorderPainter extends CustomPainter {
  _CornerBorderPainter({
    required this.color,
    required this.strokeWidth,
    required this.cornerLength,
    required this.borderRadius,
  });

  final Color color;
  final double strokeWidth;
  final double cornerLength;
  final double borderRadius;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = strokeWidth
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    final w = size.width;
    final h = size.height;

    // Top-left
    final pathTL = Path()
      ..moveTo(0, cornerLength)
      ..lineTo(0, borderRadius)
      ..arcToPoint(Offset(borderRadius, 0), radius: Radius.circular(borderRadius))
      ..lineTo(cornerLength, 0);
    canvas.drawPath(pathTL, paint);

    // Top-right
    final pathTR = Path()
      ..moveTo(w - cornerLength, 0)
      ..lineTo(w - borderRadius, 0)
      ..arcToPoint(Offset(w, borderRadius), radius: Radius.circular(borderRadius))
      ..lineTo(w, cornerLength);
    canvas.drawPath(pathTR, paint);

    // Bottom-left
    final pathBL = Path()
      ..moveTo(0, h - cornerLength)
      ..lineTo(0, h - borderRadius)
      ..arcToPoint(Offset(borderRadius, h), radius: Radius.circular(borderRadius))
      ..lineTo(cornerLength, h);
    canvas.drawPath(pathBL, paint);

    // Bottom-right
    final pathBR = Path()
      ..moveTo(w - cornerLength, h)
      ..lineTo(w - borderRadius, h)
      ..arcToPoint(Offset(w, h - borderRadius), radius: Radius.circular(borderRadius))
      ..lineTo(w, h - cornerLength);
    canvas.drawPath(pathBR, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
