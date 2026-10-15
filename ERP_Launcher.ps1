# ERP.NET Launcher PowerShell (mejor que .bat)
# Uso: .\ERP_Launcher.ps1

$ErrorActionPreference = "Stop"
$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$apiExe = Join-Path $baseDir "publish\ERP.Api\ERP.Api.exe"
$desktopExe = Join-Path $baseDir "publish\ERP.Desktop\ERP.Desktop.exe"
$dbFile = Join-Path $baseDir "publish\ERP.Api\erp.db"

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    ERP.NET - Iniciando...                    ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Verificar archivos
foreach ($f in @($apiExe, $desktopExe, $dbFile)) {
    if (-not (Test-Path $f)) {
        Write-Error "FALTA: $f"
        Read-Host "Presione Enter para salir"
        exit 1
    }
}

Write-Host "[1/3] Iniciando API server (puerto 5109)..." -ForegroundColor Green
$apiProcess = Start-Process -FilePath $apiExe -ArgumentList "--urls", "http://localhost:5109" -PassThru -WindowStyle Hidden

Write-Host "[2/3] Esperando API en http://localhost:5109..." -ForegroundColor Yellow
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:5109/health" -Method GET -TimeoutSec 2 -ErrorAction Stop
        if ($resp.StatusCode -eq 200) { $ready = $true; break }
    } catch { }
    Start-Sleep -Seconds 1
}

if (-not $ready) {
    Write-Warning "API no respondió en 30s, continuando..."
}

Write-Host "`n[OK] API corriendo en http://localhost:5109 (PID: $($apiProcess.Id))" -ForegroundColor Green
Write-Host "[OK] Abriendo ERP.Desktop..." -ForegroundColor Green

Start-Process -FilePath $desktopExe

Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host "ERP.NET INICIADO" -ForegroundColor Cyan
Write-Host "  API:      http://localhost:5109"
Write-Host "  Desktop:  $desktopExe"
Write-Host "  DB:       $dbFile"
Write-Host "================================================================`n" -ForegroundColor Cyan
Write-Host "Presione Ctrl+C para detener todo..." -ForegroundColor Gray

# Mantener vivo y limpiar al salir
try {
    while ($true) { Start-Sleep -Seconds 5 }
} finally {
    Write-Host "`nDeteniendo ERP.NET..." -ForegroundColor Yellow
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    Write-Host "API detenida. Hasta luego." -ForegroundColor Green
}