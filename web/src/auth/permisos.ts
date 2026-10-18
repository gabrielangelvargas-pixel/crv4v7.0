// Mismos códigos que Crv4.Api/Modulos/Seguridad/Permisos.cs
export const Permisos = {
  InicioVer: 'inicio.ver',
  ProductosVer: 'productos.ver',
  ProductosEditar: 'productos.editar',
  ClientesVer: 'clientes.ver',
  ClientesEditar: 'clientes.editar',
  VentasVer: 'ventas.ver',
  VentasCrear: 'ventas.crear',
  VentasEditar: 'ventas.editar',
  VentasAnular: 'ventas.anular',
  EstadisticasVer: 'estadisticas.ver',
  ControlVentasVer: 'control_ventas.ver',
  RubrosVer: 'rubros.ver',
  RubrosEditar: 'rubros.editar',
  SeguridadUsuarios: 'seguridad.usuarios',
  SeguridadRoles: 'seguridad.roles',
} as const

export type Permiso = (typeof Permisos)[keyof typeof Permisos]
