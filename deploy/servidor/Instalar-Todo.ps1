<#
.SYNOPSIS
  Instala o actualiza CRV4 7.0 en esta PC de una sola vez. Se ejecuta con doble clic en INSTALAR.cmd.

.DESCRIPTION
  Hace en orden lo mismo que los scripts sueltos, sin preguntar nada salvo la contraseña de root de MySQL
  y los datos del administrador:
    1. Comprueba que esté el runtime de ASP.NET Core 10 (si falta, lo instala con winget).
    2. Crea o actualiza las cuentas crv4_api y crv4_migraciones con contraseñas nuevas al azar.
       Esas contraseñas no se muestran ni se guardan en ningún lado salvo la de la API, que queda en
       C:\CRV4\config\crv4.secretos.json (solo la leen los administradores y el servicio).
    3. Instala el servicio CRV4Api y el certificado HTTPS para las IP de esta PC.
    4. Respalda crv4_datos en C:\CRV4\respaldos y aplica las migraciones pendientes.
    5. Ofrece crear un usuario administrador y prueba que la API responda.
  Se puede volver a ejecutar para actualizar: rota las contraseñas de MySQL y reinstala la API.
#>
param([string]$Base = 'crv4_datos', [int]$Puerto = 5443)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Comun.ps1"
Confirmar-Administrador

function Paso([string]$Texto) { Write-Host ''; Write-Host "== $Texto" -ForegroundColor Cyan }

function Clave-Azar {
    $bytes = New-Object byte[] 18
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    # Letras, números y guion bajo: no hace falta escaparla en SQL ni en la cadena de conexión,
    # y cumple la política de contraseñas de MySQL (mayúscula, minúscula, número y símbolo).
    return 'C4k_' + (([Convert]::ToBase64String($bytes)) -replace '[^A-Za-z0-9]', 'x')
}

function Buscar-MySql {
    $comando = Get-Command mysql.exe -ErrorAction SilentlyContinue
    if ($comando) { return $comando.Source }
    $candidato = Get-ChildItem 'C:\Program Files\MySQL' -Recurse -Filter mysql.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($candidato) { return $candidato.FullName }
    throw 'No encontré mysql.exe. ¿MySQL está instalado en esta PC?'
}

function Ejecutar-MySql([string]$Mysql, [string]$ClaveRoot, [string]$Sql) {
    # mysql escribe avisos en stderr; con 'Stop' PowerShell los tomaría como error.
    $ErrorActionPreference = 'Continue'
    $env:MYSQL_PWD = $ClaveRoot
    try {
        $salida = $Sql | & $Mysql --user=root --host=localhost --batch --skip-column-names 2>&1
        $errores = $salida | Where-Object { $_ -is [Management.Automation.ErrorRecord] -and "$_" -notmatch '\[Warning\]|Using a password' }
        if ($LASTEXITCODE -ne 0) {
            if ("$errores" -match '1045') { throw 'La contraseña de root de MySQL no es correcta.' }
            throw "MySQL devolvió un error: $errores"
        }
        return @($salida | Where-Object { $_ -is [string] })
    } finally { Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue }
}

function Tiene-Runtime {
    try { return [bool]((& dotnet --list-runtimes 2>$null) -match 'Microsoft\.AspNetCore\.App 10\.') } catch { return $false }
}

Write-Host 'CRV4 Mayorista 7.0 - instalación' -ForegroundColor Green

Paso 'Runtime de .NET'
if (-not (Tiene-Runtime)) {
    Write-Host 'Falta ASP.NET Core Runtime 10. Instalándolo...'
    winget install --id Microsoft.DotNet.AspNetCore.10 --exact --silent --accept-package-agreements --accept-source-agreements
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
    if (-not (Tiene-Runtime)) {
        throw 'No se pudo instalar el runtime. Bajalo de https://dotnet.microsoft.com/download/dotnet/10.0 (ASP.NET Core Runtime, Windows x64) y volvé a ejecutar.'
    }
}
Write-Host 'OK'

Paso 'MySQL'
$mysql = Buscar-MySql
$claveRoot = Leer-Secreto 'Contraseña de root de MySQL'
$existe = Ejecutar-MySql $mysql $claveRoot "SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = '$Base';"
if ("$existe".Trim() -ne '1') { throw "No existe la base $Base en este MySQL." }
$claveApi = Clave-Azar
$claveMigraciones = Clave-Azar
Ejecutar-MySql $mysql $claveRoot @"
CREATE USER IF NOT EXISTS 'crv4_api'@'localhost' IDENTIFIED BY '$claveApi';
ALTER USER 'crv4_api'@'localhost' IDENTIFIED BY '$claveApi';
GRANT SELECT, INSERT, UPDATE, DELETE ON ``$Base``.* TO 'crv4_api'@'localhost';
CREATE USER IF NOT EXISTS 'crv4_migraciones'@'localhost' IDENTIFIED BY '$claveMigraciones';
ALTER USER 'crv4_migraciones'@'localhost' IDENTIFIED BY '$claveMigraciones';
GRANT ALL PRIVILEGES ON ``$Base``.* TO 'crv4_migraciones'@'localhost';
GRANT PROCESS ON *.* TO 'crv4_migraciones'@'localhost';
FLUSH PRIVILEGES;
"@ | Out-Null
Write-Host "Cuentas crv4_api y crv4_migraciones listas sobre $Base."

Paso 'Puerto'
$ocupado = Get-NetTCPConnection -LocalPort $Puerto -State Listen -ErrorAction SilentlyContinue |
    Where-Object { (Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue).ProcessName -ne 'Crv4.Api' }
if ($ocupado) {
    $nombre = (Get-Process -Id ($ocupado | Select-Object -First 1).OwningProcess).ProcessName
    throw "El puerto $Puerto lo está usando otro programa ($nombre), quizás Sistema03. Cerralo y volvé a ejecutar."
}
Write-Host "Puerto $Puerto libre."

Paso 'Servicio'
& "$PSScriptRoot\Instalar-Api.ps1" -Puerto $Puerto

Paso 'Certificado HTTPS'
$ips = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } |
    Select-Object -ExpandProperty IPAddress -Unique)
Write-Host "IP de esta PC: $($ips -join ', ')"
$clavePfx = Clave-Azar
& "$PSScriptRoot\Crear-Certificado.ps1" -Direcciones $ips -ClavePfx $clavePfx

Paso 'Claves de la API'
& "$PSScriptRoot\Configurar-Secretos.ps1" -Puerto $Puerto -ClaveDb $claveApi -ClavePfx $clavePfx

Paso 'Respaldo y migraciones'
& "$PSScriptRoot\Aplicar-Migraciones.ps1" -Base $Base -Clave $claveMigraciones -Si

Paso 'Usuario administrador'
Write-Host 'Si ya entrás a Sistema03 con un usuario del rol Administrador, ese usuario sirve también en la 7.0.'
if ((Read-Host '¿Crear un usuario administrador nuevo? (s/N)') -eq 's') {
    & "$PSScriptRoot\Crear-Administrador.ps1" -Base $Base -Clave $claveApi
}

Paso 'Prueba'
Start-Service $NombreServicio -ErrorAction SilentlyContinue
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$respuesta = $null
for ($i = 0; $i -lt 10 -and -not $respuesta; $i++) {
    Start-Sleep -Seconds 2
    try { $respuesta = Invoke-RestMethod "https://localhost:$Puerto/api/salud" -TimeoutSec 5 } catch { }
}
if ($respuesta.estado -eq 'ok') {
    Write-Host ''
    Write-Host 'Listo: la API responde.' -ForegroundColor Green
    foreach ($ip in $ips) { Write-Host "  https://${ip}:$Puerto/api/salud" }
    Write-Host 'Cerrá Chrome por completo y abrí CRV4.'
} else {
    Write-Warning 'La API no respondió. Revisá el Visor de eventos (Registro de Windows > Aplicación, origen CRV4Api) y mandá una captura.'
}
