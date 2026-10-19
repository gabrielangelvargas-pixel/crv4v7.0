# Base de datos y migraciones

- Motor: MySQL 8 en las PCs, MariaDB en Hostinger. Juego de caracteres `utf8mb4`.
- Base: **`crv4_datos`, la misma que usa Sistema03**. Las dos versiones conviven mientras dure el cambio.
- La estructura que había antes de la 7.0 está en `database/crv4_datos-estructura.sql` (volcado sin datos).
  Las pruebas automáticas arman la base de prueba con ese archivo y después aplican las migraciones, igual que en el servidor.
- Las fechas se guardan en hora local del servidor, como los datos existentes.
- Migraciones: archivos `.sql` en `api/migraciones`, con nombre `AAAAMMDDhhmmss_Nombre.sql`.
- Historial de migraciones aplicadas: tabla `crv4_migraciones` (la misma que usaba la versión .NET, así que lo ya aplicado no se repite).

## Regla para convivir con Sistema03

Una migración de la 7.0 **solo agrega** (tablas nuevas, columnas nuevas con valor por defecto, filas que falten).
No renombra, no borra y no cambia tipos de columnas que use Sistema03. Si algún día hace falta, va en una
migración aparte, revisada antes de aplicarla.

## Qué agrega la migración `Crv4v7Inicial`

| Cambio | Para qué |
|---|---|
| Tabla `sesiones` | Una fila por token de renovación (solo el hash). Permite cerrar sesiones de forma real. |
| Tabla `numeradores` (`ventas`, `clientes`) | Números de venta y de cliente generados en la base sin repetirse. Toma el máximo existente, así que no choca con lo que cargue Sistema03. |
| Columnas `IntentosFallidos` y `BloqueadoHasta` en `usuarios_sistema` | Bloqueo tras 5 intentos fallidos. Tienen valor por defecto: Sistema03 no las ve. |
| Permisos del catálogo que falten en `permisos` | Los que ya existen quedan como están. |
| Rol `Administrador` si no existe, con todos los permisos | El rol Administrador siempre tiene todo y no se edita desde la 7.0. |

## Tablas existentes que usa la 7.0 hoy

| Tabla | Para qué |
|---|---|
| `usuarios_sistema`, `usuarios_roles` | Usuarios (un usuario puede tener varios roles). Las contraseñas de Sistema03 siguen sirviendo. |
| `roles`, `permisos`, `roles_permisos` | Roles y permisos. Los permisos de un usuario son la suma de los de sus roles activos. |
| `rubros` | Rubros y subrubros (`IdPadre`). Los productos apuntan al código (`IdRubro`). |
| `productos`, `clientes` | Por ahora solo se cuentan en el Inicio. |

## Aplicar migraciones

```powershell
cd api
npm run migrar -- --ver   # solo muestra las pendientes
npm run migrar            # las aplica
```

Antes de aplicar una migración nueva en la base de Hostinger, conviene bajar un respaldo desde phpMyAdmin.

## Si cambia la estructura desde Sistema03

Volvé a generar el volcado sin datos y reemplazá `database/crv4_datos-estructura.sql`:

```powershell
mysqldump -u root -p --no-data --skip-add-drop-table crv4_datos > database\crv4_datos-estructura.sql
```

Después corré las pruebas: si algo de la 7.0 dejó de coincidir con la base, falla ahí y no en el servidor.
