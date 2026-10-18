import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'
import paquete from './package.json' with { type: 'json' }

export default defineConfig({
  define: { __VERSION__: JSON.stringify(paquete.version) },
  plugins: [
    react(),
    VitePWA({
      // La versión nueva se instala sola: al abrir la web el service worker detecta el cambio,
      // se activa y recarga la página. No hace falta que cada PC haga nada.
      registerType: 'autoUpdate',
      injectRegister: false,
      includeAssets: ['favicon.svg', 'iconos/apple-touch-icon.png'],
      manifest: {
        name: 'CRV4 Mayorista',
        short_name: 'CRV4',
        description: 'Gestión interna CRV4 Mayorista',
        lang: 'es',
        start_url: '/',
        display: 'standalone',
        background_color: '#f3f5f5',
        theme_color: '#174b57',
        icons: [
          { src: '/iconos/icono-192.png', sizes: '192x192', type: 'image/png' },
          { src: '/iconos/icono-512.png', sizes: '512x512', type: 'image/png' },
          { src: '/iconos/icono-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,svg,png,woff2}'],
        navigateFallback: '/index.html',
        cleanupOutdatedCaches: true,
        // config.json siempre se pide a la red (si no hay red, se usa la última copia).
        runtimeCaching: [
          {
            urlPattern: ({ url }) => url.pathname === '/config.json',
            handler: 'NetworkFirst',
            options: { cacheName: 'config', networkTimeoutSeconds: 3 },
          },
        ],
      },
    }),
  ],
  server: { port: 5173 },
  build: { chunkSizeWarningLimit: 800 },
})
