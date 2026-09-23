@echo off
setlocal
chcp 65001 > nul
pushd "%~dp0"
title FuelTrack - Lanzador de Servicios Integrados (Backend + Web + Mobile)

echo ====================================================================
echo        FUELTRACK - ENTORNO DE DESARROLLO Y PRUEBAS EN VIVO
echo ====================================================================
echo.

set "FUELTRACK_ADB=%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe"
if exist "%FUELTRACK_ADB%" (
    "%FUELTRACK_ADB%" reverse tcp:5298 tcp:5298 > nul 2>&1
    "%FUELTRACK_ADB%" reverse tcp:5173 tcp:5173 > nul 2>&1
    echo   ✓ Puertos 5298 (API) y 5173 (Web) ruteados al dispositivo movil por USB.
) else (
    echo   ! ADB no encontrado. Se continuara por red local.
)
echo.

echo [1/3] Verificando Backend API (ASP.NET Core)...
powershell -NoProfile -Command "if (Test-NetConnection -ComputerName 127.0.0.1 -Port 5298 -InformationLevel Quiet) { Write-Host '  ✓ API ya esta activa en http://localhost:5298' } else { Start-Process cmd -ArgumentList '/k title FuelTrack Backend ^& dotnet run --launch-profile http' -WorkingDirectory (Join-Path (Get-Location) 'backend\FuelTrack.Api'); Write-Host '  ✓ Backend iniciado en una nueva ventana.' }"
echo.

echo [2/3] Verificando Frontend Web (Vite + React)...
powershell -NoProfile -Command "if (Test-NetConnection -ComputerName 127.0.0.1 -Port 5173 -InformationLevel Quiet) { Write-Host '  ✓ Frontend Web ya esta activo en http://localhost:5173' } else { Start-Process cmd -ArgumentList '/k title FuelTrack Web ^& npm.cmd run dev -- --host' -WorkingDirectory (Join-Path (Get-Location) 'frontend'); Write-Host '  ✓ Frontend Web iniciado en una nueva ventana.' }"
echo.

echo [3/3] Lanzando App Movil Flutter...
powershell -NoProfile -Command "$dev = (& $env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe devices 2>$null | Select-String 'device$'); if ($dev) { Start-Process cmd -ArgumentList '/k title FuelTrack Mobile ^& flutter run --dart-define=APP_ENV=development --dart-define=AUTH_MODE=local --dart-define=API_BASE_URL=http://127.0.0.1:5298/api/v1' -WorkingDirectory (Join-Path (Get-Location) 'mobile'); Write-Host '  ✓ Flutter run iniciado en dispositivo conectado.' } else { Start-Process cmd -ArgumentList '/k title FuelTrack Mobile ^& flutter run --dart-define=APP_ENV=development --dart-define=AUTH_MODE=local --dart-define=API_BASE_URL=http://127.0.0.1:5298/api/v1' -WorkingDirectory (Join-Path (Get-Location) 'mobile'); Write-Host '  ✓ Flutter run iniciado.' }"

echo.
echo ====================================================================
echo   SERVICIOS DISPONIBLES:
echo   - Backend API:    http://localhost:5298/api/v1
echo   - Frontend Web:   http://localhost:5173/
echo   - Tickets Web:    http://localhost:5173/tickets
echo ====================================================================
echo.
popd
pause
endlocal
