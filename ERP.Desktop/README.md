# ERP.Desktop — Aplicación de Escritorio (rama `escritorio`)

Esta rama convierte el ERP tal cual está (web) en app de escritorio Windows sin reescribir UI.

## Arquitectura

* **Reutilización 100%**: `ERP.Web` (Blazor WASM) + `ERP.Api` + `ERP.Data` + `ERP.Domain` + `ERP.Services` se usan sin cambios.
* **Shell WPF + WebView2** (`ERP.Desktop`): ventana nativa `net9.0-windows` que aloja un `WebView2` (`Microsoft.Web.WebView2 1.0.2957.106`) navegando a `http://localhost:5109`.
  - `ERP.Api` ya hace `UseStaticFiles() + MapFallbackToFile("index.html")` → servir el Blazor en el mismo puerto que la API.
* **BBDD idéntica**: mismo `erp.db` maestro (todos los usuarios duplicados) + `Gestion*.db` por empresa (`TenantDatabaseService.cs:184`). La ruta se resuelve junto al exe o `%LocalAppData%/ERP`; el escritorio no crea BBDD paralela.
* **Ciclo de vida**: `MainWindow.xaml.cs` comprueba `GET {ApiUrl}`; si no responde lanza `dotnet ERP.Api.dll --urls {ApiUrl}` como proceso hijo (buscando `ERP.Api.dll` junto al exe o en `ERP.Api/bin/Release/net9.0`). Al cerrar la ventana mata el hijo.

## Flujo pasillo / privado / logout

Hereda los fixes de `wsl`: `AuthService.Logout()` borra `authToken + erp_empresas_active_id + erp_onboarding_dismissed + erp_* + sessionStorage` y `EmpresaService.ClearAsync()`, `OnboardingWizard` virgen para `admin@erp.local`, y privado nunca ve wizard.

## Ejecutar

```powershell
# Terminal 1 — API (o lo lanza el escritorio automáticamente)
dotnet run --project ERP.Api --urls http://localhost:5109

# Terminal 2 — Escritorio
dotnet run --project ERP.Desktop
# o
dotnet build ERP.Desktop -c Release
.\ERP.Desktop\bin\Release\net9.0-windows\ERP.Desktop.exe
```

Configurar URL alternativa: copiar `ERP.Desktop/ERP.Desktop.json.example` a `ERP.Desktop.json` junto al exe.

## Publicar instalador

```powershell
dotnet publish ERP.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
# Salida: ERP.Desktop/bin/Release/net9.0-windows/win-x64/publish/ERP.Desktop.exe
# Requiere WebView2 Runtime (preinstalado en Windows 11).
```

## Próximos pasos (no incluidos)

* MAUI Blazor Hybrid (`maui-desktop` workload) para macOS/Linux — requiere `dotnet workload install maui`.
* Auto-updater / MSIX / Squirrel.
* Incrustar API in-process (Kestrel dentro de WPF) en lugar de proceso hijo — requiere refactorizar `ERP.Api/Program.cs` a HostBuilder reutilizable.
