# Base de datos y migraciones

- Motor: MySQL 8, juego de caracteres `utf8mb4`.
- Base: `crv4_v7` (nueva). Sistema03 sigue usando `crv4_datos` hasta el cambio.
- Nombres de tablas y columnas en `snake_case`. Fechas en UTC, `datetime(6)`.
- Historial de migraciones aplicadas: tabla `__migraciones`.

## Cuentas

| Cuenta | Permisos | La usa |
|---|---|---|
| `crv4_api` | SELECT, INSERT, UPDATE, DELETE | El servicio de la API |
| `crv4_migraciones` | Todo sobre `crv4_v7` + PROCESS | `Aplicar-Migraciones.ps1` (pide la clave cada vez) |

Se crean con `deploy/servidor/crear-base.sql`.

## Tablas actuales

| Tabla | Para qué |
|---|---|
| `usuarios` | Usuarios del sistema. Contraseña con hash PBKDF2, bloqueo por intentos fallidos. |
| `roles`, `permisos`, `roles_permisos` | Roles y sus permisos. El rol 1 (Administrador) tiene todos y no se edita. |
| `sesiones` | Una fila por token de renovación (solo el hash). Permite cerrar sesiones de forma real. |
| `rubros` | Rubros de productos. |
| `numeradores` | Contadores de ventas y clientes, incrementados en forma atómica. |

## Aplicar migraciones en el servidor

```powershell
.\Aplicar-Migraciones.ps1
```

Detiene el servicio, respalda la base en `C:\CRV4\respaldos`, muestra las migraciones pendientes, pide confirmación,
las aplica y vuelve a iniciar el servicio. Si una migración falla, el respaldo está en esa carpeta.

## Importar los datos de Sistema03

Pendiente. Con el esquema de `crv4_datos` (volcado sin datos) se define el modelo de Productos, Clientes y Ventas
y se arma un script de importación de una sola vez de `crv4_datos` a `crv4_v7`, que se puede repetir para ensayar
antes del cambio definitivo.
