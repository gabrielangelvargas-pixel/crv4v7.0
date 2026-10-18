import { BarChart3, ClipboardList, House, Package, ShieldCheck, ShoppingCart, Tag, Users, type LucideIcon } from 'lucide-react'
import { Permisos, type Permiso } from '../auth/permisos'

export interface ItemMenu {
  ruta: string
  titulo: string
  icono: LucideIcon
  /** Alcanza con tener cualquiera de estos permisos para ver la opción. Vacío: cualquier usuario con sesión. */
  permisos: Permiso[]
}

export const menu: ItemMenu[] = [
  { ruta: '/', titulo: 'Inicio', icono: House, permisos: [] },
  { ruta: '/productos', titulo: 'Productos', icono: Package, permisos: [Permisos.ProductosVer] },
  { ruta: '/clientes', titulo: 'Clientes', icono: Users, permisos: [Permisos.ClientesVer] },
  { ruta: '/ventas', titulo: 'Ventas', icono: ShoppingCart, permisos: [Permisos.VentasVer] },
  { ruta: '/estadisticas', titulo: 'Estadísticas', icono: BarChart3, permisos: [Permisos.ReportesVer] },
  { ruta: '/control-ventas', titulo: 'Control Ventas', icono: ClipboardList, permisos: [Permisos.ReportesVer] },
  { ruta: '/rubros', titulo: 'Rubros', icono: Tag, permisos: [Permisos.RubrosVer] },
  { ruta: '/seguridad', titulo: 'Seguridad', icono: ShieldCheck, permisos: [Permisos.UsuariosAdministrar] },
]
