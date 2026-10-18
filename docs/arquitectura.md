# Arquitectura y decisiones

## Vista general

- **API** (`api/src/Crv4.Api`): ASP.NET Core con controladores, organizada por módulo en `Modulos/`.
  Datos con EF Core (`MySql.EntityFrameworkCore`), mapeado a las tablas existentes de `crv4_datos`.
- **Web** (`web/`): React + Vite + TanStack Query + React Router. Cada pantalla grande se descarga
  recién al abrirla. Se instala como PWA.
- **Base**: MySQL 8, la misma `crv4_datos` de Sistema03 (conviven), con dos cuentas nuevas: `crv4_api` (solo datos)
  y `crv4_migraciones` (estructura). Las migraciones de la 7.0 solo agregan; ver `base-de-datos.md`.

## Lo que se corrigió respecto de Sistema03

| Problema en Sistema03 | Cómo se resuelve en 7.0 |
|---|---|
| Claves de MySQL, JWT y certificado en archivos del proyecto | `appsettings.json` no tiene ninguna clave. En el servidor están en `C:\CRV4\config\crv4.secretos.json`, que crea `Configurar-Secretos.ps1` y solo lee el servicio. En desarrollo, `dotnet user-secrets`. |
| Tokens de 100 años | Token de acceso de 15 minutos + token de renovación con vencimiento absoluto de 12 horas, guardado como hash en `sesiones`. Se revoca al salir, al cambiar la contraseña, al cambiar el rol o desactivar al usuario. Si un token ya usado vuelve a aparecer, se cierran todas las sesiones del usuario. |
| "Debe cambiar contraseña" sin forma de cambiarla | `POST /api/auth/cambiar-password` y pantalla propia. Mientras esté pendiente, la API rechaza todo lo demás. |
| Login sin límite de intentos | Límite por IP (10 por minuto) y bloqueo del usuario por 15 minutos tras 5 intentos fallidos. |
| La API cambiaba la estructura de la base al arrancar | Migraciones de EF Core. Si hay migraciones pendientes, la API no arranca y lo informa. Se aplican con `Crv4.Api.exe migrar` (vía `Aplicar-Migraciones.ps1`, que antes respalda la base). |
| Números de venta/cliente con `MAX(id)+1` | Tabla `numeradores` con `UPDATE ... LAST_INSERT_ID(ultimo_numero + 1)`: atómico aunque carguen varias PCs a la vez (hay una prueba con 40 pedidos simultáneos). |
| `App.tsx` de 6.000 líneas | Una carpeta por módulo, componentes de UI comunes en `src/ui`, cliente de API en `src/api`. |
| La API ejecutaba `git push` como SYSTEM | La API no ejecuta procesos externos. Corre con una cuenta virtual sin privilegios (`NT SERVICE\CRV4Api`). |
| Permisos repetidos como texto | Un único catálogo (`Permisos.cs`). Una prueba verifica que no haya repetidos. |

## Seguridad: cómo funciona una sesión

1. `POST /api/auth/login` devuelve `tokenAcceso` (15 min) y `tokenRenovacion` (opaco, 256 bits).
2. La web guarda el de acceso solo en memoria y el de renovación en el navegador.
3. Cuando la API responde 401, la web llama una sola vez a `POST /api/auth/renovar`, que **rota** el token
   (el viejo queda revocado) sin estirar el vencimiento de 12 horas.
4. `POST /api/auth/salir` revoca el token de renovación.

Los permisos viajan dentro del token de acceso, así que un cambio de permisos de un rol llega a sus usuarios en la
próxima renovación (máximo 15 minutos). Cambiar los roles o desactivar a un usuario cierra sus sesiones en el momento.
Un usuario puede tener varios roles: sus permisos son la suma de los de sus roles activos. El rol `Administrador` tiene
siempre todos. Los códigos son los mismos que ya usa Sistema03 (`clientes.ver`, `usuarios.administrar`, `reportes.ver`...).

Todo endpoint exige login salvo los marcados `[AllowAnonymous]` (login, renovar, salir y salud). Los permisos se piden con
`[TienePermiso(Permisos.X)]`.

## Web en Hostinger llamando a la API local

La web se abre desde internet (HTTPS) y llama a la API en la red local. Para que el navegador lo permita:

- La API usa **HTTPS** con un certificado firmado por una CA propia (`Crear-Certificado.ps1`), y cada PC confía en esa CA (`Instalar-CA.ps1`).
- **CORS** permite solo el dominio de Hostinger configurado.
- La API responde `Access-Control-Allow-Private-Network: true` en las consultas previas, que Chrome y Edge exigen
  cuando una página pública llama a una IP privada. Chrome puede además pedir permiso de "acceso a la red local"
  la primera vez: hay que aceptarlo.
- Las direcciones de la API están en `web/public/config.json` (no se compilan). Puede haber varias, por ejemplo el
  servidor del negocio y la PC de casa: la web prueba todas al abrir y usa la primera que responde (o la que se eligió
  en el login, si responde). Pasar a un servidor en la nube es agregarlo a ese archivo.

## Actualización de la web en cada PC

La PWA usa `registerType: 'autoUpdate'`. Al abrir la web (o al volver a la pestaña, o cada 30 minutos) el service
worker busca una versión nueva, la instala y recarga. `index.html`, `sw.js` y `config.json` se sirven sin caché
(`.htaccess`), así que el cambio llega en la siguiente apertura.

## Pasar la base a la nube (más adelante)

1. Restaurar un respaldo de `crv4_datos` en el servidor en la nube y aplicar las migraciones que falten.
2. Instalar la API en ese servidor (Linux o Windows) con un certificado real (por ejemplo Let's Encrypt).
3. Cambiar `apiUrl` en `web/public/config.json`. No hace falta tocar el código.
