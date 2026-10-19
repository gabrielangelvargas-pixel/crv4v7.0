# Publicación en Hostinger

## Web (`desarrollos-gv.com.ar`)

Hostinger compila la web desde la rama `main`, carpeta `web`, cada vez que se sube un cambio.
La dirección de la API está en `web/public/config.json`. La PWA se actualiza sola en cada equipo.

## API (`api.desarrollos-gv.com.ar`)

App Node.js de Hostinger conectada al mismo repositorio de GitHub.

### Configuración (una sola vez)

1. En hPanel: **Sitios web > Agregar sitio web > Node.js** (o **Node.js Web App**), importando desde GitHub
   el repositorio `crv4v7.0`, rama `main`.
2. Dominio: el subdominio `api.desarrollos-gv.com.ar`.
3. Opciones de compilación:
   - Carpeta raíz (*root directory*): `api`
   - Versión de Node: **24** (o 22, si es 22.18 o más nueva)
   - Comando de instalación: `npm install` (lo que venga por defecto)
   - Comando de compilación: vacío
   - Archivo de inicio / comando de inicio: `src/servidor.ts` / `npm start`
4. Variables de entorno (en el panel de la app, nunca en el repositorio):

   | Variable | Valor |
   |---|---|
   | `CRV4_DB_SERVIDOR` | `localhost` (la base está en el mismo Hostinger) |
   | `CRV4_DB_BASE` | nombre de la base |
   | `CRV4_DB_USUARIO` | usuario de la base |
   | `CRV4_DB_CLAVE` | contraseña de ese usuario |
   | `CRV4_CORS_ORIGENES` | `https://desarrollos-gv.com.ar,https://www.desarrollos-gv.com.ar` |

   `PORT` lo pone Hostinger.
5. Verificar que el subdominio tenga SSL activo.
6. Abrir `https://api.desarrollos-gv.com.ar/api/salud`: tiene que decir `"estado":"ok"`.
   Si dice `sin-base-datos`, revisar las variables de la base.

### Aplicar migraciones

Desde tu PC, con `api/.env.local` apuntando a la base de Hostinger (servidor `srv1898.hstgr.io`, con tu IP habilitada
en **Bases de datos > MySQL remoto**):

```powershell
cd api
npm run migrar
```

### Cada cambio nuevo de la API

Se sube a `main`. Si Hostinger no la vuelve a publicar sola, en el panel de la app tocar **Volver a implementar**.
