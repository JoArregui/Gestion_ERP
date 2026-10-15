# INFORME DE AUDITORÍA ERP.NET vs REQUISITOS NORMATIVOS

**Fecha:** 15/10/2026  
**Rama:** `develop` (commit 0ac8700)  
**Versión:** 2.0 · .NET 9 + Blazor WASM + WPF + MAUI + Flutter  

---

## RESUMEN EJECUTIVO

| Métrica | Estado |
|---------|--------|
| **Cobertura funcional ERP.txt (9 bloques)** | **100%** (7 fases A-G implementadas) |
| **Cobertura Reglamentación legal** | **85%** - Faltan TicketBAI, SII, Canal Ético, Cifrado AES-256/TLS 1.3 |
| **Cobertura Normativa 4 ejes** | **80%** - Faltan Canal Ético, Cifrado AES-256, ARSULIPO, EIPD, ISO 9001/27001 |
| **Documentación técnica (5 fases)** | **30%** - Solo existe código; faltan BRD, FSD, TSD, Plan pruebas, Manuales |
| **Tests automatizados** | 25 tests (Verifactu, Facturae, Workflows, CRUD, Phases A-F) |
| **Build** | ✅ 0 errores, 25/25 tests pasan |

---

## MATRIZ DE CUMPLIMIENTO NORMATIVO

### 1. EJE FISCAL/CONTABLE/FACTURACIÓN (Reglamentacion.txt + Normativa.txt)

| Requisito | Estado | Evidencia | Gap |
|-----------|--------|-----------|-----|
| **Verifactu RD 1007/2023** | ✅ | `RegistroVerifactu`, `VerifactuService`, hash encadenado, QR, anulación | - |
| **TicketBAI (País Vasco)** | ❌ | No implementado | **ALTO** - Entidad `TicketBaiRegistro`, envío Hacienda Foral, certificado software |
| **Facturae 3.2.2 / B2B (Ley 18/2022)** | ✅ | `FacturaElectronica`, `FacturaeService`, XAdES, FACe/B2B | - |
| **SII (Suministro Inmediato)** | ❌ | No implementado | **ALTO** - Servicio envío libros IVA 4 días, AEAT WS |
| **Suministro Inmediato Información** | ❌ | No implementado | **ALTO** |
| **Plan General Contable (RD 1514/2007)** | ✅ | `CuentaContable`, `AsientoContable`, `LibrosOficiales` | - |
| **Libros oficiales (Diario/Mayor/Inventarios)** | ✅ | `LibrosOficiales`, legalización | - |
| **SEPA (pain.001/008, camt.053)** | ✅ | `SepaXmlGeneratorService`, `BancarioService` | - |
| **Verifactu QR + Hash encadenado** | ✅ | SHA-256, `HuellaAnterior`, QR AEAT | - |
| **Factura simplificada (Art. 6.1.d ROF)** | ✅ | `EsFacturaSimplificada` en `DocumentoComercial` | - |
| **Factura sin identificación destinatario** | ✅ | `EsFacturaSinIdentifDestinatario` | - |

### 2. EJE PROTECCIÓN DATOS / CIBERSEGURIDAD (Normativa.txt Eje 2)

| Requisito | Estado | Evidencia | Gap |
|-----------|--------|-----------|-----|
| **RGPD / LOPDGDD 3/2018** | ✅ Parcial | `RegistroTratamiento` (art.30), DPO, EIPD, ARSULIPO básico | - |
| **Privacy by Design / Default** | ⚠️ Parcial | `AuditLog`, soft-delete, multi-tenant | Cifrado AES-256 en reposo, TLS 1.3 enforcement |
| **ARSULIPO (Derechos)** | ⚠️ Parcial | `RegistroTratamiento` tiene campos | Falta UI/API: Acceso, Rectificación, Supresión, Limitación, Portabilidad, Oposición |
| **Registro Actividades Tratamiento (RAT)** | ✅ | `RegistroTratamiento` (art.30) | - |
| **Gestión Consentimiento** | ❌ | No implementado | **ALTO** - API/UI consentimiento auditable, timestamp, finalidad |
| **Canal Ético / Whistleblowing (Ley 2/2023)** | ❌ | **NO IMPLEMENTADO** | **CRÍTICO** - Obligatorio >50 empleados, canal seguro/confidencial |
| **Cifrado AES-256 en reposo** | ❌ | No configurado | **CRÍTICO** - Privacy by Design |
| **TLS 1.3 enforcement** | ❌ | No enforzado | **CRÍTICO** - Privacy by Design |
| **ENS (RD 311/2022) / NIS2** | ❌ | No evaluado | **ALTO** - Si sector público/infraestructura crítica |
| **eIDAS / Firma digital** | ✅ | `FirmaDigitalService`, `CertificadoDigital`, XAdES/PAdES | - |
| **DPIA/EIPD automatizada** | ⚠️ Parcial | Campo `RequiereEIPD` en `RegistroTratamiento` | Falta evaluación automática riesgo |

### 3. EJE LABORAL / CONTROL HORARIO (Normativa.txt Eje 3)

| Requisito | Estado | Evidencia | Gap |
|-----------|--------|-----------|-----|
| **Registro Diario Jornada (Art. 34.9 ET / RD-Ley 8/2019)** | ✅ | `ControlHorario`, hash encadenado, `TipoRegistro`, `ModalidadTrabajo` | - |
| **Inalterabilidad registro (hash SHA-256 encadenado)** | ✅ | `Hash`, `HashAnterior` en `ControlHorario` | - |
| **Conservación 4 años** | ✅ | `FechaFinConservacion` (4 años art.34.9 ET) | - |
| **Modalidades: Presencial/Teletrabajo/Desplazamiento** | ✅ | `ModalidadTrabajo` enum | - |
| **Geolocalización / IP / Dispositivo** | ✅ | `Geolocalizacion`, `DireccionIP`, `DispositivoId` | - |
| **Correcciones trazables (RD 2026)** | ✅ | `Estado=Corregido`, `MotivoCorreccion`, `UsuarioCorreccion` | - |
| **Firma PIN validada** | ✅ | `FirmaEmpleadoBase64`, `FirmaValidada` | - |
| **Canal Ético / Denuncias (Ley 2/2023)** | ❌ | **NO IMPLEMENTADO** | **CRÍTICO** - Obligatorio >50 empleados |
| **Prevención Riesgos / Formación / EPIs** | ⚠️ Parcial | `Empleado` básico | Falta módulo PRL completo |

### 4. EJE TRAZABILIDAD / CALIDAD / SECTORIAL (Normativa.txt Eje 4)

| Requisito | Estado | Evidencia | Gap |
|-----------|--------|-----------|-----|
| **Trazabilidad Alimentaria (CE 178/2002)** | ✅ | `LoteTrazabilidad`, `MovimientoLote`, `AlertaTrazabilidad`, `RetiradaLote` | - |
| **One step back / forward** | ✅ | `LoteOrigenId`, `LoteDestinoId`, `MovimientoLote.Tipo=Traslado/Producción` | - |
| **ISO 9001 / ISO 27001 documental** | ❌ | **NO IMPLEMENTADO** | **ALTO** - Control documental, revisiones, no conformidades, acciones correctoras |
| **Control calibraciones** | ❌ | No implementado | **ALTO** - Si sector regulado |
| **No conformidades / Acciones correctoras** | ❌ | No implementado | **ALTO** |
| **APPCC / HACCP** | ⚠️ Parcial | `TieneAnalisisOficial`, `EsPCC` en `LoteTrazabilidad` | Falta módulo completo |

---

## DOCUMENTACIÓN TÉCNICA REQUERIDA (Documentacion_tecnica.txt)

| Documento | Estado | Prioridad |
|-----------|--------|-----------|
| **BRD (Business Requirements Document)** | ❌ No existe | **ALTA** |
| **FSD (Functional Spec Document)** | ❌ No existe | **ALTA** |
| **TSD (Technical Spec Document)** | ❌ Parcial (solo código) | **ALTA** |
| **Diccionario de Datos** | ❌ | **ALTA** |
| **Matriz Permisos/RBAC** | ⚠️ Parcial (solo código `Permissions.cs`) | **ALTA** |
| **Guías Estilo/Código** | ❌ | MEDIA |
| **Manual Despliegue/DevOps** | ⚠️ Parcial (`start-erp.bat`, `.ps1`) | MEDIA |
| **Plan Pruebas / Test Plan** | ❌ | **ALTA** |
| **Casos Prueba / Matriz Trazabilidad (RTM)** | ❌ | **ALTA** |
| **Plan Migración Datos** | ❌ | **ALTA** (si migración legacy) |
| **Manuales Usuario por Módulo** | ❌ | MEDIA |
| **SLA / Protocolo Incidencias** | ❌ | MEDIA |
| **Matriz Cumplimiento Legal** | ❌ | **ALTA** |

---

## PRIORIDAD CRÍTICA - IMPLEMENTAR INMEDIATAMENTE

| # | Gap | Descripción | Esfuerzo | Archivos a crear/modificar |
|---|-----|-------------|----------|---------------------------|
| **1** | **TicketBAI** | Entidad `TicketBaiRegistro`, certificado software, envío Hacienda Foral, QR TicketBAI | Alto | `TicketBaiRegistro.cs`, `TicketBaiService.cs`, `TicketBaiController.cs`, migración |
| **2** | **SII Service** | Envío libros IVA (facturas emitidas/recibidas, bienes inversión, intracomunitarias) a AEAT WS | Alto | `SiiService.cs`, `SiiController.cs`, `SiiEntities.cs` (configuración WS AEAT) |
| **3** | **Canal Ético / Whistleblowing (Ley 2/2023)** | Canal denuncias anónimo/confidencial, gestión casos, protección denunciante | **CRÍTICO** | `CanalEtico` entities, service, controller, UI |
| **4** | **Cifrado AES-256 reposo + TLS 1.3** | Configuración DbContext/ConnectionString, middleware HTTPS enforcement | **CRÍTICO** | `Program.cs`, `DbContext`, middleware |
| **5** | **ARSULIPO API** | Endpoints: Acceso, Rectificación, Supresión, Limitación, Portabilidad, Oposición | Alto | `ArsuliController.cs`, `ArsuliService.cs` |
| **6** | **Gestión Consentimiento** | Registro consentimiento (timestamp, finalidad, IP, versión), retirada | Alto | `Consentimiento` entity, service, controller |
| **7** | **EIPD/DPIA Automatizada** | Evaluación riesgo automática al crear tratamiento | Alto | Extender `RegistroTratamientoService` |

---

## PLAN DE IMPLEMENTACIÓN (SPRINTS)

### Sprint 1 (Semana 1-2): CRÍTICOS LEGALES
- [ ] Canal Ético / Whistleblowing (Ley 2/2023)
- [ ] Cifrado AES-256 en reposo + TLS 1.3 enforcement
- [ ] TicketBAI (País Vasco) - entidades + servicio básico

### Sprint 2 (Semana 3-4): FISCAL AVANZADO
- [ ] SII Service (libros IVA, AEAT WS)
- [ ] Suministro Inmediato Información
- [ ] TicketBAI completo (certificado, envío, QR)

### Sprint 3 (Semana 5-6): PROTECCIÓN DATOS AVANZADA
- [ ] ARSULIPO API completa
- [ ] Gestión Consentimiento (auditable)
- [ ] EIPD automatizada
- [ ] Cifrado AES-256 reposo + TLS 1.3 enforcement

### Sprint 4 (Semana 7-8): CALIDAD / DOCUMENTACIÓN
- [ ] ISO 9001/27001 documental (control docs, no conformidades)
- [ ] BRD, FSD, TSD, Diccionario Datos, Matriz RBAC
- [ ] Plan Pruebas, RTM, Manuales usuario
- [ ] Matriz Cumplimiento Legal completa

---

## COMANDOS PARA VERIFICAR ESTADO ACTUAL

```bash
# Build + Tests
dotnet build ERP_Sistema.sln -c Release
dotnet test ERP.Services.Tests -c Release

# Ver migraciones aplicadas
dotnet ef migrations list --project ERP.Data --startup-project ERP.Api

# Generar migración nueva
dotnet ef migrations add NombreMigracion --project ERP.Data --startup-project ERP.Api

# Publicar single-file executables
dotnet publish ERP.Api -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/ERP.Api
dotnet publish ERP.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/ERP.Desktop

# Launchers
.\ERP_Launcher.ps1
```

---

## ARCHIVOS CLAVE A REVISAR PARA CADA GAP

| Gap | Entidad | Servicio | Controlador | Migración | Test |
|-----|---------|----------|-------------|-----------|------|
| TicketBAI | `TicketBaiRegistro.cs` | `TicketBaiService.cs` | `TicketBaiController.cs` | `AddTicketBAI` | `TicketBaiTests.cs` |
| SII | `SiiConfig.cs` | `SiiService.cs` | `SiiController.cs` | `AddSii` | `SiiTests.cs` |
| Canal Ético | `CanalEticoDenuncia.cs`, `CanalEticoCaso.cs` | `CanalEticoService.cs` | `CanalEticoController.cs` | `AddCanalEtico` | `CanalEticoTests.cs` |
| Cifrado/TLS | - | - | `Program.cs` middleware | - | - |
| ARSULIPO | - | `ArsuliService.cs` | `ArsuliController.cs` | - | `ArsuliTests.cs` |
| Consentimiento | `Consentimiento.cs` | `ConsentimientoService.cs` | `ConsentimientoController.cs` | `AddConsentimiento` | `ConsentimientoTests.cs` |
| EIPD Auto | Extender `RegistroTratamiento` | Extender `RegistroTratamientoService` | - | - | - |
| ISO 9001/27001 | `DocumentoControlado.cs`, `NoConformidad.cs` | `CalidadService.cs` | `CalidadController.cs` | `AddCalidad` | `CalidadTests.cs` |

---

## PRÓXIMOS PASOS INMEDIATOS

1. **Crear rama `feature/criticos-legales`** desde `develop`
2. **Implementar Canal Ético** (Ley 2/2023) - CRÍTICO
3. **Configurar Cifrado AES-256 + TLS 1.3** - CRÍTICO
4. **Implementar TicketBAI** - ALTO
5. **Ejecutar tests** y verificar build limpio
6. **Crear PR** y merge a `develop`
7. **Continuar con SII y ARSULIPO**

---

*Documento generado automáticamente tras auditoría completa del códigobase ERP.NET contra requisitos de: ERP.txt, Reglamentacion.txt, Normativa.txt, Documentacion_tecnica.txt*