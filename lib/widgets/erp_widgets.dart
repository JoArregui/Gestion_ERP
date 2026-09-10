import 'package:flutter/material.dart';
import '../core/erp_theme.dart';

// Logo "E" idéntico a web: w-10 h-10 bg-blue-600 rounded-xl
class ErpLogo extends StatelessWidget {
  const ErpLogo({super.key, this.size = 40, this.fontSize = 18});
  final double size;
  final double fontSize;
  @override
  Widget build(BuildContext context) => Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          color: ErpColors.blue600,
          borderRadius: BorderRadius.circular(12),
          boxShadow: [BoxShadow(color: ErpColors.blue600.withOpacity(0.2), blurRadius: 12, offset: const Offset(0, 4))],
          border: Border.all(color: Colors.white.withOpacity(0.2)),
        ),
        child: Center(child: Text('E', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w900, fontSize: fontSize))),
      );
}

// KPI Card alineado con DashboardStats.razor + Home.razor
class KpiCard extends StatelessWidget {
  const KpiCard({super.key, required this.label, required this.value, required this.subtitle, this.accent = ErpColors.blue600, this.icon});
  final String label;
  final String value;
  final String subtitle;
  final Color accent;
  final IconData? icon;
  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border(left: BorderSide(color: accent, width: 5)),
          boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.04), blurRadius: 12, offset: const Offset(0, 4))],
        ),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            Expanded(child: Text(label.toUpperCase(), style: ErpDecor.cardLabel)),
            if (icon != null) Icon(icon, size: 16, color: accent),
          ]),
          const SizedBox(height: 8),
          Text(value, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w900, letterSpacing: -1, color: ErpColors.slate900, fontFamily: 'JetBrainsMono')),
          const SizedBox(height: 4),
          Text(subtitle, style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w600, color: ErpColors.slate400)),
        ]),
      );
}

// Card blanca genérica con título uppercase
class ErpCard extends StatelessWidget {
  const ErpCard({super.key, required this.title, required this.child, this.action});
  final String title;
  final Widget child;
  final Widget? action;
  @override
  Widget build(BuildContext context) => Container(
        decoration: ErpDecor.card(),
        padding: const EdgeInsets.all(18),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            Expanded(child: Text(title.toUpperCase(), style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w900, letterSpacing: 1.2, color: ErpColors.slate900))),
            if (action != null) action!,
          ]),
          const Divider(height: 20, color: ErpColors.slate100),
          child,
        ]),
      );
}

// Estado vacío / carga / error (criterio responsive)
class ErpState extends StatelessWidget {
  const ErpState.loading({super.key, this.message = 'Sincronizando flujo de datos...'});
  const ErpState.empty({super.key, this.message = 'Sin datos registrados'});
  const ErpState.error({super.key, this.message = 'Error de conexión con el servidor'});
  final String message;
  @override
  Widget build(BuildContext context) {
    final isLoading = message.contains('Sincronizando');
    return Container(
      padding: const EdgeInsets.all(24),
      decoration: ErpDecor.card(),
      child: Column(children: [
        if (isLoading) const CircularProgressIndicator(color: ErpColors.blue600) else const Icon(Icons.inbox_rounded, size: 36, color: ErpColors.slate400),
        const SizedBox(height: 12),
        Text(message, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: ErpColors.slate400), textAlign: TextAlign.center),
      ]),
    );
  }
}

// Item de documento en formato tarjeta móvil (tablas -> tarjetas)
class DocumentoCard extends StatelessWidget {
  const DocumentoCard({super.key, required this.ref, required this.entidad, required this.tipo, required this.importe, required this.estado});
  final String ref;
  final String entidad;
  final String tipo;
  final String importe;
  final String estado;
  @override
  Widget build(BuildContext context) {
    final isEmitido = estado.toLowerCase().contains('emitido') || estado.toLowerCase().contains('pagado');
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(14),
      decoration: ErpDecor.card(),
      child: Row(children: [
        Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(ref, style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 13, color: ErpColors.slate900)),
          Text('$entidad · $tipo', style: const TextStyle(fontSize: 11, color: ErpColors.slate400)),
        ])),
        Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
          Text(importe, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13, color: ErpColors.slate900)),
          const SizedBox(height: 4),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
            decoration: BoxDecoration(color: isEmitido ? const Color(0xFFDCFCE7) : const Color(0xFFFEF3C7), borderRadius: BorderRadius.circular(20)),
            child: Text(estado.toUpperCase(), style: TextStyle(fontSize: 8, fontWeight: FontWeight.w900, color: isEmitido ? const Color(0xFF15803D) : const Color(0xFF92400E))),
          ),
        ]),
      ]),
    );
  }
}

// Botón 44px táctil
class ErpPrimaryButton extends StatelessWidget {
  const ErpPrimaryButton({super.key, required this.label, required this.onPressed, this.icon, this.busy = false});
  final String label;
  final VoidCallback? onPressed;
  final IconData? icon;
  final bool busy;
  @override
  Widget build(BuildContext context) => SizedBox(
        height: 48,
        child: FilledButton(
          onPressed: busy ? null : onPressed,
          child: busy
              ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
              : Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                  if (icon != null) ...[Icon(icon, size: 16), const SizedBox(width: 8)],
                  Text(label.toUpperCase(), style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 11, letterSpacing: 1.2)),
                ]),
        ),
      );
}
