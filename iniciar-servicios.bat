@echo off
setlocal
chcp 65001 > nul
pushd "%~dp0"
title FuelTrack - Entorno de desarrollo

echo Requiere .NET, Node, Flutter, Android SDK y base de datos configurados.
echo Use autenticacion local habilitada en la API para este lanzador.
echo.
set "FUELTRACK_ADB=%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe"
if exist "%FUELTRACK_ADB%" (
    "%FUELTRACK_ADB%" reverse tcp:5298 tcp:5298
    if errorlevel 1 echo No se pudo configurar USB: revise dispositivo y autorizacion.
) else (
    echo ADB no encontrado. Configure USB antes de usar la app con 127.0.0.1.
)

powershell -NoProfile -Command "if (Test-NetConnection -ComputerName 127.0.0.1 -Port 5298 -InformationLevel Quiet) { Write-Host 'API ya escucha en 5298.' } else { Start-Process cmd -ArgumentList '/k dotnet run --launch-profile http' -WorkingDirectory (Join-Path (Get-Location) 'backend\FuelTrack.Api') }"
powershell -NoProfile -Command "if (Test-NetConnection -ComputerName 127.0.0.1 -Port 5173 -InformationLevel Quiet) { Write-Host 'Web ya escucha en 5173.' } else { Start-Process cmd -ArgumentList '/k npm.cmd run dev -- --host' -WorkingDirectory (Join-Path (Get-Location) 'frontend') }"
powershell -NoProfile -Command "Start-Process cmd -ArgumentList '/k flutter run --dart-define=APP_ENV=development --dart-define=AUTH_MODE=local --dart-define=API_BASE_URL=http://127.0.0.1:5298/api/v1' -WorkingDirectory (Join-Path (Get-Location) 'mobile')"

echo.
echo Procesos solicitados. Compruebe los resultados en cada ventana.
echo API: http://localhost:5298/api/v1
echo Web y QR autenticado: http://localhost:5173/tickets
echo Flutter solicitara dispositivo si hay varios disponibles.
echo Para emulador sin USB reverse use la configuracion de VS Code.
popd
pause
endlocal
