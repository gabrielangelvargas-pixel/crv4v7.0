# Desarrollo

## Requisitos

- .NET SDK 10
- Node.js 22 o superior
- MySQL 8 (local)

## Base de datos local

```sql
CREATE DATABASE crv4_v7 CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE DATABASE crv4_test CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
-- Usuario de desarrollo (elegí tu propia contraseña)
CREATE USER 'crv4_dev'@'localhost' IDENTIFIED BY '...';
GRANT ALL ON crv4_v7.* TO 'crv4_dev'@'localhost';
GRANT ALL ON crv4_test.* TO 'crv4_dev'@'localhost';
```

## API

Las claves de desarrollo van en *user-secrets* (se guardan en tu perfil de Windows, fuera del proyecto):

```powershell
cd api\src\Crv4.Api
dotnet user-secrets set "ConnectionStrings:Principal" "server=localhost;database=crv4_v7;user=crv4_dev;password=..."
dotnet user-secrets set "Jwt:Clave" "una-clave-larga-de-al-menos-32-caracteres-solo-para-desarrollo"

dotnet tool restore            # instala dotnet-ef (está en api\dotnet-tools.json)
dotnet run -- migrar           # crea las tablas
dotnet run -- crear-admin      # crea tu usuario
dotnet run --launch-profile https
```

La API queda en `https://localhost:5443` (`/api/salud` para probar).

### Pruebas

```powershell
$env:CRV4_TEST_MYSQL = "server=localhost;database=crv4_test;user=crv4_dev;password=..."
dotnet test api\Crv4.sln
```

Sin `CRV4_TEST_MYSQL` se corren solo las pruebas que no usan base. La base de pruebas se borra y se recrea en cada
corrida, por eso su nombre tiene que terminar en `_test`.

### Agregar una tabla o columna

```powershell
cd api
dotnet ef migrations add NombreDelCambio -p src\Crv4.Api -o Datos\Migraciones
dotnet ef migrations script -p src\Crv4.Api --idempotent -o publish\migracion.sql   # para revisar el SQL
```

## Web

```powershell
cd web
npm install
npm run dev
```

La web lee la dirección de la API de `web/public/config.json` (en el repositorio está la del servidor).
Para usar tu API local creá `web/.env.development.local` (no se sube a Git) con:

```
VITE_API_URL=https://localhost:5443
```

## Agregar un módulo nuevo

1. API: carpeta `Modulos/<Modulo>` con entidad, configuración (en `Datos/Configuraciones`), DTOs y controlador.
2. Permisos: constantes en `Permisos.cs` y `web/src/auth/permisos.ts`.
3. Migración: `dotnet ef migrations add <Modulo>`.
4. Web: carpeta `src/modulos/<modulo>` con su página, ruta en `App.tsx` y opción en `layout/menu.ts`.
5. Pruebas de integración en `api/tests/Crv4.Api.Tests/Integracion`.
