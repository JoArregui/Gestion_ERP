# Auditoría ERP.NET vs Flujo ERP.txt — Rama `develop`

> Fecha: 2026-10-15 · Rama base: `main@625830d` → `develop` · Solución: `ERP_Sistema.sln` (8 proyectos) · .NET 9 + Blazor WASM + WPF + MAUI + Flutter
> Origen flujo: `C:\Users\josearregui\Desktop\ERP.txt` (9 bloques). Build verificado en `develop`: `dotnet build ERP_Sistema.sln -c Release` → **0 errores, 58 warnings**.

## 1. Inventario real (verificado en disco)

- **Domain:** 35 entities (`Cliente, Proveedor, Acreedor, Articulo, Familia, DocumentoComercial/Linea, Vencimiento, CierreCaja, MovimientoStock, Empleado, ControlHorario, PoliticaControlHorario, Nomina, Tarea, Llamada` + `Bancario(4), Contabilidad(6), Fiscal(5+Tarifa), FirmaDigital(5), RGPD(1), Trazabilidad(4), Verifactu(2), FacturaElectronica`).
- **API:** 29 controllers (`Acreedores, Articulos, Auth, CicloFacturacion, CierreCaja, Clientes, Compras, Dashboard, Documentos, Empleados, Empresas, Facturacion, Familias, Fichaje, Inventario, Llamadas, Nominas, Onboarding, Proveedores, Settings, Stock, Tareas, Tesoreria, Users` + `Bancario, Contabilidad, FirmaDigital, Fiscal, Trazabilidad`).
- **Services:** 18 (`CicloFacturacion, Compras, ControlHorario, Facturacion, Facturae, Fiscal, MotorIVA, Nomina, Stock, Verifactu, Bancario, SepaXmlGenerator, Contabilidad, Trazabilidad, FirmaDigital, RRHH, PoliticaControlHorario, RegistroTratamiento, TenantDatabase, Acreedor, Pdf, Seed`).
- **Web:** 60+ `.razor` (`Ventas/*5, Compras/*2, Stock/*5, RRHH/*4, Maestros/*6, Contabilidad, Fiscal, Bancario, Trazabilidad, FirmaDigital, RGPD, Informes/InformeRentabilidad, Dashboard, Vencimientos, Verifactu, Facturae, IVA, Nominas, NuevaVenta, CierreCaja`).
- **Data:** 6 migraciones + Snapshot. Multi-tenant: `erp.db` maestro + `Gestion{Empresa}.db` por empresa (`TenantDatabaseService.cs`, claim `Tenant`).
- **Tests:** 3 (`FacturacionWorkflowTests, CierreCajaSecurityTests, TenantIsolationTests`).

## 2. Matriz de cobertura por bloque ERP.txt

| # | Bloque ERP.txt | Cobertura | Lo que YA existe (evidencia) | Gap crítico |
|---|---|---|---|---|
| 1 | Gestión Financiera y Contabilidad | **70%** | PGC `CuentaContable/Asiento/Apunte`, `LibrosDiario/Mayor/Inventarios`, `EjercicioContable`, IVA/IGIC/IPSI `MotorIVAService`, Verifactu hash+QR, Facturae 3.2 FACe/B2B, SEPA pain.001/008 + camt.053 `BancarioService.ImportarExtractoCamt053Async`, `Vencimientos`, `CierreCaja` | Sin entidad `ActivoFijo` (solo enum `AsientoTipo.Amortizacion`); sin presupuesto tesorería / cash-flow previsional; conciliación semi-manual (falta matching auto extracto↔vencimiento/asiento) |
| 2 | Ventas y CRM | **45%** | `DocumentoComercial` Presupuestos/Albaranes/Facturas/Pedidos, conversión presupuesto→pedido→albarán→factura (`CicloFacturacionService`), `Clientes`, `Tarea/Llamada/Agenda` básico, descuentos/tarifas por `DocumentoLinea`, TPV `NuevaVenta` | Sin `Lead/Oportunidad`, sin pipeline/embudo, sin historial interacciones, sin `Portal Cliente B2B` (solo SEPA B2B y Facturae B2B, no portal) |
| 3 | Compras y Proveedores | **55%** | `Proveedor/Acreedor`, `ComprasService`, `NuevoPedido/RecepcionPedidos` (actualiza stock), `Listado Compras`, `Documento EsCompra=true`, `FacturaElectronica` compra | Sin RFQ/solicitud oferta, sin comparativa precios, sin homologación (`Homologado`, ISO), sin 3-way matching formal pedido–albarán–factura |
| 4 | Almacén e Inventario | **50%** | `Articulo` Stock/Reservado/Minimo, `MovimientoStock`, Kardex, Valoración, Ajustes, Etiquetas QR, `LoteTrazabilidad/MovimientoLote` | Sin entidad `Almacen` (solo `AlmacenOrigenId/DestinoId int?` en trazabilidad), sin `Ubicacion` pasillo/estantería, sin picking/packing/reabastecimiento auto, sin `CodigoBarras/EAN, UnidadMedida, PesoNeto, LotesObligatorios` en `Articulo.cs:1-58`, sin PDA/RFID |
| 5 | Producción MRP/MES | **5%** | Solo `FechaProduccion` en lote + `TipoMovimientoLote.Produccion` (transformación lotes) | Sin `BOM`, sin `CentroTrabajo/HojaRuta`, sin `OrdenFabricacion`, sin MRP, sin imputación tiempos/consumos ni costes |
| 6 | RRHH y Talento | **65%** | `Empleado`, `ControlHorario` hash encadenado + `PoliticaControlHorario` UNIQUE por empresa, `Kiosko`, `Nomina` con desglose base/IRPF/SS, `LiquidacionSeguridadSocial` | Sin turnos, sin gestión vacaciones/ausencias, sin PRL/Formación/EPIs, sin historial documental |
| 7 | Proyectos y Servicios | **20%** | `Tarea`, `Llamada`, `Agenda`, `AsientoContable.ProyectoId` (solo FK suelta) | Sin entidad `Proyecto`, sin WBS/Gantt, sin timesheets/imputación, sin rentabilidad proyecto, sin SAT (partes, contratos mantenimiento, SLA) |
| 8 | BI / Informes / Dashboard | **40%** | `DashboardController:GetResumen/GetDetalle`, `DashboardHub` SignalR, `Dashboard/DashboardCompras/Home`, `InformeRentabilidad` | Sin generador informes custom, sin export Excel/PDF/CSV global, sin KPIs central tiempo real, sin predictiva/consolidado centros coste |
| 9 | Transversal / Técnica | **70%** | RBAC `AppPermissions` + JWT + claims `Permission`, API REST + Swagger, multi-empresa real, RGPD `RegistroTratamiento`, FirmaDigital PAdES/XAdES, Web+Desktop WPF+MAUI+Flutter | Sin `AuditLog`, sin motor `Workflow` (solo `FirmaDigitalService #region Solicitudes de Firma (Workflow)` parcial), sin multidioma, multidivisa hardcodeada `EUR`, sin conectores e-commerce/logística |

## 3. Flujo aplicado — Backlog priorizado en `develop` (orden de valor/riesgo)

> Reglas: no renombrar (`Familia` singular, `Documentos` plural), no duplicar, una migración por fase, mantener índices UNIQUE (`Documentos[EmpresaId+Numero]`, `RegistrosVerifactu[DocumentoId]`).

### Fase A — Cierre financiero (P0, 1–2 sprints)
- [ ] A1 `ActivoFijo` + `Amortizacion` (alta, depreciación lineal, baja) + asiento auto `Amortizacion`.
- [ ] A2 Conciliación auto: `BancarioService.ConciliarAsync` matching `MovimientoExtracto` ↔ `Vencimiento/OperacionRemesa/Asiento` + flag `Conciliado/Fecha/Usuario`.
- [ ] A3 Cash-flow previsional: vista `Tesoreria/Prevision` desde `Vencimientos` (cobros/pagos) + `RemesasSEPA`.

### Fase B — CRM mínimo viable (P0)
- [ ] B1 Entidades `Lead` → `Oportunidad` (`Estado, Probabilidad, Importe, Origen, ComercialId`) + conversión `Oportunidad→Presupuesto`.
- [ ] B2 Pipeline Kanban en `Ventas/Pipeline.razor` + historial interacciones (reusar `Llamada/Tarea` con `LeadId/OportunidadId`).
- [ ] B3 Portal B2B lectura: endpoints `PortalController` (mis presupuestos/facturas/pedidos por `ClienteId` del JWT) — sin app nueva, solo rol `PortalCliente`.

### Fase C — Compras completa (P1)
- [ ] C1 `SolicitudOferta (RFQ)` + `LineaRFQ` + `OfertaProveedor` + comparativa.
- [ ] C2 `Proveedor.Homologado, CertificadoCalidad, Evaluacion` + bloqueo si no homologado.
- [ ] C3 3-way matching: `RecepcionPedidos` exige `Pedido→Albaran→Factura` con tolerancias antes de `EstaEmitidaFormalmente`.

### Fase D — WMS mínimo (P1)
- [ ] D1 Entidades `Almacen` + `Ubicacion (Pasillo/Estanteria/Nivel)` + `StockUbicacion`; extender `Articulo` con `CodigoBarras, UnidadMedida, PesoNeto, LotesObligatorios`.
- [ ] D2 Picking/packing básico + reabastecimiento auto por `StockMinimo`.
- [ ] D3 PDA: página MAUI `StockPage` con scan código barras (reusa `CodigoBarras`).

### Fase E — Producción ligera (P2, solo si hay planta)
- [ ] E1 `BOM (ArticuloPadre→Componente, Cantidad)` + `CentroTrabajo` + `HojaRuta (Operacion, Tiempo)`.
- [ ] E2 `OrdenFabricacion` (consumo real + imputación horas) + coste directo/indirecto.

### Fase F — Proyectos/SAT (P2)
- [ ] F1 Entidad `Proyecto (ClienteId, Presupuesto, CostePrevisto, Estado)` + `ImputacionHoras (EmpleadoId, ProyectoId, Horas, Fecha)` + rentabilidad `Previsto vs Real`.
- [ ] F2 SAT: `Ticket (Proyecto/Cliente, SLA, Estado)` + `ContratoMantenimiento`; Gantt simple con `FechaInicio/Fin/Dependencia` en `Tarea`.

### Fase G — Transversal (continuo)
- [ ] G1 `AuditLog (Usuario, Entidad, Id, Antes/Despues JSON, Fecha, IP)` vía interceptor EF.
- [ ] G2 Motor `Workflow` genérico (aprobación presupuestos/pedidos > umbral + alertas email).
- [ ] G3 Informes custom + export Excel/PDF/CSV + KPIs central + multidioma (resx es/en) + divisa por `Empresa.Moneda`.

## 4. Decisión requerida

Responde con qué fase arranco en `develop` (recomendado **A1+A2+B1** en una sola migración `AddERPFlujoFaseA_B1`):
1. `A1+A2` financiero, 2. `B1+B2` CRM, 3. `D1` almacén, 4. Todo el backlog tal cual.

## 5. Cómo verificar

```powershell
git checkout develop; git pull
dotnet build ERP_Sistema.sln -c Release   # 0 errores esperado
dotnet test ERP.Services.Tests -c Release
```
