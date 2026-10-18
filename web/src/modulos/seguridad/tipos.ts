export interface RolResumen {
  id: number
  nombre: string
}

export interface UsuarioDto {
  id: number
  nombreUsuario: string
  nombre: string
  email: string | null
  roles: RolResumen[]
  activo: boolean
  debeCambiarPassword: boolean
  bloqueado: boolean
  ultimoIngreso: string | null
}

export interface RolDto {
  id: number
  nombre: string
  descripcion: string | null
  activo: boolean
  /** El rol Administrador tiene todos los permisos y no se edita. */
  esAdministrador: boolean
  usuarios: number
  permisos: string[]
}

export interface PermisoDto {
  codigo: string
  modulo: string
  descripcion: string
}
