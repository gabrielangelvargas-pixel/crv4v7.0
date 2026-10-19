# Desarrollo

## Requisitos

- Node.js 22.18 o más nuevo (o 24)
- MySQL 8 o MariaDB local

## Base de datos local

Para desarrollar conviene una **copia** de `crv4_datos` (por ejemplo `crv4_datos_dev`), así probás las migraciones
sin tocar la base que usa Sistema03.

```sql
CREATE DATABASE crv4_datos_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
-- restaurá ahí un respaldo de crv4_datos: mysql -u root -p crv4_datos_dev < respaldo.sql
-- Usuario de desarrollo (elegí tu propia contraseña)
CREATE USER 'crv4_dev'@'localhost' IDENTIFIED BY '...';
GRANT ALL ON crv4_datos_dev.* TO 'crv4_dev'@'localhost';
GRANT ALL ON crv4_test.* TO 'crv4_dev'@'localhost';
```

## API

Creá `api/.env.local` (está en `.gitignore`, nunca se sube) con tus datos:

```
CRV4_DB_SERVIDOR=localhost
CRV4_DB_BASE=crv4_datos_dev
CRV4_DB_USUARIO=crv4_dev
CRV4_DB_CLAVE=...
CRV4_CORS_ORIGENES=http://localhost:5173
```

```powershell
cd api
npm install
npm run migrar     # muestra y aplica las migraciones pendientes (con -- --ver solo las muestra)
npm run dev        # API en http://localhost:3000, se reinicia sola al guardar
```

Probá `http://localhost:3000/api/salud`.

### Pruebas

```powershell
$env:CRV4_TEST_MYSQL = "mysql://crv4_dev:CLAVE@localhost:3306/crv4_test"
npm test
npm run typecheck
npm run lint
```

La base de pruebas se borra y se arma de nuevo en cada corrida con `database/crv4_datos-estructura.sql`, por eso
su nombre tiene que terminar en `_test`.

### Agregar una tabla o columna

Crear un archivo en `api/migraciones` con la fecha y un nombre, por ejemplo `20261101120000_ClientesNotas.sql`.
Solo sentencias que agregan (ver `base-de-datos.md`), separadas por `;` al final de la línea.

## Web

```powershell
cd web
npm install
npm run dev
```

Para usar tu API local creá `web/.env.development.local` (no se sube a Git) con:

```
VITE_API_URL=http://localhost:3000
```
