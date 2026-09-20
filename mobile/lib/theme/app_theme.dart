import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

// ─────────────────────────────────────────────────────────────────────────────
// FuelTrack Design System — "Panel de instrumentación"
// Tokens alineados 1:1 con el CSS web (index.css @theme).
// No hardcodear colores en widgets: usar estas constantes.
// ─────────────────────────────────────────────────────────────────────────────
class AppColors {
  // Primario  → --color-tanque
  static const primary     = Color(0xFF16333A);
  static const primaryDark = Color(0xFF0D1F23);
  static const primaryLight = Color(0xFFE4ECED);

  // Acento    → --color-medidor (SOLO alertas reales, no decorativo)
  static const accent      = Color(0xFFE29B2E);
  static const accentLight = Color(0xFFFDF3DC);

  // Superficie / Fondo → --color-fondo
  static const background  = Color(0xFFF7F8F6);
  static const surface     = Colors.white;

  // Bordes   → acero/20
  static const cardBorder  = Color(0xFFD0D8DB);

  // Texto    → --color-tinta / --color-acero
  static const textPrimary   = Color(0xFF12181A);
  static const textSecondary = Color(0xFF4A5A63);
  static const textMuted     = Color(0xFF7A8A93);

  // Semánticos
  static const info           = Color(0xFF2F6F8F);
  static const infoBg         = Color(0xFFDDEEF5);

  static const statusCreated  = Color(0xFF2F6F8F); // info/blue
  static const statusCreatedBg = Color(0xFFDDEEF5);

  static const statusConsumed  = Color(0xFF2E7D5B); // exito
  static const statusConsumedBg = Color(0xFFD4EDDF);

  static const statusWarning   = Color(0xFFC97A1A); // advertencia
  static const statusWarningBg = Color(0xFFFEF0D6);

  static const statusExpired   = Color(0xFFC1432B); // peligro
  static const statusExpiredBg = Color(0xFFFBDED9);

  static const statusAnulado   = Color(0xFF16333A); // tanque/purple
  static const statusAnuladoBg = Color(0xFFE4ECED);

  // ── helpers de badge ──────────────────────────────────────────────────────
  static ({Color fg, Color bg}) getStatusColors(String state) {
    return switch (state) {
      'Consumido'        => (fg: statusConsumed,  bg: statusConsumedBg),
      'ProximoAVencer'   => (fg: statusWarning,   bg: statusWarningBg),
      'Próximo a vencer' => (fg: statusWarning,   bg: statusWarningBg),
      'Vencido'          => (fg: statusExpired,   bg: statusExpiredBg),
      'Anulado'          => (fg: statusAnulado,   bg: statusAnuladoBg),
      'Enviado'          => (fg: statusConsumed,  bg: statusConsumedBg),
      _                  => (fg: statusCreated,   bg: statusCreatedBg),
    };
  }

  // Legacy helpers (usados en dispatch_screen)
  static Color getStatusColor(String state) => getStatusColors(state).fg;
  static Color getStatusBgColor(String state) => getStatusColors(state).bg;
}

class AppTheme {
  // Tipografía principal: Public Sans (web: font-sans)
  static TextTheme _buildTextTheme() {
    final base = GoogleFonts.publicSansTextTheme();
    return base.copyWith(
      displayLarge:  base.displayLarge?.copyWith(color: AppColors.textPrimary),
      displayMedium: base.displayMedium?.copyWith(color: AppColors.textPrimary),
      displaySmall:  base.displaySmall?.copyWith(color: AppColors.textPrimary),
      headlineLarge: base.headlineLarge?.copyWith(color: AppColors.textPrimary, fontWeight: FontWeight.w700),
      headlineMedium:base.headlineMedium?.copyWith(color: AppColors.textPrimary, fontWeight: FontWeight.w600),
      headlineSmall: base.headlineSmall?.copyWith(color: AppColors.textPrimary, fontWeight: FontWeight.w600),
      titleLarge:    base.titleLarge?.copyWith(color: AppColors.textPrimary, fontWeight: FontWeight.w600),
      titleMedium:   base.titleMedium?.copyWith(color: AppColors.textPrimary, fontWeight: FontWeight.w600),
      titleSmall:    base.titleSmall?.copyWith(color: AppColors.textSecondary, fontWeight: FontWeight.w500),
      bodyLarge:     base.bodyLarge?.copyWith(color: AppColors.textPrimary),
      bodyMedium:    base.bodyMedium?.copyWith(color: AppColors.textPrimary),
      bodySmall:     base.bodySmall?.copyWith(color: AppColors.textSecondary),
      labelLarge:    base.labelLarge?.copyWith(color: AppColors.textPrimary, fontWeight: FontWeight.w600),
      labelMedium:   base.labelMedium?.copyWith(color: AppColors.textSecondary),
      labelSmall:    base.labelSmall?.copyWith(color: AppColors.textMuted),
    );
  }

  static ThemeData get lightTheme {
    final textTheme = _buildTextTheme();
    return ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: AppColors.primary,
        primary: AppColors.primary,
        onPrimary: Colors.white,
        secondary: AppColors.accent,
        surface: AppColors.surface,
        onSurface: AppColors.textPrimary,
        error: AppColors.statusExpired,
      ),
      textTheme: textTheme,
      primaryTextTheme: textTheme,
      scaffoldBackgroundColor: AppColors.background,

      // AppBar — igual que el sidebar/header oscuro de la web
      appBarTheme: AppBarTheme(
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: GoogleFonts.publicSans(
          color: Colors.white,
          fontSize: 17,
          fontWeight: FontWeight.w600,
          letterSpacing: 0.1,
        ),
        iconTheme: const IconThemeData(color: Colors.white),
        actionsIconTheme: const IconThemeData(color: Colors.white),
      ),

      // Cards — borde acero/20, sin radio grande (rounded-sm = 4px)
      cardTheme: CardThemeData(
        color: Colors.white,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(4),
          side: const BorderSide(color: AppColors.cardBorder, width: 1),
        ),
        margin: const EdgeInsets.symmetric(vertical: 6),
        shadowColor: AppColors.textSecondary.withValues(alpha: 0.08),
      ),

      // Inputs — borde acero/40, foco tanque
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: AppColors.cardBorder),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: BorderSide(color: AppColors.textSecondary.withValues(alpha: 0.4)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: AppColors.primary, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: AppColors.statusExpired),
        ),
        labelStyle: GoogleFonts.publicSans(color: AppColors.textSecondary, fontSize: 14, fontWeight: FontWeight.w500),
        hintStyle: GoogleFonts.publicSans(color: AppColors.textMuted, fontSize: 14),
      ),

      // FilledButton — fondo tanque, esquinas 6px, alto táctil 52px
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(6)),
          textStyle: GoogleFonts.publicSans(fontSize: 14, fontWeight: FontWeight.w600),
          elevation: 0,
        ),
      ),

      // OutlinedButton
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.primary,
          minimumSize: const Size.fromHeight(48),
          side: const BorderSide(color: AppColors.primary, width: 1.5),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(6)),
          textStyle: GoogleFonts.publicSans(fontSize: 14, fontWeight: FontWeight.w600),
        ),
      ),

      // NavigationBar
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: Colors.white,
        elevation: 0,
        shadowColor: Colors.transparent,
        surfaceTintColor: Colors.transparent,
        indicatorColor: AppColors.primaryLight,
        labelTextStyle: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return GoogleFonts.publicSans(fontSize: 11, fontWeight: FontWeight.w600, color: AppColors.primary);
          }
          return GoogleFonts.publicSans(fontSize: 11, fontWeight: FontWeight.w500, color: AppColors.textSecondary);
        }),
      ),

      // Divider
      dividerTheme: const DividerThemeData(
        color: AppColors.cardBorder,
        space: 1,
        thickness: 1,
      ),

      // Chip — para filtros
      chipTheme: ChipThemeData(
        backgroundColor: AppColors.background,
        selectedColor: AppColors.primary,
        labelStyle: GoogleFonts.publicSans(fontSize: 12, fontWeight: FontWeight.w500),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(4),
          side: const BorderSide(color: AppColors.cardBorder),
        ),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      ),
    );
  }

  // ── Fuente monoespaciada para galones, códigos, folios ────────────────────
  // Uso: style: AppTheme.mono(fontSize: 14, fontWeight: FontWeight.w600)
  static TextStyle mono({
    double fontSize = 14,
    FontWeight fontWeight = FontWeight.w600,
    Color color = AppColors.textPrimary,
  }) => GoogleFonts.ibmPlexMono(fontSize: fontSize, fontWeight: fontWeight, color: color);
}

