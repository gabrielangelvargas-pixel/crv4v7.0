@echo off
rem Doble clic para instalar o actualizar CRV4 7.0 en esta PC. Pide permiso de administrador.
net session >nul 2>&1
if %errorlevel% neq 0 (
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%~dp0' -Recurse | Unblock-File; & '%~dp0Instalar-Todo.ps1'"
echo.
pause
