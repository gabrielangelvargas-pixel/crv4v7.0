<#
.SYNOPSIS
  Crea el certificado HTTPS de la API para la red local.

.DESCRIPTION
  1. Crea (una sola vez) una autoridad certificante local "CRV4 Mayorista CA".
  2. Con ella firma el certificado del servidor, válido para su IP y su nombre de equipo.
  3. Exporta:
       C:\CRV4\certificados\crv4-api.pfx   certificado del servidor (con contraseña), lo usa la API
       C:\CRV4\certificados\crv4-ca.cer    certificado público de la CA, se instala en cada PC con Instalar-CA.ps1

  La web en Hostinger usa HTTPS, así que el navegador solo acepta llamar a una API también HTTPS
  y con un certificado de confianza. Por eso cada PC tiene que confiar en esta CA.

.EXAMPLE
  .\Crear-Certificado.ps1 -Direcciones 192.168.100.128
#>
param(
    [string[]]$Direcciones = @('192.168.100.128'),
    [string[]]$Nombres = @($env:COMPUTERNAME, 'localhost'),
    [int]$AniosServidor = 2
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Comun.ps1"
Confirmar-Administrador

New-Item -ItemType Directory -Force -Path $CarpetaCertificados | Out-Null
$sujetoCa = 'CN=CRV4 Mayorista CA'

$ca = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -eq $sujetoCa -and $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date) } | Select-Object -First 1
if (-not $ca) {
    Write-Host 'Creando la autoridad certificante local...'
    $ca = New-SelfSignedCertificate -Subject $sujetoCa -FriendlyName 'CRV4 Mayorista CA' `
        -KeyUsage CertSign, CRLSign, DigitalSignature -KeyUsageProperty Sign -KeyLength 4096 -HashAlgorithm SHA256 `
        -KeyExportPolicy NonExportable -CertStoreLocation Cert:\LocalMachine\My -NotAfter (Get-Date).AddYears(10) `
        -TextExtension @('2.5.29.19={critical}{text}ca=true&pathlength=0')
} else {
    Write-Host "Usando la CA existente (vence $($ca.NotAfter.ToShortDateString()))."
}

$rutaCa = Join-Path $CarpetaCertificados 'crv4-ca.cer'
Export-Certificate -Cert $ca -FilePath $rutaCa -Force | Out-Null
Import-Certificate -FilePath $rutaCa -CertStoreLocation Cert:\LocalMachine\Root | Out-Null

$san = @($Direcciones | ForEach-Object { "IPAddress=$_" }) + @($Nombres | ForEach-Object { "DNS=$_" })
Write-Host "Creando el certificado del servidor para: $($san -join ', ')"
$servidor = New-SelfSignedCertificate -Subject 'CN=CRV4 API' -FriendlyName 'CRV4 API' -Signer $ca `
    -KeyUsage DigitalSignature, KeyEncipherment -KeyLength 2048 -HashAlgorithm SHA256 -KeyExportPolicy Exportable `
    -CertStoreLocation Cert:\LocalMachine\My -NotAfter (Get-Date).AddYears($AniosServidor) `
    -TextExtension @("2.5.29.17={text}$($san -join '&')", '2.5.29.37={text}1.3.6.1.5.5.7.3.1')

$clave = Leer-Secreto 'Contraseña para el archivo PFX (guardala: la pide Configurar-Secretos.ps1)'
$rutaPfx = Join-Path $CarpetaCertificados 'crv4-api.pfx'
Export-PfxCertificate -Cert $servidor -FilePath $rutaPfx -Password (ConvertTo-SecureString $clave -AsPlainText -Force) -Force | Out-Null
Remove-Item "Cert:\LocalMachine\My\$($servidor.Thumbprint)"

# Solo administradores y SYSTEM pueden leer la carpeta (SIDs, para que funcione con Windows en cualquier idioma).
# Configurar-Secretos.ps1 le da lectura al servicio sobre el PFX.
icacls $CarpetaCertificados /inheritance:r /grant:r '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-18:(OI)(CI)F' | Out-Null

Write-Host ''
Write-Host "Listo. Certificado del servidor: $rutaPfx (vence $((Get-Date).AddYears($AniosServidor).ToShortDateString()))"
Write-Host "Copiá $rutaCa a cada PC y ejecutá ahí Instalar-CA.ps1."
