@echo off
chcp 65001 >nul
title ERP.NET Launcher

echo.
echo ╔══════════════════════════════════════════════════════════════╗
echo ║                    ERP.NET - Iniciando...                    ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

set BASE_DIR=%~dp0
set API_DIR=%BASE_DIR%publish\ERP.Api
set DESKTOP_EXE=%BASE_DIR%publish\ERP.Desktop\ERP.Desktop.exe

echo [1/3] Verificando API en %API_DIR%...
if not exist "%API_DIR%\ERP.Api.exe" (
    echo ERROR: No se encuentra ERP.Api.exe en %API_DIR%
    echo Ejecute primero: dotnet publish ERP.Api\ERP.Api.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\ERP.Api
    pause
    exit /b 1
)

echo [2/3] Iniciando API server (puerto 5109)...
start "ERP.NET API" /B "%API_DIR%\ERP.Api.exe" --urls "http://localhost:5109"

echo [3/3] Esperando a que la API responda...
timeout /t 5 /nobreak >nul

echo.
echo [OK] API corriendo en http://localhost:5109
echo [OK] Abriendo ERP.Desktop...
echo.

start "" "%DESKTOP_EXE%"

echo.
echo ERP.NET iniciado correctamente.
echo.
echo Para detener: cierre esta ventana y el proceso "ERP.NET API".
echo.
pause