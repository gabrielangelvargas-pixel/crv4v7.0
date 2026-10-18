import { obtenerConfiguracion } from '../config'
import type { SesionRespuesta } from './tipos'

/** Error con un mensaje listo para mostrar al usuario. */
export class ErrorApi extends Error {
  readonly estado: number
  readonly campos: Record<string, string[]>

  constructor(mensaje: string, estado: number, campos: Record<string, string[]> = {}) {
    super(mensaje)
    this.estado = estado
    this.campos = campos
  }
}

const CLAVE_RENOVACION = 'crv4.renovacion'

// El token de acceso vive solo en memoria (dura minutos). El de renovación se guarda en el
// navegador para no pedir login al recargar, pero el servidor lo vence a las pocas horas
// y lo invalida al salir, al cambiar la contraseña o al desactivar el usuario.
let tokenAcceso: string | null = null
let renovando: Promise<SesionRespuesta | null> | null = null
let alCerrarSesion: (() => void) | null = null
let alRenovar: ((sesion: SesionRespuesta) => void) | null = null

export const sesionAlmacenada = {
  leer: () => {
    try {
      return localStorage.getItem(CLAVE_RENOVACION)
    } catch {
      return null
    }
  },
  guardar: (sesion: SesionRespuesta) => {
    tokenAcceso = sesion.tokenAcceso
    try {
      localStorage.setItem(CLAVE_RENOVACION, sesion.tokenRenovacion)
    } catch {
      /* sin almacenamiento: la sesión dura hasta cerrar la pestaña */
    }
  },
  borrar: () => {
    tokenAcceso = null
    try {
      localStorage.removeItem(CLAVE_RENOVACION)
    } catch {
      /* nada */
    }
  },
}

export function escucharSesion(opciones: { alCerrar: () => void; alRenovar: (s: SesionRespuesta) => void }) {
  alCerrarSesion = opciones.alCerrar
  alRenovar = opciones.alRenovar
}

/** Pide un token nuevo. Si varias llamadas lo necesitan a la vez, se hace una sola renovación. */
export function renovarSesion(): Promise<SesionRespuesta | null> {
  renovando ??= renovar().finally(() => {
    renovando = null
  })
  return renovando
}

async function renovar(): Promise<SesionRespuesta | null> {
  const token = sesionAlmacenada.leer()
  if (!token) return null
  try {
    const sesion = await solicitud<SesionRespuesta>('POST', '/api/auth/renovar', { tokenRenovacion: token }, false)
    sesionAlmacenada.guardar(sesion)
    alRenovar?.(sesion)
    return sesion
  } catch (error) {
    if (error instanceof ErrorApi && error.estado === 401) {
      sesionAlmacenada.borrar()
      alCerrarSesion?.()
      return null
    }
    throw error
  }
}

async function solicitud<T>(metodo: string, ruta: string, cuerpo?: unknown, autenticar = true, reintento = true): Promise<T> {
  const encabezados: Record<string, string> = {}
  if (cuerpo !== undefined) encabezados['Content-Type'] = 'application/json'
  if (autenticar && tokenAcceso) encabezados.Authorization = `Bearer ${tokenAcceso}`

  let respuesta: Response
  try {
    respuesta = await fetch(obtenerConfiguracion().apiUrl + ruta, {
      method: metodo,
      headers: encabezados,
      body: cuerpo === undefined ? undefined : JSON.stringify(cuerpo),
    })
  } catch {
    throw new ErrorApi('No hay conexión con el servidor de CRV4.', 0)
  }

  if (respuesta.status === 401 && autenticar && reintento) {
    const sesion = await renovarSesion()
    if (sesion) return solicitud<T>(metodo, ruta, cuerpo, autenticar, false)
  }

  if (!respuesta.ok) throw await leerError(respuesta)
  if (respuesta.status === 204) return undefined as T
  return (await respuesta.json()) as T
}

async function leerError(respuesta: Response): Promise<ErrorApi> {
  try {
    const problema = (await respuesta.json()) as { title?: string; errors?: Record<string, string[]> }
    if (problema.errors) {
      const mensajes = Object.values(problema.errors).flat()
      return new ErrorApi(mensajes[0] ?? 'Hay datos con errores.', respuesta.status, problema.errors)
    }
    if (problema.title) return new ErrorApi(problema.title, respuesta.status)
  } catch {
    /* respuesta sin cuerpo JSON */
  }
  const porEstado: Record<number, string> = {
    403: 'No tenés permiso para esta acción.',
    404: 'No se encontró el dato.',
    429: 'Demasiados intentos. Esperá un minuto.',
  }
  return new ErrorApi(porEstado[respuesta.status] ?? `Error del servidor (${respuesta.status}).`, respuesta.status)
}

export const api = {
  get: <T>(ruta: string) => solicitud<T>('GET', ruta),
  post: <T>(ruta: string, cuerpo?: unknown) => solicitud<T>('POST', ruta, cuerpo ?? {}),
  put: <T>(ruta: string, cuerpo: unknown) => solicitud<T>('PUT', ruta, cuerpo),
  delete: <T>(ruta: string) => solicitud<T>('DELETE', ruta),
  /** Para login y salud, que no llevan token. */
  publico: <T>(metodo: string, ruta: string, cuerpo?: unknown) => solicitud<T>(metodo, ruta, cuerpo, false),
}
