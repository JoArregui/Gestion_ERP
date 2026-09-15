# DOSSIER LEGAL ERP 2026 — Ciclo Completo España
> Recopilación normativa vigente a 31-08-2026 para ERP multi-tenant (EmpresaId). Verifica cada BOE antes de aplicar en producción.

## 0. Resumen mapeo existente (para NO duplicar nombres)

| Módulo | Entidades ya existentes (`ERP.Domain/Entities*` y `ERP.Services*`) | Estado |
|---|---|---|
| **Ciclo facturación** | `DocumentoComercial` (TipoDocumento, EsCompra, NumeroDocumento, BaseImponible, TotalIva, EstadoDocumento, IsContabilizado...), `DocumentoLinea` (PorcentajeIva, Cantidad, PrecioUnitario), `Vencimiento`, `CierreCaja`, `FacturacionService`, `CicloFacturacionService`, `ComprasService` | Base sólida. Faltan campos legal FACe/FaceB2B y SII |
| **Verifactu** | `RegistroVerifactu` (NifEmisor, NumeroFactura, HuellaAnterior/Huella SHA256, UrlQr, EstadoRemision...), `VerifactuService.CalcularHuellaAlta()`, `GenerarUrlQr()` | Implementado mínimo viable. Ver §1 gaps |
| **IVA** | `Fiscal/IVAEntities.cs`: `ConfiguracionIVA` (prorrata general/especial Art102-105, sectores Art9.1.c, recargo equiv. Art161, IVA caja Art163bis), `LiquidacionIVA` (303), `DetalleLiquidacionIVA`, `SectorDiferenciadoIVA`; `MotorIVAService` | Muy completo LIVA. Falta IGIC/IPSI 347/349/390 |
| **Contable** | `Contabilidad/AsientoContable` + `ApunteContable`, `CuentaContable` (PGC 9 grupos), `LibrosOficiales.cs` (LibroDiario, LibroMayor, LibroInventariosCuentasAnuales, EjercicioContable), HashArchivo, Legalización | Completo PGC. Ver §4 gaps |
| **Trazabilidad/Stock** | `Articulo` (Stock, StockReservado, PorcentajeIva), `MovimientoStock`, `Trazabilidad/LoteTrazabilidad`, `MovimientoLote`, `AlertaTrazabilidad`, `RetiradaLote`, `StockService` | Cubre Reg. CE 178/2002 |
| **Bancario SEPA** | `Bancario/CuentaBancaria`, `MandatoSEPA`, `RemesaSEPA`, `OperacionRemesaSEPA`, `ExtractoBancario`, `BancarioService`, `SepaXmlGeneratorService` (pain.001.001.03/pain.008.001.02 ISO20022) | Completo, direcciones estructuradas ISO2022 ya modeladas |
| **RRHH/Fichaje** | `Empleado` (DNI, NSS, IBAN, PinAcceso), `ControlHorario` (Entrada/Salida/Ubicacion), `Nomina`, `RRHHService`, `NominaService` | Básico. Ver §6 gaps control horario digital |
| **Firma/RGPD** | `FirmaDigital/FirmaDigitalEntities.cs` (CertificadoDigital, FirmaElectronica PAdES/XAdES/CAdES/JAdES, SelloTiempo RFC3161, ComunicacionCertificada), `FirmaDigitalService` | Avanzado eIDAS. Falta RGPD registro tratamiento |
| **Compras/Logística** | `Proveedor`, `Acreedor`, `Familia` | Base |

> **Regla de oro aplicada en este dossier:** Mantener nombres existentes (`ERP.Data/ApplicationDbContext.cs:21-72` ya registra todos los DbSet) y **extender**, no renombrar.

---

## 1. VERI*FACTU — Reglamento Antifraude (inmune a duplicados)

### Base legal
- **Art. 29.2.j LGT** (Ley 58/2003) redactado por **Ley 11/2021 antifraude** → obligación SIF con integridad, conservación, accesibilidad, legibilidad, trazabilidad, inalterabilidad.
- **RD 1007/2023 de 5-dic** (RRSIF) — publica requisitos SIF y estandarización registros (BOE 06-12-2023). `sede.agenciatributaria.gob.es` y `BOE-A-2023-24840`.
- **Orden HAC/1177/2024 de 17-oct** — especificaciones técnicas, funcionales y de contenido (hash SHA256 encadenado, firma electrónica, QR, declaración responsable). Publicado 28-10-2024.
- **RD 254/2025 de 1-abr** (BOE-A-2025-6600) → aplaza plazos a 1-ene-2026 (IS) y 1-jul-2026 (resto); obliga a fabricantes a tener SIF adaptados 9 meses tras Orden (→ jul-2025).
- **RD-ley 15/2025 de 2-dic** (BOE 03-12-2025, DF1ª modifica DF4ª RD1007/2023) → **plazos vigentes a 31-08-2026:**  
  - Sociedades IS: **1-ene-2027**  
  - Resto (autónomos IRPF, entidades atribución rentas, no residentes EP): **1-jul-2027**  
  - Sanción **art. 201 bis LGT** entra en vigor justo ese día: usuario 50.000€/ejercicio (hasta 150.000€ si doble contabilidad), fabricante 150.000€/ejercicio·tipo software. Antes no es sancionable emitir con SIF no conforme (periodo pruebas).

### Modalidades (art. 7, 8.2, 14.2, 15, 16 RRSIF)
1. **VERI*FACTU** (emisión verificable): remisión automática e inmediata de cada registro de facturación a sede AEAT. Incluye QR + leyenda “VERI*FACTU”.
2. **NO VERI*FACTU** (conservación en SIF emisor): guarda registros con huella + firma, sin remisión inmediata, pero debe remitir a requerimiento AEAT (art. 15).
3. **Aplicativo AEAT** (art. 7.b): formulario web gratuito VERI*FACTU para pequeños sin SIF.

### Registros estandarizados (± homólogos a `RegistroVerifactu` actual)
- **Alta** (`ERP.Domain/Entities/RegistroVerifactu.cs:7`): NIF emisor, serie-número, fecha expedición (dd-MM-yyyy), tipo factura (F1 ordinaria, F2 simplificada, R1-R5 rectificativa, etc. art. 10), cuota/total, huella anterior, fecha-hora-huso generación (ISO8601 con offset), huella SHA256 = hash(IDEmisor&NumSerie&Fecha&Tipo&Cuota&Importe&HuellaAnterior&FechaHuso), firma electrónica opcional según modalidad, ID sistema.
- **Anulación** (no modelado aún): mismo encadenamiento, indica factura anulada.
- **Eventos**: el SIF debe registrar eventos (inicio/fin, incidencias, exportación) con inalterabilidad.

### Requisitos SIF (art. 8-12)
- Inalterabilidad + conservación durante prescripción (4 años general tributaria, 6 años mercantil).
- Trazabilidad: huella encadenada (`HuellaAnterior` en `RegistroVerifactu:44`), declaración responsable del productor (no homologación AEAT).
- QR obligatorio en toda factura (completa o simplificada) desde entrada en vigor obligatoria, con URL `https://www2.agenciatributaria.gob.es/wlpl/TIKE-CONT/ValidarQR?nif=...&numserie=...&fecha=...&importe=...` — ya implementado en `VerifactuService.GenerarUrlQr():85`.
- Fecha expedición = fecha generación registro (± reintento por incidencia técnica).
- Opción VERI*FACTU tácita al iniciar remisión sistemática; vincula mínimo hasta fin año natural (art. 16.5). Puede cambiar de NO VERI*FACTU→VERI*FACTU en cualquier momento, no a la inversa hasta 31-dic.
- TicketBAI foral queda excluido (territorios forales).

### Gaps en proyecto vs legal
- [ ] Añadir entidad `RegistroVerifactuAnulacion` + `TipoOperacionRegistroVerifactu` (Alta/Anulación/Evento).
- [ ] Campos: `Incidencia` (S/N), `RechazoPrevio` (S/N + motivo), `RefExterna`, `NombreRazonEmisor`, `IdSistemaInformatico` (nombre+versión+ID fabricante), `NumeroInstalacion`, `TipoHuella` (01 SHA256).
- [ ] `VerifactuService` → soportar anulación, reenvío con backoff, validación rechazo AEAT, generación XML TicketBAI-like para forales.
- [ ] `Empresa` → añadir `NIF` validado (no solo CIF), `CertificadoVerifactuId` FK, `ModalidadVerifactu` (Verifactu/NoVerifactu), `FechaAltaVerifactu`.
- [ ] `DocumentoComercial` → ya tiene `Observaciones`, añadir `IncidenciaVerifactu`, `EsFacturaSimplificada`, `EsFacturaSinIdentifDestinatario` (art. 6.1.d ROF).

---

## 2. FACTURACIÓN ELECTRÓNICA

### 2.1 FACe — Factura a AA.PP. (B2G)
- **Ley 25/2013 de impulso factura electrónica + RD 1619/2012 ROF** + Orden HAP/1074/2014.
- Obligatoria desde 15-ene-2015 para proveedores de AA.PP. >5.000€ (algunas AA.PP. exigen desde 0€).
- Formato **Facturae 3.2 / 3.2.2** XML firmado XAdES-Enveloped con certificado cualificado, remitido a **FACe (punto general entrada)** → FACE.gob.es y sus variantes autonómicas FACeB2B, eFACT Catalunya.
- Workflow estados: Registrada → En trámite → Aceptada/Rechazada/Pagada (art. 9 Ley 25/2013). Plazo pago 30 días (Ley 3/2004 morosidad). DIR3 (Oficina contable, Órgano gestor, Unidad tramitadora) obligatorio.
- Requiere sellado cualificado y custodia.

### 2.2 B2B — Ley Crea y Crece 18/2022 (art. 12) + Reglamento desarrollo (proyecto RD 2024, pendiente BOE definitivo a 31-08-2026)
- **Ámbito:** todas las operaciones B2B entre empresarios/profesionales residentes (trasposición Directiva 2014/55 + EN16931).
- **Calendario oficial anunciado** (pendiente confirmación BOE):
  - Grandes empresas (>8M€ facturación): 1 año tras publicación Reglamento.
  - Resto (<8M€): 2 años tras publicación (estimación 2027/2028). El Gobierno vinculó a disponibilidad Verifactu público.
- **Formato:** interoperabilidad obligatoria **Facturae 3.2.x** o **EN16931 UBL/CEFACT** (PEPPOL BIS Billing 3.0). Comunicación vía **plataformas privadas certificadas** + **solución pública de la AEAT** (hub).
- **Contenido mínimo ROF art. 6:** NIF emisor/receptor, serie-número, fecha expedición/operación, base/IVA, tipo impositivo, cuota, contraprestación, etc. + estado pago (aceptada/rechazada) y plazo pago a informar a observatorio morosidad.
- **Compatibilidad Verifactu:** un mismo XML puede servir para Verifactu (registro) y para B2B (factura). El RRSIF indica que el SIF usará modelo de datos único. Ya existe en `DocumentoComercial.NumeroDocumento:20` y `Fecha:28`.

### Gaps vs proyecto
- [ ] Nueva entidad `FacturaElectronica` (Id, DocumentoId FK único, Formato (Facturae/UBL), Version, XmlBase64, Hash, FirmaXAdES, EstadoFACe/EstadoB2B, CodigoDIR3 OC/OG/UT, PuntoEntrada (FACe/Privado/AEAT), FechaRegistro, AcuseRecibo).
- [ ] `Cliente`/`Proveedor` → DIR3 separados, `NIF_UE` (VIES), `EsAdministracionPublica` bool.
- [ ] Servicio `FacturaeService` (generar 3.2.2, firmar XAdES con `CertificadoDigital`, validar XSD, enviar FACe SOAP, polling estados).

---

## 3. FISCALIDAD — IVA / IRPF / IGIC / EXCEPCIONES

### 3.1 IVA — Ley 37/1992 (texto consolidado 28-02-2026 BOE-A-1992-28740) + RD 1624/1992 RIVA
- **Tipos estructurales (§ IVA 2026 AEAT):**
  - General 21% (art. 90) — default `Articulo.PorcentajeIva:29 =21`.
  - Reducido 10% (art. 91.Uno) — hostelería, transporte viajeros, vivienda rehabilitación, etc.
  - Superreducido 4% (art. 91.Dos) — pan, leche, queso, huevos, fruta/verdura sin transformar, libros físicos, medicamentos humanos, VPO 1ª transmisión.
  - Temporal 0% alimentos básicos (RD-ley 20/2022 prorrogado sucesivo). En 2026: aceite oliva pasó a 4% permanente; pasta/aceites semillas salieron 0% fin24. Consultar tipo vigente por fecha en `MotorIVAService`.
  - Exento art. 20 (sanidad, educación, seguros, financiero, alquiler vivienda) y no sujeto art. 7 (export, intracom con NIF-IVA válido).
  - **SII:** grandes empresas/RedeMe/Grupos IVA → suministro 4 días hábiles (no aplica a pymes estándar; no modelado — opcional).
- **Recargo equivalencia** (art. 148-163, 161): comerciantes minoristas PF / CB sin transformación al consumidor final, no sociedad mercantil → obligado. Tipos 2026 AEAT:
  - 5,2% sobre base al 21%, 1,4% sobre base al 10%, 0,5% sobre base al 4%, 1,75% tabaco (LABORES). `IVAEntities.ConfiguracionIVA.RecargoGeneral:53=5.2` ya correcto. Proveedor repercute IVA+recargo por separado; minorista no presenta 303 ni deduce.
- **Prorrata** (§ ya bien modelado): general art.102-103 (base deducible/base total*100, redondeo techo `Math.Ceiling` en `MotorIVAService:52`), especial art.104-105 (>20% perjuicio obliga).
- **Sectores diferenciados** art.9.1.c: diferencia >50pp deducción entre actividades → liquidación separada (ya `SectorDiferenciadoIVA`).
- **Regímenes especiales** (arts. 134-163 LIVA): ya flags en `ConfiguracionIVA:62-66` (agencias viajes, bienes usados, arte, oro inversión, OSS/IOSS). Necesita ampliar mini-ventanilla OSS para servicios electrónicos.
- **IVA caja** art.163 bis (CAJA): facturación <2M€, criterio cobro/pago, liquidación al cobro (flag `AplicaIVACaja:69` ya existe).
- **Inversión sujeto pasivo** art.84.Uno.2º (ya `InversionSujetoPasivoHabitual:76`).
- **Modelos:** 303 trimestral (20-abr, 20-jul, 20-oct, 30-ene), 390 resumen anual, 349 intracom (VIES), 347 (declaración anual operaciones >3.005,06€, derogado/reemplazado por SII+303 info pero aún exigible histórico), 111/115 IRPF (ver §3.2). `LiquidacionIVA` ya genera 303; falta 390/349.

### 3.2 IRPF / Retenciones — Ley 35/2006 + RD 439/2007
- Factura con retención (profesionales 15% general, 7% primer año, 19% capital, 1% módulos): `DocumentoLinea` necesita `PorcentajeRetencionIRPF` + `BaseRetencion`.
- `Cliente`/`Proveedor` → `TipoIRPF` y `EpigrafeIAE`.
- Libros IRPF estimación directa: Libro ingresos/gastos/bienes inversión/provisiones (no contable).

### 3.3 IGIC / IPSI — Ley 20/1991 (Canarias) + Ley 8/1991 (Ceuta-Melilla)
- Canarias fuera territorio IVA UE → **IGIC** gestionado ATC, tipos 2026 (guiafiscal.es + ATC): 0% (básicos), 3% (transporte/hostelería), 5% (vivienda/refrescos), 7% general (mayoría bienes/servicios), 9,5% incrementado (perfumería/joyería<400), 13,5%/15% especial (lujo/joyas>400/embarcaciones), 20% tabaco rubio, 1% petróleo nuevo 2026. Modelo 420 trimestral (no 303), 400 franquicia <30k€ (no repercute).
- Ceuta/Melilla **IPSI** 0,5-10% según ordenanza (ayuntamiento).
- Operaciones Península↔Canarias = exportación/importación (DUA). `Empresa`/`Articulo` necesita `TerritorioFiscal` (IVA/IGIC/IPSI) y tipo aplicable. `DocumentoComercial` no puede mezclar IGIC+IVA.

### 3.4 Canónicas España extra
- **Suministro Inmediato Información (SII):** RD 703/2017 — grandes >6M€, REDEME, grupos → 4 días. Si no aplica, no se implementa ahora (flag `Empresa.EsSII`).
- **Intracomunitaria:** NIF-IVA VIES + modelo 349 (art. 79-80 RIVA).

### Gaps fiscalidad
- [ ] Ampliar `Articulo.PorcentajeIva` a enum `TipoIVA:346` (General/Reducido/Superreducido/Exento/NoSujeto/Recargo/IGIC variantes) y tabla `TarifaImpuesto` por `TerritorioFiscal`.
- [ ] `DocumentoLinea` + `PorcentajeRecargoEquivalencia`, `PorcentajeRetencionIRPF`.
- [ ] Entidades `LibroRegistroIVA` (soportado/repercutido), `Modelo390/349`; servicio `LibroIVAService`.
- [ ] `MotorIVAService` → añadir cálculo IGIC 420 y regularización prorrata anual art.107.

---

## 4. CONTABLE — PGC, LIBROS, DEPÓSITO

### Base legal
- **Código de Comercio art. 25-33**: obligación llevanza contabilidad ordenada, Libro Diario y Libro de Inventarios y Cuentas Anuales; conservación 6 años.
- **PGC RD 1514/2007** + **PGC PYMES RD 1515/2007** (modificado RD 1/2021): cuadro cuentas 9 grupos, principios, modelos Cuentas Anuales (Normal/Abreviado/PYME). Grupos 1 financiación básica, 2 inmovilizado, 3 existencias, 4 acreedores/deudores, 5 financiera, 6 compras/gastos, 7 ventas/ingresos, 8 gastos patrimonio, 9 analítica.
- **Ley 14/2013 art.18 + Instrucción DGRN 12-feb-2015 (BOE-A-2015-1481) + RRM art.329-335**: legalización telemática obligatoria en **Registro Mercantil** domicilio, en soporte electrónico, **dentro de 4 meses tras cierre ejercicio** (ej. cierre 31-dic → límite 30-abr). Presentación vía `legamus.registradores.org` / CIRCE. Libros en blanco no legalizables desde 29-09-2013. Actas también.
- **Depósito Cuentas Anuales:** art. 279 LSC + RRM 365-379: dentro del **mes siguiente a aprobación por Junta** (máx 30-jul si cierre dic y Junta 30-jun). Modelos normalizados (Balance, PyG, ECPN, EFE, Memoria, Informe auditoría si obligatorio). Sanción cierre registral + multa ICAC 1.200-60.000€ (hasta 300k si >60k capital) art.283 LSC.
- **Auditoría:** LAC 22/2015: obligatoria si 2 de 3 (activo >2,85M, cifra negocios >5,7M, empleados >50) dos ejercicios.

### Estado proyecto
`Contabilidad/AsientoContable.cs:12` (Debe=Haber, Serie/Numero, Tipo Apertura/Cierre), `CuentaContable.cs:12` (PGC 9 grupos, Naturaleza), `LibrosOficiales.cs:12` (LibroDiario/LibroMayor/LibroInventariosCuentasAnuales/EjercicioContable con Estado, HashArchivo, FechaLegalización, NumeroLegalización, CuentasDepositadas). Ya contempla hash + fecha legalización.

### Gaps
- [ ] `AsientoContable` → añadir `EsLegalizado` + `HashSHA256` + `FicheroLegalizadoBase64` para descarga Registradores.
- [ ] Servicio `LegalizacionService` (generar XML/CSV Legalia, calcular huella, presentar telemáticamente vía certificado FNMT).
- [ ] `Empresa.RegistroMercantil` → estructurar en `Tomo/Libro/Hoja`.
- [ ] `CuentaContable` seeding oficial PGC 2021 (8 dígitos) — ya EsPGCOficial flag.

---

## 5. COMPRAS / ALMACÉN / LOGÍSTICA / TRAZABILIDAD

### Base legal
- **Reg. CE 178/2002 art.18** (food law): trazabilidad “un paso atrás / un paso adelante”. Cada lote identificado, conservar info 5 años. Ya `TrazabilidadEntities.LoteTrazabilidad` lo implementa literal (§ Un paso atrás/proveedor, Un paso adelante/cliente).
- **Reg. CE 852/2004 higiene + APPCC + RD 191/2011**: control temperatura, PH, PCC, retiradas. Ya `EsPCC`, `ParametrosCriticos`.
- **RASFF / AESAN** (art.19-20 178/2002): notificación retirada inmediata clase I → ya `RetiradaLote` con `NumeroNotificacionRASFF`, `SeveridadRetirada Clase I/II/III`.
- **Facturación compras:** ROF art. 6 + Verifactu también aplica a facturas recibidas si SIF las registra (no genera huella, solo custodia).
- **Stock/Valoración:** PGC norma 10ª: FIFO, PMP. `MovimientoStock` debe enlazar con `AsientoContable` existencias (grupo 3).
- **Transporte ADR / Carta porte**: para logística general (si aplica).

### Gaps (mantener nombres)
- `Articulo` → añadir `CodigoBarras` (EAN13), `UnidadMedida` (UNE-EN), `PesoNeto`, `LotesObligatorios` bool, `FichaAPPCC` FK.
- Proveedor → `Homologado` + `CertificadoCalidad` (ISO22000/BRC).

---

## 6. BANCARIO / COBROS-PAGOS — SEPA

### Base legal vigente 2026
- **Reglamento UE 260/2012** (SEPA end-date) + EPC Rulebooks SCT/SDD 2025 + **Reg. UE 2024/886 SCT Inst** (transferencia inmediata 10s, obligatoria desde 9-ene-2025 emisora y 9-oct-2025 receptora para PSP).
- **ISO 20022**: pain.001.001.03 (SCT), pain.001.001.03 SCT Inst, pain.008.001.02 (SDD Core/B2B), camt.053/054 (extracto), pain.002 (rechazo). Ya `SepaXmlGeneratorService:18-19` genera exactamente esos.
- **PSD2 (Dir. 2015/2366) + verificación beneficiario (VoP) obligatoria desde 5-oct-2025** (Reg. 2024/886 art.5c): PSP debe verificar coincidencia nombre-IBAN antes de ejecutar SCT. `MandatoSEPA.BeneficiarioVerificado` ya existe.
- **Direcciones estructuradas ISO20022 híbridas obligatorias 22-nov-2026** (EPC): adiós no-estructurado. Ya `MandatoSEPA.DeudorCalle/Numero/CP/...` y `OperacionRemesaSEPA.BeneficiarioCalle...` cubren híbrido.
- **Mandato SEPA**: referencia única RUM (35 chars), Creditor Identifier, secuencia FRST/RCUR/FNAL/OOFF, caducidad 13 meses sin uso (`MandatoSEPA.EstaVigente:160`), derecho devolución 8 semanas Core vs 0 B2B.
- **Ley 16/2009 servicios pago + Orden EHA/3360/2008**: plazos adeudo D-1 RCUR, D-5 FRST (antes D-14 para B2B — armonizado).
- **Ley 3/2004 morosidad** (modif. Ley 15/2010): 60 días B2B, 30 días AA.PP. → campo `Vencimiento` ya.

### Estado proyecto
`BancarioService` + `SepaXmlGeneratorService` 100% ISO20022. `CuentaBancaria.CreditorIdentifier:56`, `IBAN/BIC`, `MandatoSEPA.Estado`, `RemesaSEPA` con `XmlGenerado`/`HashXmlSHA256`/`EstadoRemesaSEPA` (Borrador→Conciliada) y `ExtractoBancario` camt.

### Gaps mínimos
- [ ] Añadir `RemesaSEPA.FechaDisponibilidadFondos` y validación VoP previa a `GenerarPain001`.
- [ ] `OperacionRemesaSEPA.CodigoDevolucion` ya existe (AM04, MD01...), completar catálogo ISO.

---

## 7. NÓMINAS / SEGURIDAD SOCIAL

### Base legal
- **ET RD-Leg 2/2015 + Estatuto Trabajadores art. 26-30** (recibo salario RD 1006/1995): nómina debe contener datos empresa/trabajador, categoría, antigüedad, percepciones (salario base, complementos, horas extra, pagas prorrateadas), deducciones (SS 4,7% CC, desempleo, IRPF, MEI, solidaridad), líquido.
- **TGSS SLD (CRET@) desde 2014-2015** sustituye TC1/TC2: la TGSS calcula propuesta; empresa confirma. Documentos **RLC** (Recibo Liquidación Cotizaciones, ex-TC1) y **RNT** (Relación Nominal Trabajadores, ex-TC2). Ya `TC1/TC2_2026.pdf` lo confirma.
- **Canales:** `RED Direct` (<15 trabajadores, navegador) o `SILTRA` (software Java multiplataforma, v4.0.0 01-06-2026 con CNAE-2025 y nueva T-41). Certificado digital obligatorio.
- **Bases y tipos 2026 — Orden PJC/297/2026 de 30-mar (BOE 31-03-2026):**
  - Bases comunes: mínima G1 1.847,40€/mes, máxima general 4.909,50€/mes (art.3 Orden).
  - Tipos 2026 (TGSS): CC 23,60 empresa +4,70 trabajador =28,30%; desempleo indef 5,50+1,55=7,05% (temporal 6,70+1,60=8,30%); FOGASA 0,20; FP 0,60+0,10=0,70; MEI 0,58+0,12=0,70; AT/EP según CNAE (tarifa RD-ley).
  - Cotización adicional solidaridad (2025+), cotización prácticas formativas no laborales 1,61+0,09% (sin desempleo).
- **Plazo:** 1 → último día mes siguiente (enero→28 feb, etc.). Domiciliación último día hábil o CPE.
- **IRPF nómina:** modelos 111 (trimestral) + 190 (anual) TGSS/AEAT.
- **Contratación/bonif.:** RDL 1/2023 + Ley 1/2026 (transición empleo). Gestión vía afiliación RED.

### Estado proyecto
`Nomina.cs:6` mínimo (SalarioBase/Complementos/Deducciones/TotalNeto) + `Empleado.IBAN` para SEPA nóminas + `NominaService`. Falta desagregado legal.

### Gaps
- [ ] `Nomina` → desglosar `BaseCC`, `BaseAT_EP`, `BaseHorasExtra`, `TipoIRPF`, `CuotaSS_Trabajador`, `CuotaAT_EP_Empresa`, `ProrrataPagasExtra`, `CategoriaProfesional`, `GrupoTarifa`, `CCC`.
- [ ] `Empleado` → `GrupoCotizacion` (1-11), `ConvenioColectivo`, `TipoContrato` (indef/temporal/formación/prácticas), `CNAE`, `FechaAntigüedad`, `NumeroAfiliacion` (NAF).
- [ ] Servicio `SiltraService` (generar XML RLC/RNT, exportar pain.001 nóminas vía `SepaXmlGeneratorService`).
- [ ] Entidad `LiquidacionSeguridadSocial` (Periodo, RLC_Base64, RNT_Base64, EstadoTGSS).

---

## 8. CONTROL HORARIO

### Base legal vigente a 31-08-2026
- **RD-ley 8/2019 de 8-mar** (BOE 12-mar-2019, vigor 12-may-2019) modifica art.34.9 ET: registro diario obligatorio inicio/fin jornada de **todos** los trabajadores (completa/parcial, presencial/teletrabajo/móviles/comerciales), conservación **4 años**, acceso trabajadores/representantes/ITSS. La empresa garantiza el sistema (negociación colectiva o decisión tras consulta). Antes solo parciales; ahora universal.
- **Art. 35.5 ET**: registro día a día horas extra, total mensual y entrega copia junto nómina (sobre todo parciales).
- **LISOS art.7.5**: no llevar registro o llevarlo incompleto = **infracción grave 751-7.500€** (baremo actual WoltersKluwer) / 626-6.250€ según guía; leves 70-750€, muy graves manipulación/obstrucción hasta 225.018€. ITSS 20M€ sanciones último año.
- **Proyecto RD registro jornada digital** (tramitación 2024-2026, previsto sept-2026 según Público/EFE, El Día 30-08-2026, aprobado urgente 30-09-2025, dictamen Consejo Estado 23-03-2026 con objeciones protección datos):
  - Prohíbe papel/Excel → solo digital objetivo, fiable, inalterable, trazable (quién/cuándo/desde dónde + historial cambios).
  - Registro detallado: ordinarias/extra/complementarias, pausas no computables, modalidad presencial/teletrabajo.
  - Acceso remoto inmediato ITSS (sin visita). Conservación 4 años. Sanción nueva propuesta **10.000€ por trabajador** (vs 7.500€ empresa).
  - **A 31-08-2026 NO es obligatorio digital** (Sage/WoltersKluwer): sigue válido papel/Excel si fiable, pero Inspección lo rechaza en la práctica. El proyecto no debe presentarse como vigente. Biometría huella restringida AEPD (guidance 2023).
  - Inversión carga prueba (art.217.7 LEC + STS 16-? tutela): empresa debe probar jornada registrada; sin registro, pierde reclamación horas extra.

### Estado proyecto
`ControlHorario.cs:6` (EmpleadoId, Entrada, Salida, Ubicacion, TotalHoras) + `Empleado.PinAcceso` kiosko. Cubre RDL8/2019 mínimo pero no proyecto digital.

### Gaps
- [ ] `ControlHorario` → añadir `TipoRegistro` (Entrada/Salida/PausaInicio/PausaFin), `Modalidad` (Presencial/Teletrabajo), `Origen` (Web/Móvil/Terminal), `IP`, `Geolocalizacion`, `DispositivoId`, `HashEncadenado`, `FirmaEmpleado`, `Estado` (Valido/Corregido), `MotivoCorreccion`, `UsuarioCorreccion`, `FechaCorreccion`, `JornadaComputada` calculada.
- [ ] Entidad `PoliticaControlHorario` (EmpresaId FK, RequiereGeoloc bool, MargenTolerancia, ConvenioId).
- [ ] Servicio `ControlHorarioService` con inalterabilidad (append-only + SHA256 encadenado idéntico a Verifactu) y exportación 4 años.

---

## 9. PROTECCIÓN DE DATOS + FIRMA DIGITAL

### RGPD/LOPDGDD
- **RGPD UE 2016/679** + **LOPDGDD 3/2018**: licitud, minimización, DPIA, registro actividades (art.30), EIPD, DPO, brechas 72h a AEPD, derechos ARSPOPOL, conservación limitación, biometría dato especial (art.9 RGPD → huella para fichaje prohibida AEPD sin base art.6.1 y 9.2).
- Control horario y nóminas son tratamiento con base contrato (6.1.b) + obligación legal (6.1.c) + interés legítimo fraude. Requiere info arts.13-14, registro TRA, evaluación impacto biometría.
- Sanciones hasta 20M€ o 4% global.

### Firma digital — eIDAS
- **Reglamento eIDAS 910/2014** + **Ley 6/2020 servicios confianza** + **EN 319 122/132/142**:
  - Simple/Avanzada (art.26: vinculada firmante, control exclusivo, detectable alteración)/Cualificada (avanzada + certificado cualificado + DSCF QSCD) → equivalente firma manuscrita art.25.2 eIDAS. Ya `FirmaDigitalEntities.TipoFirma`.
  - Formatos **PAdES** (PDF), **XAdES** (XML Facturae), **CAdES** (CMS), **JAdES** (JSON) — ya `FirmaDigitalService` los implementa.
  - **Prestador Cualificado (QTSP)** listado EUTL (ej. FNMT, Camerfirma). `CertificadoDigital.QTSP` ya.
  - **Sello tiempo cualificado RFC3161** (TSA) → `SelloTiempo` con TSAUrl.
  - **Comunicación certificada** (burofax electrónico art.43-44 eIDAS) → `ComunicacionCertificada` con PruebaEntrega/Contenido.
- **Certificado FNMT/AC** must almacenado HSM/Software/Token/Cloud + validación OCSP/CRL (`CertificadoDigital.OCSPUrl/CRLUrl`).
- Verifactu exige firma del registro (opcional según modalidad) con mismo certificado.

### Gaps
- [ ] Entidad `RegistroTratamientoRGPD` (Responsable, Finalidad, Base jurídica, Plazo, Medidas técnicas, EIPD bool).
- [ ] `Empleado` consentimiento informado timestamp + DPIA.

---

## 10. SÍNTESIS PLAN DE APLICACIÓN (sin duplicar — ordenado por prioridad legal)

> **Orden BOE real:** Verifactu (2027) > Control horario digital (sept 2026) > Factura B2B (2027-28) > resto.

### Fase A — Inmediata (sept-2026, proyecto control horario)
1. Extender `ControlHorario` + `ControlHorarioService` inalterable (hash encadenado) y `PoliticaControlHorario`.
2. `ApplicationDbContext` migración `AddControlHorarioDigital`.

### Fase B — Facturación/Verifactu (Q4 2026)
3. Crear `RegistroVerifactuAnulacion`, ampliar `RegistroVerifactu` con campos incidencia/sistema.
4. `VerifactuService` completo (anulaciones, QR ya hecho, remisión AEAT mock→real con certificado).
5. `Empresa.ModalidadVerifactu` + FK Certificado.

### Fase C — IVA/Fiscal (Q4 2026)
6. Tabla `TarifaImpuesto` + enum `TerritorioFiscal` para IGIC/IPSI; `DocumentoLinea.Recargo/IRPF`.
7. `Modelo390/349Service` y ampliar `MotorIVAService` IGIC 420.

### Fase D — FACe/B2B (Q1 2027)
8. Entidad `FacturaElectronica` + `FacturaeService` (XAdES, FACe SOAP, hub B2B).

### Fase E — Contable/Legalización (Q1 2027)
9. Comando `GenerarFicheroLegalia` (hash LibroDiario 4 meses).

### Fase F — Nóminas SLD (Q2 2027)
10. Desglose `Nomina` + `LiquidacionSeguridadSocial` + integración pain.001 nóminas.

> **Validación:** cada extensión debe tener migración EF + test en `ERP.Services.Tests` (ej. `FacturacionWorkflowTests.cs`, `CierreCajaSecurityTests.cs`) y conservar todos los índices únicos actuales (`DocumentoComercial: NumeroDocumento`, `RegistroVerifactu: DocumentoId`).

---
*Fuentes primarias citadas: BOE-A-2023-24840, BOE-A-2024-22138, BOE-A-2025-6600, BOE 03-12-2025 RDL15/2025, BOE-A-1992-28740, BOE-A-2015-1481, BOE 31-03-2026 Orden PJC/297/2026, Reglamento UE 260/2012, Reglamento UE 2024/886, sede AEAT Verifactu FAQ 26-03-2026, ATC IGIC Ley 20/1991.*