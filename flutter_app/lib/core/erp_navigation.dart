import 'package:flutter/material.dart';

// Estructura 1:1 con ERP.Web/Layout/NavMenu.razor
class NavSection {
  final String title;
  final IconData icon;
  final String route;
  final List<NavItem> children;
  const NavSection({required this.title, required this.icon, required this.route, this.children = const []});
}

class NavItem {
  final String title;
  final IconData icon;
  final String route;
  const NavItem({required this.title, required this.icon, required this.route});
}

// Definición completa alineada con web
class ErpNavigation {
  static const sections = [
    // Home directo
    NavSection(title: 'Escritorio Principal', icon: Icons.home_rounded, route: '/'),

    NavSection(title: 'General', icon: Icons.dashboard_rounded, route: 'general', children: [
      NavItem(title: 'Insights / Dashboard', icon: Icons.pie_chart_rounded, route: '/dashboard'),
      NavItem(title: 'Agenda', icon: Icons.calendar_today_rounded, route: '/agenda'),
      NavItem(title: 'Comunicaciones', icon: Icons.chat_bubble_rounded, route: '/llamadas'),
    ]),

    NavSection(title: 'Operativa y Ventas', icon: Icons.point_of_sale_rounded, route: 'ventas', children: [
      NavItem(title: 'Nueva Venta (TPV)', icon: Icons.calculate_rounded, route: '/ventas/nueva'),
      NavItem(title: 'Cierre de Caja', icon: Icons.lock_rounded, route: '/cierre-caja'),
      NavItem(title: 'Historial Cierres', icon: Icons.history_rounded, route: '/ventas/cierres'),
      NavItem(title: 'Presupuestos', icon: Icons.description_rounded, route: '/ventas/presupuestos'),
      NavItem(title: 'Albaranes', icon: Icons.local_shipping_rounded, route: '/ventas/albaranes'),
      NavItem(title: 'Facturas de Venta', icon: Icons.receipt_long_rounded, route: '/ventas/facturas'),
      NavItem(title: 'Nueva Factura / Doc', icon: Icons.note_add_rounded, route: '/ventas/nueva-factura'),
      NavItem(title: 'Editor Documento', icon: Icons.edit_document, route: '/ventas/editor'),
      NavItem(title: 'Listado Documentos', icon: Icons.folder_rounded, route: '/ventas/documentos'),
    ]),

    NavSection(title: 'Tesorería', icon: Icons.account_balance_rounded, route: 'tesoreria', children: [
      NavItem(title: 'Registrar Gasto', icon: Icons.remove_circle_rounded, route: '/tesoreria/gasto'),
      NavItem(title: 'Vencimientos', icon: Icons.event_available_rounded, route: '/vencimientos'),
    ]),

    NavSection(title: 'Compras', icon: Icons.shopping_cart_rounded, route: 'compras', children: [
      NavItem(title: 'Panel de Compras', icon: Icons.shopping_cart_rounded, route: '/compras'),
      NavItem(title: 'Nuevo Pedido', icon: Icons.add_shopping_cart_rounded, route: '/compras/nuevo-pedido'),
      NavItem(title: 'Recepción Pedidos', icon: Icons.move_to_inbox_rounded, route: '/compras/recepcion'),
    ]),

    NavSection(title: 'Recursos Humanos', icon: Icons.people_rounded, route: 'rrhh', children: [
      NavItem(title: 'Kiosko Fichajes', icon: Icons.timer_rounded, route: '/rrhh/kiosko'),
      NavItem(title: 'Control Horario', icon: Icons.schedule_rounded, route: '/rrhh/control-horario'),
      NavItem(title: 'Gestión Empleados', icon: Icons.badge_rounded, route: '/rrhh/empleados'),
      NavItem(title: 'Nóminas', icon: Icons.payments_rounded, route: '/rrhh/nominas'),
    ]),

    NavSection(title: 'Stock y Logística', icon: Icons.warehouse_rounded, route: 'stock', children: [
      NavItem(title: 'Ajuste de Inventario', icon: Icons.tune_rounded, route: '/stock/ajuste'),
      NavItem(title: 'Valoración Almacén', icon: Icons.assessment_rounded, route: '/stock/valoracion'),
      NavItem(title: 'Impresión Etiquetas', icon: Icons.qr_code_rounded, route: '/stock/etiquetas'),
      NavItem(title: 'Informe Auditoría', icon: Icons.fact_check_rounded, route: '/stock/auditoria'),
    ]),

    NavSection(title: 'Maestros', icon: Icons.category_rounded, route: 'maestros', children: [
      NavItem(title: 'Artículos', icon: Icons.inventory_2_rounded, route: '/maestros/articulos'),
      NavItem(title: 'Familias', icon: Icons.label_rounded, route: '/maestros/familias'),
      NavItem(title: 'Clientes', icon: Icons.person_rounded, route: '/maestros/clientes'),
      NavItem(title: 'Proveedores', icon: Icons.local_shipping_rounded, route: '/maestros/proveedores'),
      NavItem(title: 'Acreedores', icon: Icons.business_rounded, route: '/maestros/acreedores'),
    ]),

    NavSection(title: 'Informes', icon: Icons.bar_chart_rounded, route: 'informes', children: [
      NavItem(title: 'Informe Rentabilidad', icon: Icons.trending_up_rounded, route: '/informes/rentabilidad'),
    ]),

    NavSection(title: 'Legal', icon: Icons.gavel_rounded, route: 'legal', children: [
      NavItem(title: 'Bancario SEPA', icon: Icons.account_balance_rounded, route: '/bancario'),
      NavItem(title: 'Contabilidad PGC', icon: Icons.menu_book_rounded, route: '/contabilidad'),
      NavItem(title: 'Firma Digital eIDAS', icon: Icons.verified_rounded, route: '/firmadigital'),
      NavItem(title: 'Fiscal IVA', icon: Icons.calculate_rounded, route: '/fiscal'),
      NavItem(title: 'Trazabilidad Lotes', icon: Icons.qr_code_scanner_rounded, route: '/trazabilidad'),
      NavItem(title: 'Verifactu', icon: Icons.shield_rounded, route: '/verifactu'),
      NavItem(title: 'IVA/IGIC', icon: Icons.percent_rounded, route: '/iva'),
      NavItem(title: 'Facturae FACe', icon: Icons.code_rounded, route: '/facturae'),
    ]),

    NavSection(title: 'Configuración Sistema', icon: Icons.settings_rounded, route: 'config', children: [
      NavItem(title: 'Empresas', icon: Icons.business_rounded, route: '/configuracion/empresas'),
      NavItem(title: 'Seguridad', icon: Icons.security_rounded, route: '/configuracion/seguridad'),
      NavItem(title: 'Permisos y Roles', icon: Icons.admin_panel_settings_rounded, route: '/configuracion/seguridad/permisos'),
      NavItem(title: 'Configuración Email', icon: Icons.mail_rounded, route: '/configuracion/email'),
    ]),
  ];

  // 5 destinos principales para BottomNavigation (criterio 5 items máx)
  static const bottomItems = [
    (Icons.home_rounded, 'Inicio', '/'),
    (Icons.pie_chart_rounded, 'Dashboard', '/dashboard'),
    (Icons.point_of_sale_rounded, 'Ventas', '/ventas/nueva'),
    (Icons.shopping_cart_rounded, 'Compras', '/compras'),
    (Icons.more_horiz_rounded, 'Más', 'more'),
  ];
}
