export interface UsuarioDto {
  id: number
  nombreUsuario: string
  nombre: string
  rolId: number
  rol: string
  activo: boolean
  debeCambiarPassword: boolean
  bloqueado: boolean
  ultimoIngreso: string | null
}

export interface RolDto {
  id: number
  nombre: string
  descripcion: string | null
  esSistema: boolean
  usuarios: number
  permisos: string[]
}

export interface PermisoDto {
  codigo: string
  modulo: string
  descripcion: string
}
