<#
.SYNOPSIS
  En la PC servidor: hace una copia de seguridad de la base y aplica las migraciones pendientes.

.DESCRIPTION
  Es la única forma en que cambia la estructura de la base: la API nunca lo hace sola al arrancar.
  Usa una cuenta de MySQL con permisos de estructura (por ejemplo crv4_migraciones), distinta de la
  cuenta de la API. Su contraseña se pide cada vez y no se guarda en ningún lado.
  Pasos:
    1. Detiene el servicio CRV4Api.
    2. Respalda la base con mysqldump en C:\CRV4\respaldos\ (si hay mysqldump instalado).
    3. Muestra las migraciones pendientes, pide confirmación y las aplica.
    4. Vuelve a iniciar el servicio.
#>
param(
    [string]$Servidor = 'localhost',
    [int]$Puerto = 3306,
    [string]$Base = 'crv4_v7',
    [string]$Usuario = 'crv4_migraciones'
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Comun.ps1"
Confirmar-Administrador

$exe = Join-Path $CarpetaApi 'Crv4.Api.exe'
if (-not (Test-Path $exe)) { throw "No encuentro $exe. Instalá la API con Instalar-Api.ps1." }

$clave = Leer-Secreto "Contraseña de MySQL para $Usuario"

$servicio = Get-Service $NombreServicio -ErrorAction SilentlyContinue
if ($servicio -and $servicio.Status -eq 'Running') {
    Write-Host 'Deteniendo el servicio...'
    Stop-Service $NombreServicio
}

$mysqldump = Buscar-MySqlDump
if ($mysqldump) {
    New-Item -ItemType Directory -Force -Path $CarpetaRespaldos | Out-Null
    $archivo = Join-Path $CarpetaRespaldos ("{0}-{1:yyyyMMdd-HHmmss}.sql" -f $Base, (Get-Date))
    Write-Host "Respaldando $Base en $archivo ..."
    $env:MYSQL_PWD = $clave
    try {
        & $mysqldump --host=$Servidor --port=$Puerto --user=$Usuario --single-transaction --routines --triggers --no-tablespaces --result-file=$archivo $Base
        if ($LASTEXITCODE -ne 0) { throw 'mysqldump falló. No se aplicó ninguna migración.' }
    } finally { Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue }
} else {
    Write-Warning 'No encontré mysqldump.exe: no se hizo respaldo automático.'
    if ((Read-Host '¿Seguir igual? (s/N)') -ne 's') { return }
}

$env:CRV4_ConnectionStrings__Migraciones = Armar-CadenaMySql $Servidor $Puerto $Base $Usuario $clave
try {
    & $exe migrar
    $resultado = $LASTEXITCODE
} finally { Remove-Item Env:\CRV4_ConnectionStrings__Migraciones -ErrorAction SilentlyContinue }

if ($servicio) {
    Write-Host 'Iniciando el servicio...'
    Start-Service $NombreServicio
}
if ($resultado -ne 0) { throw 'La migración no se completó. Revisá el mensaje de arriba.' }
