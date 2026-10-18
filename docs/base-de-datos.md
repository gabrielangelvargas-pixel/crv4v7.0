# Base de datos y migraciones

- Motor: MySQL 8, juego de caracteres `utf8mb4`.
- Base: **`crv4_datos`, la misma que usa Sistema03**. Las dos versiones conviven mientras dure el cambio.
- La estructura que había antes de la 7.0 está en `database/crv4_datos-estructura.sql` (volcado sin datos).
  Las pruebas automáticas arman la base de prueba con ese archivo y después aplican las migraciones, igual que en el servidor.
- Las fechas se guardan en hora local del servidor, como los datos existentes.
- Historial de migraciones aplicadas: tabla `crv4_migraciones`.

## Regla para convivir con Sistema03

Las tablas existentes se mapean con `ExcludeFromMigrations`: EF las lee y escribe pero nunca las crea ni las cambia.
Una migración de la 7.0 **solo agrega** (tablas nuevas, columnas nuevas con valor por defecto, filas que falten).
No renombra, no borra y no cambia tipos de columnas que use Sistema03. Si algún día hace falta, va en una
migración aparte, escrita a mano y revisada antes de aplicarla.

## Cuentas

| Cuenta | Permisos | La usa |
|---|---|---|
| `crv4_api` | SELECT, INSERT, UPDATE, DELETE sobre `crv4_datos` | El servicio de la API |
| `crv4_migraciones` | Todo sobre `crv4_datos` + PROCESS | `Aplicar-Migraciones.ps1` (pide la clave cada vez) |

Se crean con `deploy/servidor/crear-cuentas.sql`. No tocan las cuentas que ya usa Sistema03.

## Qué agrega la migración `Crv4v7Inicial`

| Cambio | Para qué |
|---|---|
| Tabla `sesiones` | Una fila por token de renovación (solo el hash). Permite cerrar sesiones de forma real. |
| Tabla `numeradores` (`ventas`, `clientes`) | Números de venta y de cliente generados en la base sin repetirse. Toma el máximo existente, así que no choca con lo que cargue Sistema03. |
| Columnas `IntentosFallidos` y `BloqueadoHasta` en `usuarios_sistema` | Bloqueo tras 5 intentos fallidos. Tienen valor por defecto: Sistema03 no las ve. |
| Permisos del catálogo que falten en `permisos` | Los que ya existen quedan como están. |
| Rol `Administrador` si no existe, con todos los permisos | El rol Administrador siempre tiene todo y no se edita desde la 7.0. |

Para deshacerla: `Down` borra las dos tablas y las dos columnas. Los permisos y el rol se dejan.

## Tablas existentes que usa la 7.0 hoy

| Tabla | Para qué |
|---|---|
| `usuarios_sistema`, `usuarios_roles` | Usuarios (un usuario puede tener varios roles). Las contraseñas de Sistema03 siguen sirviendo. |
| `roles`, `permisos`, `roles_permisos` | Roles y permisos. Los permisos de un usuario son la suma de los de sus roles activos. |
| `rubros` | Rubros y subrubros (`IdPadre`). Los productos apuntan al código (`IdRubro`). |
| `productos`, `clientes` | Por ahora solo se cuentan en el Inicio. |

## Aplicar migraciones en el servidor

```powershell
.\Aplicar-Migraciones.ps1
```

Detiene el servicio, respalda la base en `C:\CRV4\respaldos`, muestra las migraciones pendientes, pide confirmación,
las aplica y vuelve a iniciar el servicio. Si una migración falla, el respaldo está en esa carpeta.

## Si cambia la estructura desde Sistema03

Volvé a generar el volcado sin datos y reemplazá `database/crv4_datos-estructura.sql`:

```powershell
mysqldump -u root -p --no-data --skip-add-drop-table crv4_datos > database\crv4_datos-estructura.sql
```

Después corré las pruebas: si algo de la 7.0 dejó de coincidir con la base, falla ahí y no en el servidor.
