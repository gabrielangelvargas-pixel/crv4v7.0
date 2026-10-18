<#
.SYNOPSIS
  Hace que esta PC confíe en el certificado HTTPS del servidor CRV4. Se ejecuta una vez en cada PC
  (como administrador), con el archivo crv4-ca.cer en la misma carpeta que este script.
  Chrome y Edge usan el almacén de Windows; Firefox necesita además
  security.enterprise_roots.enabled = true en about:config.
#>
param([string]$Archivo = (Join-Path $PSScriptRoot 'crv4-ca.cer'))
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Comun.ps1"
Confirmar-Administrador
if (-not (Test-Path $Archivo)) { throw "No encuentro $Archivo. Copialo desde C:\CRV4\certificados del servidor." }
Import-Certificate -FilePath $Archivo -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
Write-Host 'Certificado de CRV4 instalado. Cerrá y volvé a abrir el navegador.'
