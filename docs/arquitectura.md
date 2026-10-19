# Arquitectura y decisiones

## Vista general

- **API** (`api/`): Node.js 22.18 o más nuevo, TypeScript, Express 5, `mysql2` y `zod` para validar lo que llega.
  Node corre los `.ts` directamente (sin paso de compilación); `tsc` solo revisa los tipos.
  Las rutas se organizan por módulo en `src/modulos/<modulo>` (desde la etapa 2).
- **Web** (`web/`): React + Vite + TanStack Query + React Router. Cada pantalla grande se descarga recién al abrirla.
  Se instala como PWA.
- **Base**: `crv4_datos`, la misma de Sistema03 (conviven). En Hostinger es MariaDB. Las migraciones de la 7.0 solo agregan;
  ver `base-de-datos.md`.

## Por qué Node.js en Hostinger

La versión .NET corría en una PC de la red local con un certificado propio. Desde otros celulares, o con datos móviles,
no se podía llegar a la API ni confiar en ese certificado. Hostinger permite apps Node.js con HTTPS real, en el mismo
lugar que la base remota, así que la web y la API quedan en internet sin depender de la PC del negocio.

Se mantienen las mismas direcciones y el mismo JSON que la versión .NET, así la web casi no cambia. Las contraseñas
existentes (formato de ASP.NET Identity, PBKDF2) se siguen verificando igual.

## Lo que se corrigió respecto de Sistema03

| Problema en Sistema03 | Cómo se resuelve en 7.0 |
|---|---|
| Claves de MySQL y JWT en archivos del proyecto | Todo sale de variables de entorno `CRV4_*`: en Hostinger, en el panel de la app; en tu PC, en `api/.env.local`. |
| Tokens de 100 años | Token de acceso de 15 minutos + token de renovación con vencimiento absoluto de 12 horas, guardado como hash en `sesiones`. |
| "Debe cambiar contraseña" sin forma de cambiarla | `POST /api/auth/cambiar-password` y pantalla propia. |
| Login sin límite de intentos | Límite por IP (10 por minuto) y bloqueo del usuario por 15 minutos tras 5 intentos fallidos. |
| La API cambiaba la estructura de la base al arrancar | Migraciones `.sql` numeradas, aplicadas a mano con `npm run migrar`. `/api/salud` avisa si falta alguna. |
| Números de venta/cliente con `MAX(id)+1` | Tabla `numeradores`, atómica aunque carguen varias PCs a la vez. |
| `App.tsx` de 6.000 líneas | Una carpeta por módulo en la web y en la API. |
| Permisos repetidos como texto | Un único catálogo de permisos. |

## Seguridad: cómo funciona una sesión

1. `POST /api/auth/login` devuelve `tokenAcceso` (15 min) y `tokenRenovacion` (opaco, 256 bits).
2. La web guarda el de acceso solo en memoria y el de renovación en el navegador.
3. Cuando la API responde 401, la web llama una sola vez a `POST /api/auth/renovar`, que **rota** el token
   (el viejo queda revocado) sin estirar el vencimiento de 12 horas.
4. `POST /api/auth/salir` revoca el token de renovación.

Un usuario puede tener varios roles: sus permisos son la suma de los de sus roles activos. El rol `Administrador` tiene
siempre todos. Los códigos son los mismos que ya usa Sistema03 (`clientes.ver`, `usuarios.administrar`, `reportes.ver`...).

## CORS

La API solo acepta llamadas del navegador desde los dominios de `CRV4_CORS_ORIGENES` (por ejemplo
`https://desarrollos-gv.com.ar,https://www.desarrollos-gv.com.ar`).

## Actualización de la web en cada equipo

La PWA usa `registerType: 'autoUpdate'`. Al abrir la web (o al volver a la pestaña, o cada 30 minutos) el service
worker busca una versión nueva, la instala y recarga. `index.html`, `sw.js` y `config.json` se sirven sin caché
(`.htaccess`).
