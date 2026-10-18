# Publicación de la web en Hostinger

Hostinger toma el repositorio de GitHub y **compila la web él mismo** cada vez que se sube un cambio a `main`.

## Configuración en hPanel

| Opción | Valor |
|---|---|
| Repositorio | `gabrielangelvargas-pixel/crv4v7.0` |
| Preset | Vite |
| Rama | `main` |
| Node | 22.x |
| Directorio root | `web` |
| Compilación y salida | Predeterminado para Vite (`npm run build`, carpeta `dist`) |
| Dominio actual | `https://white-eland-918813.hostingersite.com` |

Si cambia el dominio (por ejemplo a uno propio), hay que actualizarlo en `Cors:OrigenesPermitidos` de
`api/src/Crv4.Api/appsettings.json` o en el servidor con `Configurar-Secretos.ps1`.

## Cambiar la dirección de la API

Editar `web/public/config.json` y subir el cambio a `main`.

## Cómo llega la versión nueva a cada PC

La web es una PWA con actualización automática: al abrirla busca la versión nueva, la instala y se recarga.
`public/.htaccess` evita que `index.html`, `sw.js` y `config.json` queden en caché y manda todas las rutas a `index.html`.
