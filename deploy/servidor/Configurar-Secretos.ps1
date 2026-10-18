<#
.SYNOPSIS
  En la PC servidor: guarda las claves de la API en C:\CRV4\config\crv4.secretos.json.

.DESCRIPTION
  Ese archivo vive solo en el servidor, fuera de la carpeta de la API (las actualizaciones no lo tocan)
  y fuera del repositorio. Solo lo pueden leer los administradores, SYSTEM y el servicio CRV4Api.
  Pide por consola:
    - la conexión a MySQL de la cuenta de la API (solo SELECT/INSERT/UPDATE/DELETE)
    - la contraseña del PFX creado con Crear-Certificado.ps1
    - el dominio de Hostinger desde el que se abre la web (para CORS)
  La clave de firma de sesiones (JWT) se genera sola al azar. Volver a generarla cierra todas las sesiones.
  Se puede ejecutar de nuevo para cambiar cualquier dato.
#>
param(
    [int]$Puerto = 5443,
    [switch]$RegenerarClaveSesiones
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Comun.ps1"
Confirmar-Administrador

if (-not (Get-Service $NombreServicio -ErrorAction SilentlyContinue)) { throw 'Primero instalá el servicio con Instalar-Api.ps1.' }

$actual = $null
if (Test-Path $ArchivoSecretos) { $actual = Get-Content $ArchivoSecretos -Raw | ConvertFrom-Json }

Write-Host '--- Base de datos (cuenta de la API) ---'
$servidor = Leer-Valor 'Servidor MySQL' 'localhost'
$puertoDb = [int](Leer-Valor 'Puerto MySQL' '3306')
$base = Leer-Valor 'Base de datos' 'crv4_v7'
$usuario = Leer-Valor 'Usuario MySQL de la API' 'crv4_api'
$claveDb = Leer-Secreto "Contraseña de $usuario"

Write-Host '--- Certificado HTTPS ---'
$pfx = Leer-Valor 'Archivo PFX' (Join-Path $CarpetaCertificados 'crv4-api.pfx')
if (-not (Test-Path $pfx)) { throw "No existe $pfx. Ejecutá Crear-Certificado.ps1." }
$clavePfx = Leer-Secreto 'Contraseña del PFX'
try { [void][Security.Cryptography.X509Certificates.X509Certificate2]::new($pfx, $clavePfx) }
catch { throw 'La contraseña del PFX no es correcta.' }
icacls $pfx /grant "NT SERVICE\${NombreServicio}:R" | Out-Null

Write-Host '--- Web ---'
$origenActual = if ($actual) { @($actual.Cors.OrigenesPermitidos)[0] } else { $null }
$origen = Leer-Valor 'Dirección de la web en Hostinger (con https://)' $(if ($origenActual) { $origenActual } else { 'https://' })
if ($origen -notmatch '^https://[^/]+$') { throw 'La dirección tiene que ser del tipo https://dominio.com, sin barra final.' }

$claveJwt = if ($actual) { $actual.Jwt.Clave } else { $null }
if ($RegenerarClaveSesiones -or -not $claveJwt) {
    $bytes = New-Object byte[] 64
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $claveJwt = [Convert]::ToBase64String($bytes)
    Write-Host 'Se generó una clave de sesiones nueva.'
}

$secretos = [ordered]@{
    ConnectionStrings = [ordered]@{ Principal = (Armar-CadenaMySql $servidor $puertoDb $base $usuario $claveDb) }
    Jwt               = [ordered]@{ Clave = $claveJwt }
    Cors              = [ordered]@{ OrigenesPermitidos = @($origen) }
    Kestrel           = [ordered]@{
        Endpoints = [ordered]@{
            Https = [ordered]@{
                Url         = "https://0.0.0.0:$Puerto"
                Certificate = [ordered]@{ Path = $pfx; Password = $clavePfx }
            }
        }
    }
}

New-Item -ItemType Directory -Force -Path $CarpetaConfig | Out-Null
icacls $CarpetaConfig /inheritance:r /grant:r '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-18:(OI)(CI)F' "NT SERVICE\${NombreServicio}:(OI)(CI)R" | Out-Null
$secretos | ConvertTo-Json -Depth 6 | Set-Content -Path $ArchivoSecretos -Encoding UTF8

Write-Host ''
Write-Host "Claves guardadas en $ArchivoSecretos (solo lo leen administradores y el servicio)."
Write-Host 'Siguiente paso: Aplicar-Migraciones.ps1 (y Crear-Administrador.ps1 la primera vez). Después: Start-Service CRV4Api'
