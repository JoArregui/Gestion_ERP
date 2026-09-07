# Manual de Usuario - ERP.NET

## Versión 1.0 | Septiembre 2026

---

### 1. Introducción

**ERP.NET** es una solución de planificación empresarial (ERP) completa diseñada específicamente para PYMES y autónomos en España, cumpliendo con la normativa legal 2026. El sistema integra módulos de ventas, compras, inventario, RR.HH., fiscalidad, verifactu, factura electrónica y contabilidad.

**Arquitectura**: Aplicación Web Blazor con API REST subyacente.
**Acceso**: Navegador web en `http://localhost:5109`
**Base de datos**: SQLite por defecto o SQL Server configurable.

---

### 2. Acceso y Autenticación

#### 2.1. Login
1. Abrir el navegador e ingresar a `http://localhost:5109`
2. Hacer clic en "Acceder" y completar el formulario de login
3. Ingresar credenciales:
   - **Usuario administrador**: `admin@erp.local` / `Admin123!`
   - **Usuarios personalizados**: Credenciales proporcionadas por el administrador

#### 2.2. Seguridad
- Tokens **JWT** Bearer para todas las solicitudes
- **Políticas de autorización** basadas en roles/permisos
- 6 permisos principales: `Usuarios`, `Roles`, `Ver`, `Editar`, `Stock`, `Facturar`
- Cierre de sesión automático después de 30 minutos de inactividad

---

### 3. Módulos Principales

#### 3.1. Ventas (TPV y Facturación)
| Funcionalidad | Descripción |
|--------------|-------------|
| **Nueva Venta** | Acceder desde el menú Ventas → Nueva Venta. Agregar artículos, cantidades y aplicar descuentos. |
| **Presupuestos** | Crear presupuestos preliminares con validação de IVA. |
| **Albaranes** | Generar albaranes de entrega vinculados a pedidos de cliente. |
| **TPV Punto de Venta** | Interfaz rápida para ventas al mostrador; emisión inmediata de facturas. |
| **Listado Documentos** | Consultar historial de facturas, presupuestos y albaranes con filtros por fecha, cliente, estado. |

**Ruta**: Menú lateral → Ventas

#### 3.2. Compras
| Funcionalidad | Descripción |
|--------------|-------------|
| **Pedidos Proveedores** | Crear y gestionar pedidos a proveedores con seguimiento de estado. |
| **Recepción** | Registrar mercancía recibida y actualizar automáticamente el stock. |
| **Listado Compras** | Historial completo de pedidos y facturas de proveedores. |

**Ruta**: Menú lateral → Compras

#### 3.3. Stock e Inventario
| Funcionalidad | Descripción |
|--------------|-------------|
| **Ajustes de Stock** | Modificar cantidades físicas por pérdidas, daños o inventarios cíclicos. |
| **Valoración** | Ver valor total del stock valorado por costo unitario y IVA. |
| **Etiquetas QR** | Generar y leer códigos QR para identificación de artículos. |
| **Auditoría** | Registrar todas las movimientaciones de stock con usuario y fecha. |

**Ruta**: Menú lateral → Stock

#### 3.4. RR.HH. y Nóminas
| Funcionalidad | Descripción |
|--------------|-------------|
| **Kiosko Fichajes** | Los empleados fichan entrada/salida; registro horario RDL 8/2019. |
| **Empleados** | Alta de empleados, datos personales, contratos, asignación de cargos. |
| **Nóminas** | Generar nóminas mensuales con cálculo automático de IRPF, Seguridad Social. |
| **Control Horario** | Ver horarios, horas extra, faltas y prórrogas. |

**Ruta**: Menú lateral → RR.HH.

#### 3.5. Fiscalidad e IVA
| Funcionalidad | Descripción |
|--------------|-------------|
| **Configuración IVA/IGIC/IPSI** | Definir tipos impositivos (general, reducido, superreducido) y territoriales (Península, Canarias, Ceuta/Melilla). |
| **Liquidaciones Modelo 303** | Generar liquidaciones trimestrales de IVA con datos concursados. |
| **Tarifas Personalizadas** | Configurar tarifas específicas por artículo o cliente/proveedor. |

**Ruta**: Menú lateral → Fiscal → IVA

#### 3.6. Verifactu (Antifraude)
| Funcionalidad | Descripción |
|--------------|-------------|
| **Generar Registro Alta** | Crear registros SIF cadena de hash SHA256 parafacturación antifraude. |
| **Anulación de Registros** | Anular registros previamente generados manteniendo integridad. |
| **Códigos QR** | Generar códigos QR visibles en facturas para verificación SII. |

**Ruta**: Menú lateral → Verifactu

#### 3.7. Factura Electrónica (FACe)
| Funcionalidad | Descripción |
|--------------|-------------|
| **Formato Facturae 3.2.x** | Generar archivos XML conformes al modelo Facturae 3.2. |
| **Envío FACe** | Enviar facturas al sistema de la AEAT mediante el punto de acceso electrónico. |
| **Firma XAdES** | Aplicar firma digital electrónica para validez jurídica. |

**Ruta**: Menú lateral → Facturae

#### 3.8. Contabilidad
| Funcionalidad | Descripción |
|--------------|-------------|
| **Asientos Contables** | Registrar asientos de entrada, salidas, traspasos y ajustes. |
| **Libros Oficiales** | Generar Libro Diario y Libro Mayor automáticamente. |
| **Legalización** | Preparar libros para su legalización en el Registro Mercantil. |

**Ruta**: Menú lateral → Contabilidad

#### 3.9. RGPD (Protección de Datos)
| Funcionalidad | Descripción |
|--------------|-------------|
| **Registro Actividades Tratamiento** | Catalogar ficheros y actividades de tratamiento de datos personales. |
| **Delegado DPO** | Configurar datos del Delegado de Protección de Datos. |
| **Derechos Ciudadanos** | gestionar solicitudes de acceso, rectificación, supresión. |

**Ruta**: Menú lateral → RGPD

#### 3.10. Tesorería y SEPA
| Funcionalidad | Descripción |
|--------------|-------------|
| **Remesas Bancarias** | Generar archivos `.pain.001` para transferencias y pagos nómina. |
| **Mandatos SEPA** | Gestionar autorizaciones de débito directo. |
| **Validación VoP** | Verificar beneficiario contra catálogo de entidades bancarias. |

**Ruta**: Menú lateral → Tesorería

---

### 4. Perfiles de Usuario y Permisos

| Perfil | Permisos | Acceso Principal |
|--------|----------|------------------|
| **Administrador** | Todos los permisos | Configuración completa, todos los módulos |
| **Gestor Fiscal** | Ver, Editar, Facturar | Módulos fiscales, IVA, Verifactu, Facturae |
| **Vendedor/Cajero** | Ver, Facturar | TPV, nuevas ventas, listado documentos |
| **Responsable Compras** | Ver, Editar | Pedidos, proveedores, recepción |
| **RR.HH.** | Ver, Editar | Empleados, nóminas, fichajes |
| **Contable** | Ver, Editar | Asientos, libros oficiales, legalización |
| **Tesorero** | Ver, Editar | Remesas SEPA, mandatos, valida beneficiario |

**Nota**: Los permisos se asignan por rol en la sección **Administración → Usuarios y Roles**.

---

### 5. Tareas Comunes del Día a Día

#### 5.1. Inicio de Turno (Vendedor)
1. Acceder al sistema con credenciales de vendedor
2. Seleccionar "Nueva Venta" en el módulo Ventas
3. Buscar artículos por código, nombre o categoría
4. Agregar artículos al carrito; aplicar descuentos si corresponde
5. Emitir factura al cliente (obligatorio para operaciones imponibles)
6. Cerrar venta y emitir comprobante (PDF/impreso)

#### 5.2. Registro de Fichaje (Empleado)
1. Acceder al módulo RR.HH. → Kiosko Fichajes
2. Seleccionar "Fichar Entrada" o "Fichar Salida"
3. El sistema registra hora y fecha automáticamente
4. Consultar historial de fichajes en "Control Horario"

#### 5.3. Generar Liquidación IVA (Gestor Fiscal)
1. Acceder a Fiscal → IVA → Liquidaciones Modelo 303
2. Seleccionar trimestre correspondiente
3. Revisar datos autoliquidados (ventas, adquisiciones, intracomunitarias)
4. Confirmar y generar archivo para presentación ante la AEAT

#### 5.4. Emitir Factura Electrónica (Facturae)
1. En Ventas → Listado Documentos, seleccionar factura pendiente
2. Hacer clic en "Enviar a FACe"
3. Elegir tipo de firma (certificado digital instalado en el equipo)
4. Confirmar envío; el sistema generará el acuse de recibo

#### 5.5. Ajuste de Stock (Responsable Almacén)
1. Menú Stock → Ajustes de Stock
2. Seleccionar artículo y justificar motivo (inventario, pérdida, daño)
3. Ingresar cantidad real; el sistema actualiza valoración automáticamente
4. Revisar auditoría para historial completo

---

### 6. Cuestiones Importantes y Consideraciones

#### 6.1. Cumplimiento Legal 2026
- **Verifactu**: Obligatorio para facturas emitidas a partir de julio de 2025. El sistema genera registros SIF con hash SHA256 encadenado.
- **Factura Electrónica (FACe)**: Obligatorio para facturas a administraciones públicas. Formato Facturae 3.2.x con firma XAdES.
- **Control Horario Digital**: Registro obligatorio de entrada/salida por RDL 8/2019. El módulo RR.HH. cumple este requisito.
- **IVA Territorial**: Configuración correcta según ubicación (Península/Canarias/CEUTA/Melilla) afecta tipos IGIC/IPSI.

#### 6.2. Configuración Inicial Recomendada
1. Definir **empresa** completa: nombre, NIF, dirección, régimen fiscal.
2. Configurar **tipos IVA/IGIC/IPSI** según actividad y ubicación geográfica.
3. Dar de alta **artículos/servicios** con códigos, precios y cuentas contables.
4. Dar de alta **clientes y proveedores** con datos fiscales completos (código cliente/proveedor, condiciones pago).
5. Definir **usuarios y roles** según responsabilidades laborales.
6. Configurar **entidades bancarias** para generación de remesas SEPA.

#### 6.3. Buenas Prácticas
- **Realizar respaldos** de la base de datos antes de operaciones masivas.
- **Firmar digitalmente** todas las facturas y comunicaciones con la AEAT.
- **Mantener actualizados** los tipos impositivos ante cambios legislativos.
- **Auditar periódicamente** el módulo Stock por diferencias físicas.
- **Capacitar a usuarios** en el uso del TPV para operaciones rápidas y sin errores.
- **Respaldo automático**: El sistema incluye servicio de seed data en el primer arranque.

#### 6.4. Solución de Problemas Comunes

| Síntoma | Causa Posible | Solución |
|---------|---------------|----------|
| No puedo acceder al sistema | Credenciales incorrectas o bloqueo por intentos fallidos | Verificar usuario/contraseña; contactar al administrador si bloqueado |
| No aparece el menú de un módulo | Falta de permiso correspondiente | Solicitar al administrador la asignación del permiso necesario |
| Error al generar factura electrónica | Certificado digital no instalado o caducado | Instalar certificado válido en el almacén de Windows/Mac y reiniciar |
| El IVA se calcula incorrecto | Tipo impositivo no configurado para el artículo/cliente | Revisar y configurar tipo IVA en ficha del artículo o forma de pago |
| No puedo generar remesa SEPA | Datos bancarios incompletos en proveedor/cliente | Completar campos Cuenta IBAN, entidad, oficina, número de cuenta en ficha |
| El sistema va lento | Base de datos sin optimizar o muchos registros | Ejecutar mantenimiento; archivar documentos antiguos; consultar con administrador |

---

### 7. Mantenimiento y Soporte

- **Actualizaciones**: Verificar nuevas versiones mensualmente; aplicar parches de seguridad.
- **Logs**: Los registros de actividad se guardan en `Logs/` del proyecto; útiles para diagnóstico.
- **Soporte Técnico**: Para consultas avanzadas, contactar con el equipo de desarrollo con la siguiente información:
  - Pantalla/error exacto
  - Pasos para reproducir
  - Datos relevantes (cliente, artículo, fecha)
  - Versión del navegador y del sistema

---

### 8. Glosario Abreviado

| Abreviatura | Significado |
|-------------|-------------|
| AEAT | Agencia Estatal de Administración Tributaria |
| FACe | Factura Electrónica (sistema de la AEAT) |
| IVA | Impuesto sobre el Valor Añadido |
| IGIC | Impuesto General Indirecto Canario |
| IPSI | Impuesto sobre los Productos de primera Necesidad e el Instituto |
| SIF | Sistema de Identificación Fiscal (Verifactu) |
| SEPA | Single Euro Payments Area |
| XAdES | XML Advanced Electronic Signatures |
| PGC | Plan General Contable |
| RDL | Real Decreto- Ley |

---

**Fin del Manual de Usuario**

*Para sugerencias, correos o dudas sobre este manual, contactar con el departamento de soporte del proyecto ERP.NET.*