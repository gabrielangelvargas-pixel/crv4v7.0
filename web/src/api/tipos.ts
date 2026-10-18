export interface UsuarioSesion {
  id: number
  nombreUsuario: string
  nombre: string
  roles: string[]
  permisos: string[]
  debeCambiarPassword: boolean
}

export interface SesionRespuesta {
  tokenAcceso: string
  accesoExpiraEn: string
  tokenRenovacion: string
  sesionExpiraEn: string
  usuario: UsuarioSesion
}

export interface EstadoSalud {
  estado: 'ok' | 'migraciones-pendientes' | 'sin-base-datos'
  version: string
  baseDatos: boolean
  migracionesPendientes: number
}
