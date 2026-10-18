<#
.SYNOPSIS
  En la PC servidor: arranca la API contra otra base MySQL (por ejemplo la de Hostinger) para probarla.

.DESCRIPTION
  No cambia la instalación: el archivo de claves sigue apuntando a la base local.
  La conexión a la base remota se pide por consola y solo vive mientras esta ventana está abierta.
  Pasos:
    1. Pide los datos de la base remota y prueba que se pueda llegar al servidor.
    2. Muestra las migraciones pendientes de esa base y pregunta si aplicarlas.
    3. Detiene el servicio CRV4Api y arranca la API en esta ventana, con la base remota.
    4. Al cerrar con Ctrl+C vuelve a iniciar el servicio, que sigue usando la base local.
  Mientras dura la prueba, todo lo que se cargue en la web queda en la base remota, no en la local.
#>
param(
    [string]$Servidor,
    [int]$Puerto = 3306,
    [string]$Base = 'u605057087_crv4_70',
    [string]$Usuario = 'u605057087_crv4_70'
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Comun.ps1"
Confirmar-Administrador

$exe = Join-Path $CarpetaApi 'Crv4.Api.exe'
if (-not (Test-Path $exe)) { throw "No encuentro $exe. Instalá la API con INSTALAR.cmd." }

Write-Host '--- Base remota ---'
Write-Host 'El servidor figura en Hostinger, en Bases de datos > MySQL remoto (por ejemplo srv1234.hstgr.io).'
if (-not $Servidor) { $Servidor = Read-Host -Prompt 'Servidor MySQL' }
if ([string]::IsNullOrWhiteSpace($Servidor)) { throw 'Falta el servidor.' }
$Servidor = $Servidor.Trim()
$Puerto = [int](Leer-Valor 'Puerto MySQL' "$Puerto")
$Base = Leer-Valor 'Base de datos' $Base
$Usuario = Leer-Valor 'Usuario MySQL' $Usuario
$clave = Leer-Secreto "Contraseña de $Usuario"

Write-Host "Probando la conexión a ${Servidor}:$Puerto ..."
if (-not (Test-NetConnection $Servidor -Port $Puerto -InformationLevel Quiet -WarningAction SilentlyContinue)) {
    $ip = try { (Invoke-RestMethod 'https://api.ipify.org' -TimeoutSec 10).ToString().Trim() } catch { 'no se pudo averiguar' }
    Write-Warning "No se pudo llegar a ${Servidor}:$Puerto."
    Write-Host "En Hostinger, en MySQL remoto, agregá la IP pública de esta PC ($ip) para la base $Base y volvé a probar."
    return
}

$cadena = Armar-CadenaMySql $Servidor $Puerto $Base $Usuario $clave
$env:CRV4_ConnectionStrings__Principal = $cadena
$env:CRV4_ConnectionStrings__Migraciones = $cadena
$servicio = Get-Service $NombreServicio -ErrorAction SilentlyContinue
$detenido = $false
try {
    Write-Host '--- Migraciones de la base remota ---'
    & $exe migrar
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo revisar o migrar la base remota. Revisá el mensaje de arriba.' }

    if ($servicio -and $servicio.Status -eq 'Running') {
        Write-Host 'Deteniendo el servicio (base local)...'
        Stop-Service $NombreServicio
        $detenido = $true
    }
    Write-Host ''
    Write-Host "La API está usando la base remota $Base. Abrí la web como siempre." -ForegroundColor Green
    Write-Host 'Para terminar la prueba apretá Ctrl+C: el servicio vuelve a la base local.' -ForegroundColor Green
    Write-Host ''
    Push-Location $CarpetaApi
    try { & $exe } finally { Pop-Location }
} finally {
    Remove-Item Env:\CRV4_ConnectionStrings__Principal, Env:\CRV4_ConnectionStrings__Migraciones -ErrorAction SilentlyContinue
    if ($detenido) {
        Write-Host 'Iniciando el servicio con la base local...'
        Start-Service $NombreServicio
    }
}
