# Plan ERP Completo — Cumplimiento Legal 2026 (sin duplicar / respetando nombres existentes)

**Fecha:** 2026-08-31  
**Inventario base:** `ERP.Domain/Entities`, `ERP.Data/ApplicationDbContext.cs:21-72`, `ERP.Services`, `ERP.Api/Controllers`, `ERP.Web/Pages` (ver informe de auditoría)

## 0. Regla de no-duplicación (crítica)

Inventario muestra **95% plural** (`Empresas`, `ConfiguracionesGenerales`, `Acreedores`, `Articulos`...). Excepciones actuales:
- `Familia` → `DbSet<Familia> Familia {get;set;}` (`ApplicationDbContext.cs:26`) + `ToTable("Familia")` (`ApplicationDbContext.cs:85`) + `Familia.cs:8` — **único singular de todo el contexto**. Referencia correcta actual: `ConfiguracionGeneral.cs:8` → `ConfiguracionesGenerales` (plural DbSet, tabla plural). **No crear `Familias`**: usar `Familia` tal cual o migrar con `RenameTable("Familia","Familias")` + renombrar DbSet a `Familias` en una única migración (elegir y documentar). Recomendación: mantener `Familia` singular y documentar en README para evitar `Familias` duplicada vista en `MaestroFamilias.razor:166` y `FamiliasController.cs:8`.
- `Acreedor.cs:7` vs `Proveedor.cs:12` con `EsAcreedor` — **duplicidad lógica** (mismo concepto: proveedor servicios). No crear tercera entidad `Acreedores` distinta: mantener split actual (mercadería=`Proveedor`/`EsAcreedor=false`, servicios=`Acreedor`/`EsAcreedor=true`) y documentar.
- `GastoCaja.cs` — existe entidad pero **no tiene `DbSet<GastoCaja>` en `ApplicationDbContext.cs`**. No crear `GastosCaja` nuevo sin añadir `DbSet<GastoCaja> GastosCaja` (plural para coherencia con `CierresCaja`). Añadir `DbSet` antes de cualquier controlador nuevo.
- `DocumentoComercial.cs:7` mapeado a `DbSet<DocumentoComercial> Documentos` (`ApplicationDbContext.cs:29`) — nombre genérico `Documentos` usado en toda la app. No crear `DocumentosComerciales` paralelo. Mantener `Documentos`.

Borrar `.bak` para no confundir: `FirmaDigitalEntities.cs.bak`, `ComprasController.cs.bak`.

---

## 1. Ciclo de Facturación — Verifactu / FACe / B2B

**Legal vigente (actualizado 2026-08):**
- Verifactu: Ley 11/2021 (BOE-A-2021-11473) + RD 1007/2023 + RD 254/2025 (calendario: 01-01-2026 sociedades, 01-07-2026 resto sin SII) + especificación AEAT (registro con hash encadenado, QR, envío opcional tiempo real).
- Ley Crea y Crece B2B: Ley 18/2022 art.12 + RD 238/2026 (31-03-2026) + Orden Ministerial prevista 01-10-2026 (plazos: +12m >8M€ → 01-10-2027, +24m resto → 01-10-2028). Formatos Facturae 3.2.x / UBL EN16931 + estados 4 días.
- FACe B2G: Ley 25/2013 + Orden HAP/1074/2014 (PGE FACe).
- SII: RD 596/2016 (exime Verifactu).

**Existente en repo (reutilizar, no duplicar):**
- `DocumentoComercial.cs:7` (`TipoDocumento` enum: `Presupuesto/FacturaProforma/Pedido/Albaran/Factura/FacturaRectificativa/AjusteStock`, `EstadoDocumento: Borrador/Emitido/Pagado/Anulado`, `EnviadaCliente/FechaEnvioCliente/PresentadaHacienda/FechaPresentacionHacienda`, `EstaEmitidaFormalmente [NotMapped]`) — **nombre a mantener**.
- `RegistroVerifactu.cs` (`DocumentoId unique`, `NifEmisor(20)`, `NumeroFactura(60)`, `FechaExpedicion`, `TipoFactura(2)="F1"`, `CuotaTotal/ImporteTotal decimal(18,2)`, `FechaHoraHusoGeneracion`, `EsPrimerRegistro`, `HuellaAnterior(64)/Huella(64)`, `EstadoRemision`, `DatosRegistroJson`, `UrlQr(500)`) — **no crear `VerifactuRegistro`**.
- `VerifactuService.cs` (`GenerarRegistroAltaAsync`, `CalcularHuellaAlta` SHA256, `GenerarUrlQr` AEAT `wlpl/TIKE-CONT/ValidarQR`) — extender, no sustituir.
- `FacturacionService.cs:RegistrarFacturaVentaAsync` (tx + `StockService.ProcesarMovimientoStock` + `Vencimiento` + Verifactu) — mantener.
- `CicloFacturacionService.cs:CrearDocumento/ConvertirDocumento/IntentarEliminarAlbaran/DesvincularFacturaAsync/AnularFacturaInternaAsync/CrearRectificativaAsync/CrearAlbaranDevolucionAsync/MarcarEnviadaAsync/GenerarCorrelativo` — ya cubre regla operativa interna→emitida→rectificativa. Completar con SII check.

**Qué falta y cómo añadir sin duplicar:**
- Añadir a `RegistroVerifactu` campo `QR` ya existe (`UrlQr`). Añadir `SistemaVerifactu` boolean si hace falta distinguir VERI*FACTU vs NO-VERI*FACTU sin nueva tabla.
- Para B2B: crear `ERP.Domain/Entities/Facturacion/EstadoFacturaB2B.cs` (no tocar `RegistroVerifactu`) con enum `Aceptada/Rechazada/Pagada` + servicio `B2BRouterService` que reuse `DocumentoComercial.Lineas` y `FacturaeGenerator` (existente en `PdfService.cs` si hay). No duplicar `DocumentoComercial`.
- Para SII: añadir `bool UsaSII` a `Empresa.cs:40` (ya tiene `SerieFacturacion`, `IvaDefecto`) para eximir Verifactu cuando `SII=true`.

## 2. Módulo Control Horario

**Legal:** RD-ley 8/2019 (BOE-A-2019-3481) modifica art.34.9 ET: registro diario inicio/fin por trabajador, conservación 4 años, disposición ITSS, sanción grave 751-7500€ (art.7.5 LISOS). Guía MITES. Teletrabajo incluido. Alta dirección art.2.1.a ET excluida.

**Existente:** `ControlHorario.cs:EmpleadoId→Empleado, Entrada/Salida?, Ubicacion, TotalHoras [NotMapped]` + `Empleado.cs:ControlesHorarios, PinAcceso` + `ControlHorario.razor` + `FichajeController.cs`. Mantener nombres.

**Pendiente sin duplicar:** Añadir a `ControlHorario` campos `OrigenFichaje` (enum: Web/Movil/WhatsApp/Geofencing) y `HashCadena` (inalterabilidad) sin nueva entidad `Fichaje`. Export ITSS ya pedido: generar PDF/CSV desde mismo `ControlHorario`.

## 3. Tipos de IVA + Excepciones

**Legal:** LIVA 37/1992 art.90 (21%), art.91.Uno (10%), art.91.Dos (4%), art.20 exentas interiores (sanidad, educación, financiero...), art.21-27 exentas exportaciones/intracomunitarias, art.102-105 prorrata, art.9.1.c sectores diferenciados, art.161 recargo equivalencia, RD-ley 4/2024 (aceite oliva 4% permanente), IGIC/IPSI Canarias/Ceuta/Melilla. Info: `sede.agenciatributaria.gob.es/.../tipos-impositivos-iva.html` y `BOE-A-1992-28740`.

**Existente:** `Articulo.cs:PorcentajeIva decimal(18,2)=21`, `DocumentoLinea.PorcentajeIva decimal(5,2)`, `ConfiguracionGeneral` no cubre IVA.

**Sin duplicar:** Crear `ERP.Domain/Entities/Fiscal/ConfiguracionIVA.cs` (ya existe según inventario: `Fiscal/IVAEntities.cs`) — usar esa, no crear `IvaConfiguracion`. Contenido: `AplicaProrrataGeneral/Sector`, `RecargoEquivalencia` (5.2/1.4/0.5), `RegimenesEspeciales` (agencias viajes, bienes usados...), `UsaSII`. Verificar no exista `Iva` duplicado antes de crear `TipoIVA` enum.

## 4. Módulo Contable + Libros Oficiales

**Legal:** PGC RD 1514/2007 + PGC PYMES RD 1515/2007, Código Comercio arts.25-26 (libros obligatorios: Diario + Inventarios y Cuentas Anuales + Actas + Registro Socios), Reglamento Registro Mercantil art.329 + LGT art.29, Legalización telemática Instruction DGRN 12-02-2015 (4 meses post-cierre, vía telemática, formato XML LEGALIZACION libros).

**Existente (ya creado, no duplicar):**
- `Contabilidad/CuentaContable.cs` (Codigo 9 PK, Grupo/Nivel, `ToTable` no duplicar)
- `AsientoContable.cs` / `ApunteContable` (Serie/Numero, `Cuadra [NotMapped]`)
- `LibrosOficiales.cs` (`LibroDiario`, `LibroMayor`, `LibroInventariosCuentasAnuales`, `EjercicioContable`) — **usar estos, no crear `LibroDiarioOficial`**.
- `DTOs/Contabilidad/ContabilidadDtos.cs` ya con `CrearAsientoDto`, `BalanceComprobacionDto`.

**Pendiente:** Servicio `ContabilidadService` en `ERP.Services/Contabilidad` (si no existe) + controlador `ERP.Api/Controllers/Contabilidad/LibrosController.cs` que genere XML para Registro Mercantil (formato `leg052a`).

## 5. Compras / Almacén / Logística / Trazabilidad

**Legal:** Reglamento (CE) 178/2002 art.18 (un paso atrás/adelante), Reglamento (UE) 931/2011 (origen animal inmediato), Reglamento (CE) 852/2004 APPCC, Ley 17/2011, RD 191/2011 RGSEAA, Ley 1/2025 desperdicio alimentario.

**Existente:**
- `NuevoPedido.razor` + `RecepcionPedidos.razor` + `ComprasService.cs` + `ComprasController.cs`
- `Stock`: `ValoracionAlmacen.razor`, `KardexArticulo.razor`, `MovimientoStock.cs`, `StockService.cs` (`ProcesarMovimientoStock`, `LiberarReservaPorAnulacion`)
- `TrazabilidadEntities.cs` (`LoteTrazabilidad`, `MovimientoLote`, `AlertaTrazabilidad`, `RetiradaLote`) — **no crear `TrazabilidadLote`**.

**Sin duplicar:** Añadir a `Articulo` campo `RequiereTrazabilidadLote bool` si hace falta, y usar `LoteTrazabilidad.CodigoLote(50)` + `LoteProveedor` ya existente. No crear `Producto` paralelo a `Articulo`.

## 6. Bancario — Cobros/Pagos SEPA

**Legal:** Reglamento (UE) 260/2012, EPC Rulebooks SCT/SDD Core/B2B/SCT Inst, ISO20022 `pain.001` (CT) / `pain.008` (DD) / `camt.053/054` extractos, IBAN/BIC, Mandato SEPA RUM+CreditorID, PSD2/PSD3 (SCA, verificación titular oct-2025), direcciones estructuradas nov-2026, transferencias instantáneas <10s (Reg. 2024/886).

**Existente (ya creado, no duplicar):**
- `Bancario/CuentaBancaria.cs` (`CreditorIdentifier(35)`, `PermiteTransferenciasSEPA/AdeudosSEPA/Instant`, `MandatosAcreedor/Deudor`)
- `MandatoSEPA.cs` (`ReferenciaUnicaMandato RUM`, `CreditorIdentifier`, `DeudorNombre/IBAN/BIC + dirección estructurada`, `Esquema Core/B2B`, `TipoSecuencia RCUR/FRST`, `BeneficiarioVerificado`)
- `RemesaSEPA.cs` (`Referencia(20)`, `Tipo SCT/SDD`, `Esquema`, `XmlGenerado/HashXmlSHA256`, `EstadoRemesaSEPA`)
- Servicios `BancarioService.cs` (validación IBAN MOD97, `GenerarPain001/008`, `ConciliarAutomatico` camt.053) — usar, no crear `SepaService` duplicado.

**Pendiente:** Controlador `TesoreriaController.cs` ya existe — ampliar con `POST /api/tesoreria/remesas/{id}/enviar` en lugar de nuevo `BancarioController`.

## 7. Nóminas / Seguridad Social

**Legal:** Sistema RED, SILTRA 4.0 (01-06-2026, T-41 inactividad), FIE 5.0 (16-06-2026, nuevos segmentos nacimiento/cuidado menor), SLD, Contrat@, Delt@, MEI 0,13% y tope 4720,50€/mes 2026, BNR 06/2026.

**Existente:** `Empleado.cs` (DNI, NSS, PinAcceso, SalarioBrutoAnual, VacacionesTotales/Disfrutadas), `Nomina.cs` (Mes/Anio, SalarioBase/Complementos/Deducciones), `NominaService.cs`, `NominasController.cs`, `RRHHService.cs`.

**Sin duplicar:** Añadir a `Nomina` campos `BaseContingenciasComunes/Profesionales` si faltan, y servicio `SiltraService` que use `Empleado.EmpresaId` y `ConfiguracionIVA` ya existente.

## 8. Seguridad / Protección Datos / Firma Digital

**Legal:** RGPD (UE) 2016/679 + LOPD-GDD 3/2018 + LSSI 34/2002, eIDAS 910/2014 (Simple/Avanzada/Cualificada, TSP, sellado tiempo RFC3161 art.41-44), firma PAdES/XAdES/CAdES/JAdES (ETSI 319 122/132/142/182), certificados FNMT/Camerfirma, DPIA art.35, DPO art.37.

**Existente (ya creado, no duplicar):**
- `FirmaDigitalEntities.cs` (`CertificadoDigital` con `SubjectDN/IssuerDN/ThumbprintSHA256`, `FirmaElectronica` PAdES/XAdES/CAdES/JAdES, `SolicitudFirma`, `SelloTiempo`, `ComunicacionCertificada`)
- `FirmaDigitalService.cs` (`ImportarCertificado`, `FirmarDocumentoAsync`, `VerificarFirmaAsync`, `SellarTiempoAsync`)
- `CustomAuthenticationProvider.cs` + `AuthController.cs` (JWT) + `Permissions.cs` (`AppPermissions.All`)

**Pendiente sin duplicar:** Añadir tabla `RegistroAuditoria` genérica (quién/qué/cuándo/IP) reutilizando `SelloTiempo` para hash encadenado, no crear `Auditoria` paralela a `SelloTiempo`.

---

## Plan de implantación (respetando nombres)

**Fase 0 — Saneamiento inmediato (evitar duplicados):**
1. Borrar `.bak`.
2. Decidir convención `Familia`: mantener singular y documentar, o migración `RenameTable("Familia","Familias")` + `DbSet<Familia> Familias`.
3. Añadir `DbSet<GastoCaja> GastosCaja` si se usará (plural).
4. Verificar `ERP.Data/Migrations/ApplicationDbContextModelSnapshot.cs` no reintroduzca `Familias`.

**Fase 1 — Verifactu + B2B + SEPA (crítico 2026):**
- Completar `VerifactuService` con declaración responsable AEAT + `SistemaVerifactu` bool en `Empresa`.
- Crear `Facturacion/B2BEstadosService` sobre `DocumentoComercial` existente.
- Validar `SepaXmlGeneratorService` genera `pain.001.001.03` / `pain.008.001.02` con direcciones estructuradas (bloqueante nov-2026).

**Fase 2 — Contabilidad + IVA:**
- Implementar `ContabilidadService.GenerarLibroDiarioAsync(EjercicioId)` y `LegalizarAsync` (XML Registro Mercantil).
- `MotorIVAService.GenerarLiquidacion303Async` → casillas 303 (base 21/10/4 + recargo + prorrata).

**Fase 3 — Trazabilidad + Control Horario:**
- Añadir `LoteTrazabilidad` a recepción compras (`RecepcionPedidos.razor:Crear Lote`).
- Añadir `HashCadena` a `ControlHorario` y export ITSS.

**Fase 4 — Firma/RGPD:**
- DPIA y registro actividades tratamiento (RGPD art.30) como `ConfiguracionesGenerales` clave `RGPD_RegistroActividades`.

**Verificación:**
- `dotnet build ERP.Data` 0 errores tras añadir `DbSet`s.
- `dotnet ef migrations add` no debe detectar `CreateTable("Familias")` duplicado.
- Prueba `CrearDocumento` → `RegistroVerifactu` con QR válido + `RemesaSEPA` pain.001 válido (validador EPC).

