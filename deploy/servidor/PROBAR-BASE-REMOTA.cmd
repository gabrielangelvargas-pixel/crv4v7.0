@echo off
rem Doble clic para probar la API contra la base remota (Hostinger). Pide permiso de administrador.
rem Al cerrar con Ctrl+C la API vuelve a la base local.
net session >nul 2>&1
if %errorlevel% neq 0 (
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%~dp0' -Recurse | Unblock-File; & '%~dp0Probar-BaseRemota.ps1'"
echo.
pause
