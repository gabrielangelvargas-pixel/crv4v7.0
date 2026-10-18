<#
.SYNOPSIS
  En la PC de desarrollo: compila la API y deja el resultado en publish\api, listo para copiar al servidor.
#>
$ErrorActionPreference = 'Stop'
$raiz = Resolve-Path "$PSScriptRoot\..\.."
$salida = Join-Path $raiz 'publish\api'
if (Test-Path $salida) { Remove-Item $salida -Recurse -Force }

dotnet test "$raiz\api\Crv4.sln" -c Release
if ($LASTEXITCODE -ne 0) { throw 'Las pruebas fallaron. No se publica.' }

dotnet publish "$raiz\api\src\Crv4.Api\Crv4.Api.csproj" -c Release -r win-x64 --self-contained false -o $salida
if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación.' }

Copy-Item "$PSScriptRoot\*.ps1", "$PSScriptRoot\*.sql" $salida
Write-Host "Listo: $salida. Copiá esa carpeta al servidor y ejecutá ahí Instalar-Api.ps1."
