# Funciones compartidas por los scripts del servidor. No se ejecuta solo.

$Script:NombreServicio = 'CRV4Api'
$Script:CarpetaBase = 'C:\CRV4'
$Script:CarpetaApi = Join-Path $CarpetaBase 'api'
$Script:CarpetaCertificados = Join-Path $CarpetaBase 'certificados'
$Script:CarpetaRespaldos = Join-Path $CarpetaBase 'respaldos'
$Script:CarpetaConfig = Join-Path $CarpetaBase 'config'
# La API lee este archivo al arrancar (ver Program.cs). Nunca se copia al repositorio.
$Script:ArchivoSecretos = Join-Path $CarpetaConfig 'crv4.secretos.json'

function Confirmar-Administrador {
    $actual = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    if (-not $actual.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Abrí PowerShell como administrador y volvé a ejecutar el script.'
    }
}

function Leer-Secreto([string]$Texto) {
    $seguro = Read-Host -Prompt $Texto -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

function Leer-Valor([string]$Texto, [string]$PorDefecto) {
    $valor = Read-Host -Prompt "$Texto [$PorDefecto]"
    if ([string]::IsNullOrWhiteSpace($valor)) { return $PorDefecto }
    return $valor.Trim()
}

function Armar-CadenaMySql([string]$Servidor, [int]$Puerto, [string]$Base, [string]$Usuario, [string]$Clave) {
    # Las comillas protegen claves con ; o =
    $claveEscapada = $Clave.Replace('"', '""')
    return "server=$Servidor;port=$Puerto;database=$Base;user=$Usuario;password=""$claveEscapada"";SslMode=Preferred"
}

function Buscar-MySqlDump {
    $comando = Get-Command mysqldump.exe -ErrorAction SilentlyContinue
    if ($comando) { return $comando.Source }
    $candidato = Get-ChildItem 'C:\Program Files\MySQL' -Recurse -Filter mysqldump.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($candidato) { return $candidato.FullName }
    return $null
}
