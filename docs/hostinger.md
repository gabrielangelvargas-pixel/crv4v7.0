# Publicación de la web en Hostinger

Cada vez que se sube un cambio de `web/` a la rama `main`, GitHub Actions (`.github/workflows/web-hostinger.yml`)
compila la web y deja el resultado en la rama **`hostinger`**. Hostinger copia esa rama a la carpeta del sitio.

## Configuración (una sola vez)

1. Crear el repositorio en GitHub (privado) y subir este proyecto a `main`.
2. En hPanel: **Sitios web > (el dominio o subdominio) > Avanzado > Git**.
   - Repositorio: `git@github.com:<usuario>/<repositorio>.git`
   - Rama: `hostinger`
   - Directorio: vacío (para `public_html`) o la subcarpeta del subdominio.
   - Si el repositorio es privado, copiar la **clave SSH** que muestra hPanel y agregarla en GitHub como *Deploy key* (solo lectura).
3. En hPanel activar **Implementación automática** y copiar la URL del webhook.
   En GitHub: **Settings > Webhooks > Add webhook**, pegar la URL, tipo `application/json`, evento *push*.
4. Activar SSL para el dominio en hPanel.
5. Poner ese dominio en el servidor con `Configurar-Secretos.ps1` (CORS).

## Cambiar la dirección de la API

Editar `web/public/config.json` y subir el cambio. No hace falta recompilar nada a mano.

## Cómo llega la versión nueva a cada PC

La web es una PWA con actualización automática: al abrirla busca la versión nueva, la instala y se recarga.
`.htaccess` evita que `index.html`, `sw.js` y `config.json` queden en caché.
