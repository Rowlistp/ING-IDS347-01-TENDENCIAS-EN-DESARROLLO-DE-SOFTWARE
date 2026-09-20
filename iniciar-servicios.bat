@echo off
chcp 65001 > nul
title FuelTrack - Lanzador de Servicios Integrados (Backend + Web + Mobile)

echo ====================================================================
echo        FUELTRACK - ENTORNO DE DESARROLLO Y PRUEBAS EN VIVO
echo ====================================================================
echo.

set ADB="%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe"

echo [1/4] Configurando comunicación USB con el celular (ADB Reverse)...
if exist %ADB% (
    %ADB% reverse tcp:5298 tcp:5298 > nul 2>&1
    %ADB% reverse tcp:5173 tcp:5173 > nul 2>&1
    echo   ✓ Puertos 5298 (API) y 5173 (Web) ruteados al dispositivo movil.
) else (
    echo   ! ADB no encontrado en la ruta por defecto. Se continuara por red local.
)
echo.

echo [2/4] Verificando Backend API (ASP.NET Core)...
powershell -NoProfile -Command "if (Test-NetConnection -ComputerName 127.0.0.1 -Port 5298 -InformationLevel Quiet) { Write-Host '  ✓ Backend ya esta activo en http://localhost:5298' } else { Start-Process cmd -ArgumentList '/k title FuelTrack Backend ^& cd backend\FuelTrack.Api ^& dotnet run' -WindowStyle Normal; Write-Host '  ✓ Backend iniciado en una nueva ventana.' }"
echo.

echo [3/4] Verificando Frontend Web (Vite + React)...
powershell -NoProfile -Command "if (Test-NetConnection -ComputerName 127.0.0.1 -Port 5173 -InformationLevel Quiet) { Write-Host '  ✓ Frontend Web ya esta activo en http://localhost:5173' } else { Start-Process cmd -ArgumentList '/k title FuelTrack Web ^& cd frontend ^& npm.cmd run dev -- --host' -WindowStyle Normal; Write-Host '  ✓ Frontend Web iniciado en una nueva ventana.' }"
echo.

echo [4/4] Lanzando App Movil en tu celular Samsung...
powershell -NoProfile -Command "$dev = (& $env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe devices | Select-String 'device$'); if ($dev) { Start-Process cmd -ArgumentList '/k title FuelTrack Mobile ^& cd mobile ^& flutter run -d RF8M73MHLWZ' -WindowStyle Normal; Write-Host '  ✓ Flutter run iniciado en tu celular Samsung (RF8M73MHLWZ).' } else { Start-Process cmd -ArgumentList '/k title FuelTrack Mobile ^& cd mobile ^& flutter run' -WindowStyle Normal; Write-Host '  ✓ Flutter run iniciado en dispositivo disponible.' }"

echo.
echo ====================================================================
echo   SERVICIOS ACTIVOS:
echo   - Backend API:    http://localhost:5298/api/v1
echo   - Frontend Web:   http://localhost:5173/
echo   - Generador QR:   http://localhost:5173/qr.html
echo   - Tickets Web:    http://localhost:5173/tickets
echo   - App Movil:      Corriendo en tu Samsung Galaxy Note 10+
echo ====================================================================
echo.
pause
