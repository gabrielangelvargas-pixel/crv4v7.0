import { registerSW } from 'virtual:pwa-register'

/**
 * Registra el service worker. Con registerType 'autoUpdate' la versión nueva se activa
 * y la página se recarga sola. Además se busca una versión nueva al volver a la pestaña
 * y cada 30 minutos, para las PCs que dejan la web abierta todo el día.
 */
export function registrarPwa() {
  if (!('serviceWorker' in navigator)) return
  registerSW({
    immediate: true,
    onRegisteredSW(_url, registro) {
      if (!registro) return
      const buscar = () => registro.update().catch(() => undefined)
      setInterval(buscar, 30 * 60 * 1000)
      document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') buscar()
      })
    },
  })
}
