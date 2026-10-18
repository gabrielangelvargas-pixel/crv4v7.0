// Mismos códigos que Crv4.Api/Modulos/Seguridad/Permisos.cs (y que la tabla permisos de crv4_datos).
export const Permisos = {
  ClientesVer: 'clientes.ver',
  ClientesCrear: 'clientes.crear',
  ClientesEditar: 'clientes.editar',
  ClientesEliminar: 'clientes.eliminar',
  ProductosVer: 'productos.ver',
  ProductosCrear: 'productos.crear',
  ProductosEditar: 'productos.editar',
  ProductosEliminar: 'productos.eliminar',
  RubrosVer: 'rubros.ver',
  RubrosCrear: 'rubros.crear',
  RubrosEditar: 'rubros.editar',
  RubrosEliminar: 'rubros.eliminar',
  VentasVer: 'ventas.ver',
  VentasCrear: 'ventas.crear',
  VentasEditar: 'ventas.editar',
  VentasAnular: 'ventas.anular',
  /** Estadísticas y Control Ventas. */
  ReportesVer: 'reportes.ver',
  /** Pantalla de Seguridad: usuarios, roles y permisos. */
  UsuariosAdministrar: 'usuarios.administrar',
  ConfiguracionAdministrar: 'configuracion.administrar',
} as const

export type Permiso = (typeof Permisos)[keyof typeof Permisos]
