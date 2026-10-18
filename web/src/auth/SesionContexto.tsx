import { createContext, use, useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, escucharSesion, renovarSesion, sesionAlmacenada } from '../api/cliente'
import type { SesionRespuesta, UsuarioSesion } from '../api/tipos'
import type { Permiso } from './permisos'

interface ValorSesion {
  usuario: UsuarioSesion | null
  cargando: boolean
  ingresar: (usuario: string, password: string) => Promise<void>
  salir: () => Promise<void>
  aplicarSesion: (sesion: SesionRespuesta) => void
  tiene: (...permisos: Permiso[]) => boolean
}

const Contexto = createContext<ValorSesion | null>(null)

export function ProveedorSesion({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioSesion | null>(null)
  const [cargando, setCargando] = useState(true)

  const aplicarSesion = useCallback((sesion: SesionRespuesta) => {
    sesionAlmacenada.guardar(sesion)
    setUsuario(sesion.usuario)
  }, [])

  useEffect(() => {
    escucharSesion({ alCerrar: () => setUsuario(null), alRenovar: (s) => setUsuario(s.usuario) })
    // Al abrir la web se intenta retomar la sesión guardada.
    renovarSesion()
      .catch(() => null)
      .finally(() => setCargando(false))
  }, [])

  const ingresar = useCallback(
    async (nombreUsuario: string, password: string) => {
      const sesion = await api.publico<SesionRespuesta>('POST', '/api/auth/login', { usuario: nombreUsuario, password })
      aplicarSesion(sesion)
    },
    [aplicarSesion],
  )

  const salir = useCallback(async () => {
    const token = sesionAlmacenada.leer()
    sesionAlmacenada.borrar()
    setUsuario(null)
    if (token) await api.publico('POST', '/api/auth/salir', { tokenRenovacion: token }).catch(() => undefined)
  }, [])

  const tiene = useCallback(
    (...permisos: Permiso[]) => !!usuario && permisos.some((p) => usuario.permisos.includes(p)),
    [usuario],
  )

  const valor = useMemo(
    () => ({ usuario, cargando, ingresar, salir, aplicarSesion, tiene }),
    [usuario, cargando, ingresar, salir, aplicarSesion, tiene],
  )
  return <Contexto value={valor}>{children}</Contexto>
}

export function useSesion() {
  const valor = use(Contexto)
  if (!valor) throw new Error('useSesion fuera de ProveedorSesion')
  return valor
}
