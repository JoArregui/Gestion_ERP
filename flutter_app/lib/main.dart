import 'package:flutter/material.dart';

import 'core/erp_theme.dart';
import 'core/erp_api.dart';
import 'core/erp_navigation.dart';
import 'widgets/erp_widgets.dart';

// Entry
void main() => runApp(ErpApp(auth: AuthService()));

// Root con restauración de sesión
class ErpApp extends StatefulWidget {
  const ErpApp({super.key, required this.auth});
  final AuthService auth;
  @override
  State<ErpApp> createState() => _ErpAppState();
}

class _ErpAppState extends State<ErpApp> {
  String? token;
  bool loading = true;

  @override
  void initState() {
    super.initState();
    _restore();
  }

  Future<void> _restore() async {
    token = await widget.auth.restoreToken();
    if (mounted) setState(() => loading = false);
  }

  @override
  Widget build(BuildContext context) => MaterialApp(
        debugShowCheckedModeBanner: false,
        title: 'ERP.NET',
        theme: ErpTheme.light(),
        home: loading
            ? const Scaffold(backgroundColor: ErpColors.slate950, body: Center(child: CircularProgressIndicator(color: Colors.white)))
            : token == null
                ? LoginPage(auth: widget.auth, onLogin: (v) => setState(() => token = v))
                : AppShell(token: token!, auth: widget.auth, onLogout: () => setState(() => token = null)),
      );
}

// Login alineado con ERP.Web/Pages/Login.razor
class LoginPage extends StatefulWidget {
  const LoginPage({super.key, required this.auth, required this.onLogin});
  final AuthService auth;
  final ValueChanged<String> onLogin;
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final email = TextEditingController(text: '');
  final password = TextEditingController(text: '');
  bool busy = false;
  String? error;

  Future<void> submit() async {
    if (email.text.isEmpty || password.text.isEmpty) {
      setState(() => error = 'Introduce el correo y la contraseña.');
      return;
    }
    setState(() { busy = true; error = null; });
    try {
      final t = await widget.auth.login(email.text.trim(), password.text);
      widget.onLogin(t);
    } catch (e) {
      setState(() => error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: ErpColors.slate950,
        body: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Container(
                padding: const EdgeInsets.all(28),
                decoration: BoxDecoration(
                  color: ErpColors.slate900,
                  borderRadius: BorderRadius.circular(24),
                  border: Border.all(color: const Color(0xFF1E293B)),
                  boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.5), blurRadius: 24, offset: const Offset(0, 12))],
                ),
                child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                  Center(
                    child: Column(children: [
                      const ErpLogo(size: 64, fontSize: 28),
                      const SizedBox(height: 16),
                      RichText(
                          text: const TextSpan(children: [
                        TextSpan(text: 'BIENVENIDO AL ', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w900, fontSize: 18, letterSpacing: -0.8)),
                        TextSpan(text: 'ERP', style: TextStyle(color: ErpColors.blue600, fontWeight: FontWeight.w900, fontSize: 18, letterSpacing: -0.8)),
                      ])),
                      const SizedBox(height: 4),
                      const Text('PYMES v2026', style: TextStyle(color: ErpColors.slate400, fontSize: 10, fontWeight: FontWeight.w800, letterSpacing: 2.4)),
                    ]),
                  ),
                  const SizedBox(height: 28),
                  TextField(
                    controller: email,
                    keyboardType: TextInputType.emailAddress,
                    style: const TextStyle(color: Colors.white),
                    decoration: const InputDecoration(
                      labelText: 'CORREO ELECTRÓNICO',
                      hintText: 'usuario@empresa.com',
                      prefixIcon: Icon(Icons.mail_outline_rounded, color: ErpColors.slate400),
                      hintStyle: TextStyle(color: ErpColors.slate700),
                    ),
                  ),
                  const SizedBox(height: 14),
                  TextField(
                    controller: password,
                    obscureText: true,
                    style: const TextStyle(color: Colors.white),
                    onSubmitted: (_) => submit(),
                    decoration: const InputDecoration(
                      labelText: 'CONTRASEÑA',
                      hintText: '••••••••',
                      prefixIcon: Icon(Icons.lock_outline_rounded, color: ErpColors.slate400),
                      hintStyle: TextStyle(color: ErpColors.slate700),
                    ),
                  ),
                  if (error != null)
                    Container(
                      margin: const EdgeInsets.only(top: 14),
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(color: ErpColors.rose500.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(12), border: Border.all(color: ErpColors.rose500.withValues(alpha: 0.2))),
                      child: Row(children: [
                        const Icon(Icons.warning_rounded, size: 16, color: ErpColors.rose500),
                        const SizedBox(width: 8),
                        Expanded(child: Text(error!, style: const TextStyle(color: ErpColors.rose500, fontSize: 11, fontWeight: FontWeight.w700))),
                      ]),
                    ),
                  const SizedBox(height: 20),
                  ErpPrimaryButton(label: busy ? 'Verificando...' : 'Entrar al Sistema', onPressed: submit, busy: busy, icon: Icons.arrow_forward_rounded),
                  const SizedBox(height: 12),
                  Center(
                    child: TextButton(
                      onPressed: () {},
                      child: const Text('¿Has olvidado tu contraseña?', style: TextStyle(color: ErpColors.blue500, fontSize: 11, fontWeight: FontWeight.w700)),
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text('© 2026 ERP Industrial. Todos los derechos reservados.', style: TextStyle(color: ErpColors.slate400, fontSize: 9), textAlign: TextAlign.center),
                ]),
              ),
            ),
          ),
        ),
      );
}

// Shell responsive: Rail en ancho >=800, Drawer + BottomBar en móvil
class AppShell extends StatefulWidget {
  const AppShell({super.key, required this.token, required this.auth, required this.onLogout});
  final String token;
  final AuthService auth;
  final VoidCallback onLogout;
  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  String currentRoute = '/';
  final GlobalKey<ScaffoldState> scaffoldKey = GlobalKey();

  // Mapa de rutas a páginas (funcional + estético idéntico a web)
  Widget _pageFor(String route) {
    switch (route) {
      case '/':
        return HomePage(token: widget.token);
      case '/dashboard':
        return DashboardPage(token: widget.token);
      default:
        // Genérico para módulos aún no conectados 1:1, pero con misma estética
        final item = _findNavItem(route);
        return ModulePlaceholder(title: item?.title ?? route, subtitle: 'Módulo ERP.NET', icon: item?.icon ?? Icons.widgets_rounded);
    }
  }

  NavItem? _findNavItem(String route) {
    for (final s in ErpNavigation.sections) {
      for (final c in s.children) {
        if (c.route == route) return c;
      }
      if (s.route == route) return NavItem(title: s.title, icon: s.icon, route: s.route);
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    final wide = MediaQuery.sizeOf(context).width >= 800;
    final page = _pageFor(currentRoute);

    final bottomIndex = (() {
      final map = ['/', '/dashboard', '/ventas/nueva', '/compras', 'more'];
      final idx = map.indexOf(currentRoute);
      if (idx != -1) return idx;
      if (currentRoute.startsWith('/ventas')) return 2;
      if (currentRoute.startsWith('/compras')) return 3;
      return 4;
    })();

    return Scaffold(
      key: scaffoldKey,
      appBar: AppBar(
        backgroundColor: ErpColors.slate900,
        title: Row(children: [
          const ErpLogo(size: 32, fontSize: 14),
          const SizedBox(width: 12),
          Column(crossAxisAlignment: CrossAxisAlignment.start, children: const [
            Text('SISTEMA ERP', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w900, letterSpacing: 0.8, color: Colors.white)),
            Text('ERP PARA PYMES', style: TextStyle(fontSize: 7, fontWeight: FontWeight.w800, letterSpacing: 2, color: ErpColors.slate400)),
          ]),
        ]),
        actions: [
          IconButton(onPressed: () {}, icon: const Icon(Icons.notifications_none_rounded, color: ErpColors.slate400)),
          PopupMenuButton<String>(
            icon: const Icon(Icons.account_circle_rounded, color: Colors.white),
            onSelected: (v) async {
              if (v == 'logout') {
                await widget.auth.logout();
                widget.onLogout();
              }
            },
            itemBuilder: (_) => const [
              PopupMenuItem(value: 'logout', child: Text('Cerrar Sesión')),
            ],
          ),
          const SizedBox(width: 8),
        ],
      ),
      drawer: wide ? null : ErpDrawer(currentRoute: currentRoute, onSelect: (r) {
        Navigator.pop(context);
        setState(() => currentRoute = r);
      }, onLogout: () async {
        await widget.auth.logout();
        widget.onLogout();
      }),
      body: Row(children: [
        if (wide)
          ErpRail(currentRoute: currentRoute, onSelect: (r) => setState(() => currentRoute = r), onLogout: () async {
            await widget.auth.logout();
            widget.onLogout();
          }),
        Expanded(
          child: Container(
            color: ErpColors.slate50,
            child: SingleChildScrollView(padding: const EdgeInsets.all(16), child: page),
          ),
        ),
      ]),
      bottomNavigationBar: wide
          ? null
          : NavigationBar(
              selectedIndex: bottomIndex.clamp(0, 4),
              onDestinationSelected: (i) {
                final routes = ['/', '/dashboard', '/ventas/nueva', '/compras', 'more'];
                final r = routes[i];
                if (r == 'more') {
                  scaffoldKey.currentState?.openDrawer();
                } else {
                  setState(() => currentRoute = r);
                }
              },
              destinations: const [
                NavigationDestination(icon: Icon(Icons.home_outlined), selectedIcon: Icon(Icons.home_rounded), label: 'Inicio'),
                NavigationDestination(icon: Icon(Icons.pie_chart_outline), selectedIcon: Icon(Icons.pie_chart_rounded), label: 'Dashboard'),
                NavigationDestination(icon: Icon(Icons.point_of_sale_outlined), selectedIcon: Icon(Icons.point_of_sale_rounded), label: 'Ventas'),
                NavigationDestination(icon: Icon(Icons.shopping_cart_outlined), selectedIcon: Icon(Icons.shopping_cart_rounded), label: 'Compras'),
                NavigationDestination(icon: Icon(Icons.more_horiz), selectedIcon: Icon(Icons.more_horiz_rounded), label: 'Más'),
              ],
            ),
    );
  }
}

// Drawer oscuro idéntico a NavMenu.razor: bg-[#0f172a] text-slate-400, secciones acordeón
class ErpDrawer extends StatefulWidget {
  const ErpDrawer({super.key, required this.currentRoute, required this.onSelect, required this.onLogout});
  final String currentRoute;
  final ValueChanged<String> onSelect;
  final VoidCallback onLogout;
  @override
  State<ErpDrawer> createState() => _ErpDrawerState();
}

class _ErpDrawerState extends State<ErpDrawer> {
  String? expanded;

  @override
  Widget build(BuildContext context) => Drawer(
        backgroundColor: ErpColors.slate900,
        child: SafeArea(
          child: Column(children: [
            Padding(
              padding: const EdgeInsets.all(24),
              child: Row(children: [
                const ErpLogo(size: 40, fontSize: 16),
                const SizedBox(width: 12),
                Column(crossAxisAlignment: CrossAxisAlignment.start, children: const [
                  Text('SISTEMA ERP', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w900, fontSize: 14, letterSpacing: -0.5)),
                  Text('ERP PARA PYMES', style: TextStyle(color: ErpColors.slate400, fontSize: 8, fontWeight: FontWeight.w800, letterSpacing: 2.4)),
                ]),
              ]),
            ),
            Expanded(
              child: ListView(padding: const EdgeInsets.symmetric(horizontal: 12), children: [
                _drawerLink('Escritorio Principal', Icons.home_rounded, '/', widget.currentRoute == '/'),
                for (final section in ErpNavigation.sections.where((s) => s.children.isNotEmpty))
                  _section(section),
              ]),
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(children: [
                _drawerLink('Ajustes Generales', Icons.settings_rounded, '/configuracion', false, muted: true),
                const SizedBox(height: 8),
                SizedBox(
                  height: 44,
                  width: double.infinity,
                  child: OutlinedButton(
                    style: OutlinedButton.styleFrom(foregroundColor: ErpColors.rose500, side: BorderSide(color: ErpColors.rose500.withValues(alpha: 0.2)), backgroundColor: ErpColors.rose500.withValues(alpha: 0.08)),
                    onPressed: widget.onLogout,
                    child: const Text('CERRAR SESIÓN', style: TextStyle(fontWeight: FontWeight.w900, fontSize: 10, letterSpacing: 1.2)),
                  ),
                ),
              ]),
            ),
          ]),
        ),
      );

  Widget _drawerLink(String title, IconData icon, String route, bool active, {bool muted = false}) => Container(
        margin: const EdgeInsets.only(bottom: 4),
        decoration: BoxDecoration(color: active ? ErpColors.blue600 : Colors.transparent, borderRadius: BorderRadius.circular(12)),
        child: ListTile(
          dense: true,
          minVerticalPadding: 10,
          leading: Icon(icon, size: 18, color: active ? Colors.white : ErpColors.slate400),
          title: Text(title.toUpperCase(), style: TextStyle(color: active ? Colors.white : (muted ? ErpColors.slate400 : ErpColors.slate400), fontWeight: FontWeight.w900, fontSize: 10, letterSpacing: 1)),
          onTap: () => widget.onSelect(route),
        ),
      );

  Widget _section(NavSection section) {
    final isOpen = expanded == section.route;
    final isLegal = section.title == 'Legal';
    return Column(children: [
      ListTile(
        dense: true,
        title: Text(section.title.toUpperCase(),
            style: TextStyle(color: isLegal ? const Color(0xFFFBBF24) : ErpColors.slate400, fontWeight: FontWeight.w900, fontSize: 9, letterSpacing: 2)),
        trailing: Icon(isOpen ? Icons.expand_less_rounded : Icons.expand_more_rounded, size: 16, color: ErpColors.slate400),
        onTap: () => setState(() => expanded = isOpen ? null : section.route),
      ),
      if (isOpen)
        ...section.children.map((c) => Padding(
              padding: const EdgeInsets.only(left: 8),
              child: _drawerLink(c.title, c.icon, c.route, widget.currentRoute == c.route),
            )),
    ]);
  }
}

// Rail para escritorio
class ErpRail extends StatelessWidget {
  const ErpRail({super.key, required this.currentRoute, required this.onSelect, required this.onLogout});
  final String currentRoute;
  final ValueChanged<String> onSelect;
  final VoidCallback onLogout;
  @override
  Widget build(BuildContext context) {
    // Mapeo simple para rail: solo primarios + scroll
    final rails = [
      ('/', Icons.home_rounded, 'Inicio'),
      ('/dashboard', Icons.pie_chart_rounded, 'Dashboard'),
      ('/ventas/nueva', Icons.point_of_sale_rounded, 'Ventas'),
      ('/compras', Icons.shopping_cart_rounded, 'Compras'),
      ('/maestros/articulos', Icons.inventory_2_rounded, 'Maestros'),
      ('/rrhh/empleados', Icons.people_rounded, 'RRHH'),
      ('/stock/ajuste', Icons.warehouse_rounded, 'Stock'),
      ('/bancario', Icons.account_balance_rounded, 'Legal'),
    ];
    final selected = rails.indexWhere((e) => currentRoute == e.$1);
    return Container(
      width: 72,
      color: ErpColors.slate900,
      child: Column(children: [
        const SizedBox(height: 16),
        Expanded(
          child: NavigationRail(
            backgroundColor: ErpColors.slate900,
            selectedIndex: selected == -1 ? 0 : selected,
            onDestinationSelected: (i) => onSelect(rails[i].$1),
            labelType: NavigationRailLabelType.all,
            destinations: [for (final r in rails) NavigationRailDestination(icon: Icon(r.$2), label: Text(r.$3))],
          ),
        ),
        IconButton(onPressed: onLogout, icon: const Icon(Icons.logout_rounded, color: ErpColors.rose500)),
        const SizedBox(height: 16),
      ]),
    );
  }
}

// Home — replica ERP.Web/Pages/Home.razor: KPIs + Agenda + Llamadas + Documentos
class HomePage extends StatefulWidget {
  const HomePage({super.key, required this.token});
  final String token;
  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  Map<String, dynamic>? dashboard;
  List<dynamic> tareas = [];
  List<dynamic> llamadas = [];
  List<dynamic> documentos = [];
  bool loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final api = ApiClient(token: widget.token);
    final dash = await api.getJson('api/dashboard/resumen-financiero');
    final t = await api.getList('api/tareas');
    final l = await api.getList('api/llamadas');
    final d = await api.getList('api/CicloFacturacion') ?? await api.getList('api/facturacion/listado');
    if (mounted) {
      setState(() {
        dashboard = dash;
        if (t != null) tareas = t;
        if (l != null) llamadas = l;
        if (d != null) documentos = d.take(5).toList();
        loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const ErpState.loading();
    final totalVentas = (dashboard?['totalVentas'] ?? dashboard?['TotalVentas'] ?? 0).toString();
    final totalCompras = (dashboard?['totalCompras'] ?? 0) as num? ?? 0;
    final totalNominas = (dashboard?['totalNominas'] ?? 0) as num? ?? 0;
    final beneficio = (dashboard?['beneficioNeto'] ?? dashboard?['BeneficioNeto'] ?? 0).toString();
    final factPend = dashboard?['facturasPendientesCobro'] ?? 0;
    final factVenc = dashboard?['facturasVencidas'] ?? 0;
    final stockBajo = dashboard?['articulosStockBajo'] ?? 0;

    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      // Cabecera como web: bg-white p-8 rounded-lg
      Container(
        padding: const EdgeInsets.all(20),
        decoration: ErpDecor.card(),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          const Text('ESCRITORIO PRINCIPAL', style: TextStyle(fontWeight: FontWeight.w900, fontSize: 22, letterSpacing: -1, color: ErpColors.slate900)),
          const Text('Gestión operativa y control de tareas del sistema', style: TextStyle(color: ErpColors.slate400, fontSize: 12, fontStyle: FontStyle.italic)),
          const SizedBox(height: 16),
          Wrap(spacing: 12, runSpacing: 12, children: [
            SizedBox(height: 44, child: FilledButton(onPressed: () {}, style: FilledButton.styleFrom(backgroundColor: ErpColors.slate800), child: const Text('CONFIGURACIÓN', style: TextStyle(fontSize: 10, fontWeight: FontWeight.w900)))),
            SizedBox(height: 44, child: FilledButton(onPressed: () {}, child: const Text('+ NUEVA OPERACIÓN', style: TextStyle(fontSize: 10, fontWeight: FontWeight.w900)))),
          ]),
        ]),
      ),
      const SizedBox(height: 16),
      // KPIs
      LayoutBuilder(builder: (context, c) {
        final w = c.maxWidth > 700 ? (c.maxWidth - 16) / 3 : c.maxWidth;
        return Wrap(spacing: 12, runSpacing: 12, children: [
          SizedBox(width: w, child: KpiCard(label: 'Facturación Total', value: '$totalVentas €', subtitle: '$factPend fact. pendientes', accent: ErpColors.blue600, icon: Icons.payments_rounded)),
          SizedBox(width: w, child: KpiCard(label: 'Gastos Mensuales', value: '${(totalCompras + totalNominas).toStringAsFixed(2)} €', subtitle: 'Compras ${totalCompras.toStringAsFixed(0)}€ + Nóminas ${totalNominas.toStringAsFixed(0)}€', accent: ErpColors.rose500, icon: Icons.trending_down_rounded)),
          SizedBox(width: w, child: KpiCard(label: 'Resultado Bruto', value: '$beneficio €', subtitle: 'Vencidas: $factVenc · Stock bajo: $stockBajo', accent: ErpColors.emerald500, icon: Icons.trending_up_rounded)),
        ]);
      }),
      const SizedBox(height: 16),
      // Agenda + Llamadas
      LayoutBuilder(builder: (context, c) {
        final isWide = c.maxWidth > 700;
        final cardW = isWide ? (c.maxWidth - 12) / 2 : c.maxWidth;
        return Wrap(spacing: 12, runSpacing: 12, children: [
          SizedBox(
            width: cardW,
            child: ErpCard(
              title: 'Mi Agenda para hoy',
              action: TextButton(onPressed: () {}, child: const Text('+ CITA', style: TextStyle(fontSize: 10, fontWeight: FontWeight.w900))),
              child: tareas.isEmpty
                  ? const Text('Sin citas programadas', style: TextStyle(color: ErpColors.slate400, fontSize: 12))
                  : Column(
                      children: tareas.take(4).map((t) {
                        final prio = (t['prioridad'] ?? t['Prioridad'] ?? 'MEDIA').toString();
                        final color = prio == 'ALTA' ? ErpColors.rose500 : prio == 'MEDIA' ? ErpColors.amber500 : ErpColors.slate400;
                        return Container(
                          margin: const EdgeInsets.only(bottom: 8),
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(color: ErpColors.slate50, borderRadius: BorderRadius.circular(12), border: Border.all(color: ErpColors.slate100)),
                          child: Row(children: [
                            Text((t['hora'] ?? t['Hora'] ?? '--:--').toString(), style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 11, color: ErpColors.blue600, fontFamily: 'JetBrainsMono')),
                            const SizedBox(width: 12),
                            Expanded(child: Text((t['titulo'] ?? t['Titulo'] ?? 'Tarea').toString(), style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 12))),
                            Container(padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4), decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(8)), child: Text(prio, style: TextStyle(fontSize: 8, fontWeight: FontWeight.w900, color: color))),
                          ]),
                        );
                      }).toList(),
                    ),
            ),
          ),
          SizedBox(
            width: cardW,
            child: ErpCard(
              title: 'Control de Llamadas',
              action: TextButton(onPressed: () {}, child: const Text('+ LLAMADA', style: TextStyle(fontSize: 10, fontWeight: FontWeight.w900))),
              child: llamadas.isEmpty
                  ? const Text('Sin llamadas pendientes', style: TextStyle(color: ErpColors.slate400, fontSize: 12))
                  : Column(
                      children: llamadas.take(4).map((l) {
                        return Container(
                          margin: const EdgeInsets.only(bottom: 8),
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(12), border: Border.all(color: ErpColors.slate100)),
                          child: Row(children: [
                            Expanded(
                                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                              Text((l['empresa'] ?? l['Empresa'] ?? '—').toString(), style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 12)),
                              Text('${l['motivo'] ?? l['Motivo'] ?? ''} · ${l['telefono'] ?? l['Telefono'] ?? ''}', style: const TextStyle(fontSize: 10, color: ErpColors.slate400)),
                            ])),
                            Container(padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4), decoration: BoxDecoration(color: (l['urgente'] == true) ? ErpColors.rose50 : ErpColors.slate50, borderRadius: BorderRadius.circular(20), border: Border.all(color: (l['urgente'] == true) ? ErpColors.rose500.withValues(alpha: 0.2) : ErpColors.slate200)), child: Text((l['urgente'] == true) ? 'URGENTE' : (l['tiempo'] ?? '').toString(), style: TextStyle(fontSize: 8, fontWeight: FontWeight.w900, color: (l['urgente'] == true) ? ErpColors.rose500 : ErpColors.slate400))),
                          ]),
                        );
                      }).toList(),
                    ),
            ),
          ),
        ]);
      }),
      const SizedBox(height: 16),
      // Últimos documentos (tabla -> tarjetas en móvil)
      ErpCard(
        title: 'Últimos Documentos Registrados',
        action: TextButton(onPressed: () {}, child: const Text('VER TODO', style: TextStyle(fontSize: 10, fontWeight: FontWeight.w900))),
        child: documentos.isEmpty
            ? const ErpState.empty(message: 'Sin documentos registrados')
            : Column(
                children: documentos.map((d) {
                  return DocumentoCard(
                    ref: (d['numeroDocumento'] ?? d['NumeroDocumento'] ?? '—').toString(),
                    entidad: (d['cliente']?['razonSocial'] ?? d['proveedor']?['razonSocial'] ?? '—').toString(),
                    tipo: (d['tipo'] ?? d['Tipo'] ?? '').toString(),
                    importe: '${(d['total'] ?? 0).toString()} €',
                    estado: (d['estado'] ?? d['Estado'] ?? '').toString(),
                  );
                }).toList(),
              ),
      ),
    ]);
  }
}

// Dashboard — replica Dashboard.razor: Business Insights + Stats + Riesgos
class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key, required this.token});
  final String token;
  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  Map<String, dynamic>? dto;
  bool loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final api = ApiClient(token: widget.token);
    final res = await api.getJson('api/dashboard/resumen-financiero');
    if (mounted) setState(() { dto = res; loading = false; });
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const ErpState.loading();
    if (dto == null) return const ErpState.error(message: 'No se pudo cargar Business Insights');
    final totalVentas = (dto!['totalVentas'] ?? 0).toString();
    final pendiente = (dto!['importePendienteCobro'] ?? 0).toString();
    final beneficio = (dto!['beneficioNeto'] ?? 0).toString();
    final docs = dto!['facturasPendientesCobro'] ?? 0;
    final stockBajo = dto!['articulosStockBajo'] ?? 0;
    final vencidas = dto!['facturasVencidas'] ?? 0;
    final ventasMensuales = (dto!['ventasMensuales'] as List?) ?? [];

    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
        const Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('BUSINESS INSIGHTS', style: TextStyle(fontSize: 26, fontWeight: FontWeight.w900, fontStyle: FontStyle.italic, letterSpacing: -1, color: ErpColors.slate900)),
          Text('LIVE ERP INTELLIGENCE', style: TextStyle(fontSize: 9, fontWeight: FontWeight.w800, letterSpacing: 3, color: ErpColors.slate400)),
        ])),
        FilledButton(onPressed: () {}, style: FilledButton.styleFrom(backgroundColor: Colors.white, foregroundColor: ErpColors.slate700, side: const BorderSide(color: ErpColors.slate200)), child: const Text('EXPORTAR PDF', style: TextStyle(fontSize: 10, fontWeight: FontWeight.w800))),
      ]),
      const SizedBox(height: 16),
      // Stats 4 columnas
      LayoutBuilder(builder: (context, c) {
        final isWide = c.maxWidth > 800;
        return Wrap(spacing: 12, runSpacing: 12, children: [
          SizedBox(width: isWide ? (c.maxWidth - 36) / 4 : (c.maxWidth - 12) / 2, child: KpiCard(label: 'Ventas Brutas', value: '$totalVentas €', subtitle: 'Actualizado en tiempo real', accent: ErpColors.blue600)),
          SizedBox(width: isWide ? (c.maxWidth - 36) / 4 : (c.maxWidth - 12) / 2, child: Container(padding: const EdgeInsets.all(18), decoration: BoxDecoration(color: ErpColors.slate900, borderRadius: BorderRadius.circular(20)), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [const Text('BENEFICIO NETO', style: TextStyle(color: Color(0xFFA5B4FC), fontSize: 10, fontWeight: FontWeight.w900, letterSpacing: 1.2)), Text('$beneficio €', style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.w900, fontStyle: FontStyle.italic)), const SizedBox(height: 6), Text('Rentabilidad: ${(double.tryParse(beneficio) != null && double.parse(totalVentas) != 0) ? (double.parse(beneficio) / double.parse(totalVentas) * 100).toStringAsFixed(1) : '0'}%', style: const TextStyle(color: Color(0xFF818CF8), fontSize: 9, fontWeight: FontWeight.w800))]))),
          SizedBox(width: isWide ? (c.maxWidth - 36) / 4 : (c.maxWidth - 12) / 2, child: KpiCard(label: 'Actividad Comercial', value: '$docs Docs', subtitle: 'Periodo fiscal actual', accent: ErpColors.blue600)),
          SizedBox(width: isWide ? (c.maxWidth - 36) / 4 : (c.maxWidth - 12) / 2, child: KpiCard(label: 'Pendiente de Cobro', value: '$pendiente €', subtitle: 'Requiere seguimiento', accent: ErpColors.rose500)),
        ]);
      }),
      const SizedBox(height: 16),
      LayoutBuilder(builder: (context, c) {
        final isWide = c.maxWidth > 800;
        return Wrap(spacing: 12, runSpacing: 12, children: [
          SizedBox(
            width: isWide ? (c.maxWidth * 0.66) : c.maxWidth,
            child: Container(
              padding: const EdgeInsets.all(18),
              decoration: ErpDecor.card(),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                const Text('TENDENCIA DE VENTAS', style: TextStyle(fontSize: 14, fontWeight: FontWeight.w900, fontStyle: FontStyle.italic, letterSpacing: -0.5)),
                const Text('Últimos 6 meses de facturación', style: TextStyle(fontSize: 9, fontWeight: FontWeight.w700, color: ErpColors.slate400, letterSpacing: 1)),
                const SizedBox(height: 16),
                if (ventasMensuales.isEmpty)
                  Container(height: 120, decoration: BoxDecoration(border: Border.all(color: ErpColors.slate100, style: BorderStyle.solid), borderRadius: BorderRadius.circular(12)), child: const Center(child: Text('NO HAY DATOS SUFICIENTES', style: TextStyle(color: ErpColors.slate400, fontSize: 10, fontWeight: FontWeight.w800))))
                else
                  SizedBox(
                    height: 160,
                    child: Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
                      for (final m in ventasMensuales)
                        Expanded(
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 4),
                            child: Column(children: [
                              Expanded(
                                child: Align(
                                  alignment: Alignment.bottomCenter,
                                  child: Container(
                                    height: 80 + (m['importe'] as num? ?? 0).toDouble() % 60,
                                    decoration: BoxDecoration(color: ErpColors.blue600, borderRadius: const BorderRadius.vertical(top: Radius.circular(8))),
                                  ),
                                ),
                              ),
                              const SizedBox(height: 6),
                              Text((m['mes'] ?? '').toString().substring(0, 3).toUpperCase(), style: const TextStyle(fontSize: 9, fontWeight: FontWeight.w900, color: ErpColors.slate400)),
                            ]),
                          ),
                        ),
                    ]),
                  ),
              ]),
            ),
          ),
          SizedBox(
            width: isWide ? (c.maxWidth * 0.32) : c.maxWidth,
            child: Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(color: ErpColors.rose500, borderRadius: BorderRadius.circular(24)),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                const Text('ALERTA DE RIESGO', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w900, fontStyle: FontStyle.italic, fontSize: 16, letterSpacing: -0.5)),
                const SizedBox(height: 12),
                Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(16), border: Border.all(color: Colors.white.withValues(alpha: 0.2))),
                  child: Row(children: [
                    Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [const Text('STOCK BAJO MÍNIMOS', style: TextStyle(color: Colors.white70, fontSize: 9, fontWeight: FontWeight.w900)), Text('$stockBajo', style: const TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.w900))])),
                    const Icon(Icons.chevron_right_rounded, color: Colors.white),
                  ]),
                ),
                const SizedBox(height: 10),
                Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(16), border: Border.all(color: Colors.white.withValues(alpha: 0.2))),
                  child: Row(children: [
                    Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [const Text('VENCIMIENTOS IMPAGADOS', style: TextStyle(color: Colors.white70, fontSize: 9, fontWeight: FontWeight.w900)), Text('$vencidas', style: const TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.w900))])),
                    const Icon(Icons.chevron_right_rounded, color: Colors.white),
                  ]),
                ),
              ]),
            ),
          ),
        ]);
      }),
    ]);
  }
}

// Placeholder genérico para módulos con estética idéntica a web
class ModulePlaceholder extends StatelessWidget {
  const ModulePlaceholder({super.key, required this.title, required this.subtitle, required this.icon});
  final String title;
  final String subtitle;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(title.toUpperCase(), style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w900, letterSpacing: -1, color: ErpColors.slate900)),
        Text(subtitle, style: const TextStyle(color: ErpColors.slate400, fontSize: 11)),
        const SizedBox(height: 16),
        Container(
          padding: const EdgeInsets.all(20),
          decoration: ErpDecor.card(),
          child: Column(children: [
            Icon(icon, size: 36, color: ErpColors.blue600),
            const SizedBox(height: 12),
            Text(title, style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 14, color: ErpColors.slate900)),
            const SizedBox(height: 6),
            const Text('Conexión con los endpoints de la API pendiente para este módulo.\nLa estructura visual replica fielmente la aplicación web.',
                style: TextStyle(color: ErpColors.slate400, fontSize: 11), textAlign: TextAlign.center),
            const SizedBox(height: 16),
            Wrap(spacing: 12, runSpacing: 12, children: [
              for (final k in ['Ventas del mes', 'Facturas pendientes', 'Stock bajo', 'Cobros pendientes'])
                SizedBox(
                  width: 160,
                  child: Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(color: ErpColors.slate50, borderRadius: BorderRadius.circular(16), border: Border.all(color: ErpColors.slate100)),
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Icon(icon, size: 18, color: ErpColors.blue600),
                      const SizedBox(height: 8),
                      Text(k, style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: ErpColors.slate700)),
                      const Text('—', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w900, color: ErpColors.slate900)),
                    ]),
                  ),
                ),
            ]),
          ]),
        ),
      ]);
}
