<#
.SYNOPSIS
  En la PC servidor: crea el primer usuario administrador de CRV4 (o uno nuevo si se pierde el acceso).
  Pide los datos por consola; la contraseña no queda guardada en ningún archivo.
#>
param(
    [string]$Servidor = 'localhost',
    [int]$Puerto = 3306,
    [string]$Base = 'crv4_datos',
    [string]$Usuario = 'crv4_api',
    # Si se pasa, no se pregunta (lo usa Instalar-Todo.ps1).
    [string]$Clave
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Comun.ps1"
Confirmar-Administrador

$exe = Join-Path $CarpetaApi 'Crv4.Api.exe'
$clave = if ($Clave) { $Clave } else { Leer-Secreto "Contraseña de MySQL para $Usuario" }
$env:CRV4_ConnectionStrings__Principal = Armar-CadenaMySql $Servidor $Puerto $Base $Usuario $clave
try { & $exe crear-admin }
finally { Remove-Item Env:\CRV4_ConnectionStrings__Principal -ErrorAction SilentlyContinue }
