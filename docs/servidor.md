# Instalación en la PC servidor

Todo se ejecuta en **PowerShell como administrador**, desde la carpeta publicada (la que arma `Publicar-Api.ps1`).

## Requisitos

- Windows 10/11 o Server, con el **ASP.NET Core Runtime 10** (Hosting Bundle) instalado.
- MySQL 8 en la misma PC (o accesible desde ella).
- IP fija en la red local (hoy `192.168.100.128`).

## Primera instalación

1. **En tu PC de desarrollo**: `deploy\servidor\Publicar-Api.ps1`. Corre las pruebas y deja todo en `publish\api`.
   Copiá esa carpeta al servidor.
2. **Base y cuentas de MySQL**: editá las dos contraseñas de `crear-base.sql` y ejecutalo con `mysql -u root -p < crear-base.sql`.
   Después borrá las contraseñas del archivo.
3. `.\Instalar-Api.ps1`: crea el servicio `CRV4Api` y abre el puerto 5443 para redes privadas.
4. `.\Crear-Certificado.ps1 -Direcciones 192.168.100.128`: crea la CA local y el certificado HTTPS.
5. `.\Configurar-Secretos.ps1`: pide la clave de `crv4_api`, la del PFX y el dominio de Hostinger.
6. `.\Aplicar-Migraciones.ps1`: crea las tablas.
7. `.\Crear-Administrador.ps1`: crea tu usuario.
8. `Start-Service CRV4Api` y probá `https://192.168.100.128:5443/api/salud` desde otra PC.

## En cada PC que use el sistema

Copiá `crv4-ca.cer` (de `C:\CRV4\certificados`) e `Instalar-CA.ps1` a la PC y ejecutá `.\Instalar-CA.ps1` como administrador.
Sin esto el navegador rechaza la conexión con la API y la web muestra "No hay conexión con el servidor".

## Actualizar la API

1. `Publicar-Api.ps1` en tu PC, copiar `publish\api` al servidor.
2. `.\Instalar-Api.ps1`: detiene el servicio, guarda la versión anterior en `C:\CRV4\api-anterior` y arranca la nueva.
3. Si la versión nueva trae migraciones, el servicio no arranca hasta ejecutar `.\Aplicar-Migraciones.ps1`.

Para volver atrás: detener el servicio, copiar `C:\CRV4\api-anterior` sobre `C:\CRV4\api` e iniciarlo.

## Dónde está cada cosa

| Ruta | Contenido |
|---|---|
| `C:\CRV4\api` | La API instalada |
| `C:\CRV4\config\crv4.secretos.json` | Claves (solo administradores y el servicio) |
| `C:\CRV4\certificados` | CA local y certificado del servidor |
| `C:\CRV4\respaldos` | Respaldos previos a cada migración |
| Visor de eventos > Aplicación, origen `CRV4Api` | Registro de errores |
