<#
.SYNOPSIS
  En la PC servidor: instala o actualiza la API como servicio de Windows.

.DESCRIPTION
  Se ejecuta desde la carpeta que generó Publicar-Api.ps1 (contiene Crv4.Api.exe).
  - Primera vez: crea el servicio "CRV4Api" con una cuenta virtual sin privilegios
    (NT SERVICE\CRV4Api), lo configura para reiniciarse si falla y abre el puerto en el firewall
    solo para redes privadas.
  - Actualización: detiene el servicio, guarda la versión anterior en C:\CRV4\api-anterior,
    copia la nueva y lo vuelve a iniciar.
  Nunca toca la base de datos: si la versión nueva trae migraciones, el servicio no arranca
  hasta que se ejecute Aplicar-Migraciones.ps1.
#>
param([int]$Puerto = 5443)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Comun.ps1"
Confirmar-Administrador

$origen = $PSScriptRoot
if (-not (Test-Path (Join-Path $origen 'Crv4.Api.exe'))) { throw 'Ejecutá este script desde la carpeta publicada (donde está Crv4.Api.exe).' }

$servicio = Get-Service $NombreServicio -ErrorAction SilentlyContinue
if ($servicio -and $servicio.Status -ne 'Stopped') {
    Write-Host 'Deteniendo el servicio...'
    Stop-Service $NombreServicio
    $servicio.WaitForStatus('Stopped', '00:00:30')
}

if (Test-Path $CarpetaApi) {
    $anterior = Join-Path $CarpetaBase 'api-anterior'
    if (Test-Path $anterior) { Remove-Item $anterior -Recurse -Force }
    Copy-Item $CarpetaApi $anterior -Recurse
    Remove-Item "$CarpetaApi\*" -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $CarpetaApi | Out-Null
Copy-Item "$origen\*" $CarpetaApi -Recurse -Force

if (-not $servicio) {
    Write-Host 'Creando el servicio...'
    New-Service -Name $NombreServicio -DisplayName 'CRV4 Mayorista API' -Description 'API de CRV4 Mayorista 7.0' `
        -BinaryPathName "`"$CarpetaApi\Crv4.Api.exe`"" -StartupType Automatic | Out-Null
    sc.exe config $NombreServicio obj= "NT SERVICE\$NombreServicio" | Out-Null
    sc.exe failure $NombreServicio reset= 86400 actions= restart/5000/restart/15000/restart/60000 | Out-Null
    if (-not [Diagnostics.EventLog]::SourceExists($NombreServicio)) { [Diagnostics.EventLog]::CreateEventSource($NombreServicio, 'Application') }
    New-NetFirewallRule -DisplayName 'CRV4 API (HTTPS)' -Direction Inbound -Protocol TCP -LocalPort $Puerto -Profile Private, Domain -Action Allow | Out-Null
}
icacls $CarpetaApi /grant "NT SERVICE\${NombreServicio}:(OI)(CI)RX" | Out-Null

if (-not (Test-Path $ArchivoSecretos)) {
    Write-Host ''
    Write-Host 'Falta configurar las claves. Ejecutá Configurar-Secretos.ps1 y después Aplicar-Migraciones.ps1.'
    return
}

Write-Host 'Iniciando el servicio...'
Start-Service $NombreServicio
Start-Sleep -Seconds 3
if ((Get-Service $NombreServicio).Status -ne 'Running') {
    Write-Warning 'El servicio no quedó iniciado. Revisá el Visor de eventos (Registro de Windows > Aplicación, origen CRV4Api). Si dice que hay migraciones pendientes, ejecutá Aplicar-Migraciones.ps1.'
} else {
    Write-Host "API iniciada en https://localhost:$Puerto/api/salud"
}
