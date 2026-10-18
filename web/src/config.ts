export interface Servidor {
  nombre: string
  /** Dirección de la API, por ejemplo https://192.168.100.128:5443 */
  apiUrl: string
}

export interface Configuracion {
  servidores: Servidor[]
  /** El servidor en uso. */
  apiUrl: string
  nombreServidor: string
}

const CLAVE_SERVIDOR = 'crv4.servidor'
const ESPERA_PRUEBA_MS = 2500

let configuracion: Configuracion | null = null

/**
 * Lee /config.json en tiempo de ejecución (no se compila dentro de la app), así se pueden cambiar
 * los servidores sin volver a publicar la web. Puede tener varios (negocio, casa); se usa el último
 * elegido si responde y, si no, el primero de la lista que responda.
 */
export async function cargarConfiguracion(): Promise<Configuracion> {
  const servidores = await leerServidores()
  const elegido = leer(CLAVE_SERVIDOR)
  const ordenados = [...servidores].sort((a, b) => Number(b.apiUrl === elegido) - Number(a.apiUrl === elegido))

  let activo = ordenados[0]
  if (servidores.length > 1) {
    // Se prueban todos a la vez y se toma el primero de la lista que responda, sin esperar a los demás.
    const pruebas = ordenados.map(responde)
    for (let i = 0; i < ordenados.length; i++) {
      if (await pruebas[i]) {
        activo = ordenados[i]
        break
      }
    }
  }
  configuracion = { servidores, apiUrl: activo.apiUrl, nombreServidor: activo.nombre }
  return configuracion
}

export function obtenerConfiguracion(): Configuracion {
  if (!configuracion) throw new Error('La configuración todavía no se cargó')
  return configuracion
}

/** Cambia de servidor a mano. La sesión es de cada servidor, así que se recarga la web. */
export function elegirServidor(apiUrl: string) {
  try {
    localStorage.setItem(CLAVE_SERVIDOR, apiUrl)
  } catch {
    /* sin almacenamiento: vale solo hasta recargar */
  }
}

async function leerServidores(): Promise<Servidor[]> {
  // En desarrollo se puede apuntar a otra API con VITE_API_URL en web/.env.development.local
  const local = import.meta.env.DEV ? (import.meta.env.VITE_API_URL as string | undefined) : undefined
  if (local) return [{ nombre: 'Desarrollo', apiUrl: limpiar(local) }]

  const respuesta = await fetch('/config.json', { cache: 'no-store' })
  if (!respuesta.ok) throw new Error('No se pudo leer config.json')
  const datos = (await respuesta.json()) as { servidores?: Servidor[]; apiUrl?: string }
  const servidores = (datos.servidores ?? (datos.apiUrl ? [{ nombre: 'Servidor', apiUrl: datos.apiUrl }] : []))
    .filter((s) => s.apiUrl)
    .map((s) => ({ nombre: s.nombre || s.apiUrl, apiUrl: limpiar(s.apiUrl) }))
  if (servidores.length === 0) throw new Error('config.json no tiene servidores')
  return servidores
}

async function responde(servidor: Servidor): Promise<boolean> {
  try {
    const r = await fetch(`${servidor.apiUrl}/api/salud`, { signal: AbortSignal.timeout(ESPERA_PRUEBA_MS) })
    return r.ok
  } catch {
    return false
  }
}

function leer(clave: string) {
  try {
    return localStorage.getItem(clave)
  } catch {
    return null
  }
}

const limpiar = (url: string) => url.trim().replace(/\/+$/, '')
