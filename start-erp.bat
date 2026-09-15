@echo off
title Iniciar ERP - Ciclo Legal 
color 0A
cls

echo.
echo ==========================================
echo  Iniciando ERP - Ciclo Legal 
echo ==========================================
echo.

echo 1. Iniciando API REST (puerto 5109)...
start "API-ERP" /min cmd /c "cd C:\Users\josearregui\Desktop\Proyectos\ERP .NET\ERP.Api & dotnet run --urls http://localhost:5109"
ping 127.0.0.1 -n 3 >nul

echo.
echo 2. Iniciando Interface Web (Blazor)...
start "Web-ERP" /min cmd /c "cd C:\Users\josearregui\Desktop\Proyectos\ERP .NET\ERP.Web & dotnet run"
echo.

echo.
echo ==========================================
echo  Los servicios se están iniciando...
echo  Abra su navegador en: http://localhost:5109
echo ==========================================
echo.

pause