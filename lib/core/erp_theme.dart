import 'package:flutter/material.dart';

// Design tokens extraídos de ERP.Web (Tailwind + index.html)
// Web: bg-[#020617] slate-950, sidebar/header bg-[#0f172a] slate-900, primary blue-600 #2563eb
class ErpColors {
  static const slate950 = Color(0xFF020617);
  static const slate900 = Color(0xFF0F172A);
  static const slate800 = Color(0xFF1E293B);
  static const slate700 = Color(0xFF334155);
  static const slate400 = Color(0xFF94A3B8);
  static const slate200 = Color(0xFFE2E8F0);
  static const slate100 = Color(0xFFF1F5F9);
  static const slate50 = Color(0xFFF8FAFC);
  static const blue600 = Color(0xFF2563EB);
  static const blue500 = Color(0xFF3B82F6);
  static const rose500 = Color(0xFFE11D48);
  static const rose50 = Color(0xFFFFF1F2);
  static const emerald500 = Color(0xFF10B981);
  static const amber500 = Color(0xFFF59E0B);
  static const white = Color(0xFFFFFFFF);
}

class ErpTheme {
  static ThemeData light() {
    final scheme = ColorScheme.fromSeed(
      seedColor: ErpColors.blue600,
      brightness: Brightness.light,
      primary: ErpColors.blue600,
      surface: ErpColors.slate50,
    );
    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: ErpColors.slate50,
      appBarTheme: const AppBarTheme(
        backgroundColor: ErpColors.slate900,
        foregroundColor: Colors.white,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          fontFamily: 'Inter',
          fontSize: 14,
          fontWeight: FontWeight.w900,
          letterSpacing: 1.2,
          color: Colors.white,
        ),
      ),
      textTheme: const TextTheme(
        headlineLarge: TextStyle(fontFamily: 'Inter', fontWeight: FontWeight.w900, letterSpacing: -1.5, color: ErpColors.slate900),
        headlineMedium: TextStyle(fontFamily: 'Inter', fontWeight: FontWeight.w900, letterSpacing: -1.2, color: ErpColors.slate900),
        titleMedium: TextStyle(fontFamily: 'Inter', fontWeight: FontWeight.w800, fontSize: 10, letterSpacing: 1.6, color: ErpColors.slate400),
        bodyMedium: TextStyle(fontFamily: 'Inter', fontSize: 13, color: ErpColors.slate800),
        labelLarge: TextStyle(fontFamily: 'Inter', fontWeight: FontWeight.w800, fontSize: 11, letterSpacing: 1.2),
      ),
      cardTheme: CardThemeData(
        color: Colors.white,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(20),
          side: const BorderSide(color: Color(0xFFE2E8F0), width: 1),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: const Color(0xFFF1F5F9),
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: const BorderSide(color: Color(0xFFE2E8F0))),
        enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: const BorderSide(color: Color(0xFFE2E8F0))),
        focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: const BorderSide(color: ErpColors.blue600, width: 2)),
        labelStyle: const TextStyle(fontSize: 10, fontWeight: FontWeight.w800, letterSpacing: 1.2, color: ErpColors.slate400),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: ErpColors.blue600,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(48),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          textStyle: const TextStyle(fontWeight: FontWeight.w900, fontSize: 11, letterSpacing: 1.6),
        ),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: Colors.white,
        indicatorColor: ErpColors.blue600.withOpacity(0.12),
        labelTextStyle: WidgetStateProperty.all(const TextStyle(fontSize: 10, fontWeight: FontWeight.w800, letterSpacing: 0.8)),
      ),
      navigationRailTheme: const NavigationRailThemeData(
        backgroundColor: ErpColors.slate900,
        selectedIconTheme: IconThemeData(color: Colors.white),
        unselectedIconTheme: IconThemeData(color: ErpColors.slate400),
        selectedLabelTextStyle: TextStyle(color: Colors.white, fontWeight: FontWeight.w800, fontSize: 10, letterSpacing: 0.8),
        unselectedLabelTextStyle: TextStyle(color: ErpColors.slate400, fontWeight: FontWeight.w700, fontSize: 10, letterSpacing: 0.8),
      ),
    );
  }
}

// Helpers visuales web
class ErpDecor {
  static BoxDecoration card({Color? borderColor}) => BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: borderColor ?? const Color(0xFFE2E8F0)),
        boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.04), blurRadius: 12, offset: const Offset(0, 4))],
      );

  static BoxDecoration kpiBlue = BoxDecoration(
    color: Colors.white,
    borderRadius: BorderRadius.circular(24),
    border: const Border(left: BorderSide(color: ErpColors.blue600, width: 6)),
  );
  static BoxDecoration kpiRed = BoxDecoration(
    color: Colors.white,
    borderRadius: BorderRadius.circular(24),
    border: const Border(left: BorderSide(color: ErpColors.rose500, width: 6)),
  );
  static BoxDecoration kpiGreen = BoxDecoration(
    color: Colors.white,
    borderRadius: BorderRadius.circular(24),
    border: const Border(left: BorderSide(color: ErpColors.emerald500, width: 6)),
  );

  static const sectionLabel = TextStyle(fontSize: 9, fontWeight: FontWeight.w900, letterSpacing: 2.4, color: ErpColors.slate400);
  static const cardLabel = TextStyle(fontSize: 10, fontWeight: FontWeight.w900, letterSpacing: 1.6, color: ErpColors.slate400);
  static const monoBold = TextStyle(fontFamily: 'JetBrainsMono', fontWeight: FontWeight.w800, color: ErpColors.slate900);
}
