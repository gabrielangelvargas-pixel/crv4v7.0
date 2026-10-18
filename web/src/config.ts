export interface Configuracion {
  /** Dirección de la API. Hoy la PC servidor de la red local; mañana un servidor en la nube. */
  apiUrl: string
}

let configuracion: Configuracion | null = null

/**
 * Se lee de /config.json en tiempo de ejecución (no se compila dentro de la app),
 * así se puede cambiar la dirección de la API sin volver a publicar la web.
 */
export async function cargarConfiguracion(): Promise<Configuracion> {
  // En desarrollo se puede apuntar a otra API con VITE_API_URL en web/.env.development.local
  const local = import.meta.env.DEV ? (import.meta.env.VITE_API_URL as string | undefined) : undefined
  if (local) return (configuracion = { apiUrl: local.replace(/\/+$/, '') })

  const respuesta = await fetch('/config.json', { cache: 'no-store' })
  if (!respuesta.ok) throw new Error('No se pudo leer config.json')
  const datos = (await respuesta.json()) as Partial<Configuracion>
  if (!datos.apiUrl) throw new Error('config.json no tiene apiUrl')
  configuracion = { apiUrl: datos.apiUrl.replace(/\/+$/, '') }
  return configuracion
}

export function obtenerConfiguracion(): Configuracion {
  if (!configuracion) throw new Error('La configuración todavía no se cargó')
  return configuracion
}
