export interface Paginado<T> {
  items: T[]
  total: number
  pagina: number
  tamanoPagina: number
}

export interface Opcion {
  id: number
  nombre: string
}

export interface VentaResumen {
  id: number
  fecha: string
  idCliente: number
  cliente: string
  idVendedor: number
  vendedor: string
  idEstado: number
  estado: string
  items: number
  total: number
  pagado: number
}

export interface VentaItem {
  id: number
  idProducto: string
  descripcion: string | null
  precioCosto: number
  precioVenta: number
  cantidad: number
  subTotalCosto: number
  subTotalVenta: number
  idProveedorVenta: string | null
  subtotalBrutoVenta: number
  descuentoProveedores: number
  subtotalNetoVenta: number
}

export interface Venta extends Omit<VentaResumen, 'items' | 'total'> {
  descuentoBono: number
  subtotalProveedoresBonificados: number
  descuentoEscalon1: number
  descuentoEscalon2: number
  descuentoEscalon3: number
  descuentoProveedores: number
  fechaFinalizacion: string | null
  fechaCobro: string | null
  totalCosto: number
  totalVenta: number
  items: VentaItem[]
}

/** Lo que hace falta de un producto para cargarlo en una venta. */
export interface ProductoVenta {
  codigo: string
  descripcion: string
  idProveedor: string
  rubro: string
  precioVenta: number
  precioOferta: number | null
  fechaInicioOferta: string | null
  fechaFinOferta: string | null
}

export interface ItemFormulario {
  idProducto: string
  descripcion: string
  idProveedor: string
  precioVenta: string
  cantidad: string
}

export interface FormularioVenta {
  idCliente: string
  /** Lo que se ve en el buscador de clientes ("123 - Nombre"). */
  cliente: string
  idVendedor: string
  idEstado: string
  descuentoBono: string
  usarPrecioOfertaSinVigencia: boolean
  items: ItemFormulario[]
}

/** Estados de la tabla estados, iguales en Sistema03. */
export const Estados = { Pendiente: 1, Parcial: 2, Pagada: 3, Anulada: 4 } as const

/** Proveedores que participan del descuento por escalones. */
export const PROVEEDORES_BONIFICADOS = ['02', '46']

export const moneda = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })
const fecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
const hora = new Intl.DateTimeFormat('es-AR', { hour: '2-digit', minute: '2-digit', hourCycle: 'h23' })

export const MESES = [
  'Enero',
  'Febrero',
  'Marzo',
  'Abril',
  'Mayo',
  'Junio',
  'Julio',
  'Agosto',
  'Septiembre',
  'Octubre',
  'Noviembre',
  'Diciembre',
]

/** La API manda la hora local del servidor sin zona horaria: el navegador la toma como local. */
export function fechaHora(valor: string) {
  const d = new Date(valor)
  return Number.isNaN(d.getTime()) ? valor : `${fecha.format(d)} · ${hora.format(d)}`
}

export const redondear = (valor: number) => Math.round((valor + Number.EPSILON) * 100) / 100

export const numero = (texto: string) => {
  const valor = Number(texto || 0)
  return Number.isFinite(valor) ? valor : 0
}

/** Mismo cálculo que hace la API al guardar: descuento por escalones sobre lo bonificado y después el bono. */
export function calcularTotales(f: FormularioVenta) {
  const importe = (i: ItemFormulario) => numero(i.cantidad) * numero(i.precioVenta)
  const subtotal = f.items.reduce((t, i) => t + importe(i), 0)
  const participan = f.items.filter((i) => PROVEEDORES_BONIFICADOS.includes(i.idProveedor)).reduce((t, i) => t + importe(i), 0)
  let resto = participan
  const escalon1 = participan > 100_000 ? redondear(resto * 0.06) : 0
  resto -= escalon1
  const escalon2 = participan > 200_000 ? redondear(resto * 0.05) : 0
  resto -= escalon2
  const escalon3 = participan > 400_000 ? redondear(resto * 0.05) : 0
  const descuentoProveedores = redondear(escalon1 + escalon2 + escalon3)
  return {
    subtotal,
    participan,
    noParticipan: subtotal - participan,
    escalon1,
    escalon2,
    escalon3,
    total: Math.max(subtotal - descuentoProveedores - numero(f.descuentoBono), 0),
    productos: f.items.length,
    unidades: f.items.reduce((t, i) => t + numero(i.cantidad), 0),
  }
}

export function ofertaVigente(p: ProductoVenta, sinValidarFechas: boolean) {
  if ((p.precioOferta ?? 0) <= 0) return false
  if (sinValidarFechas) return true
  const ahora = new Date()
  const desde = p.fechaInicioOferta ? new Date(p.fechaInicioOferta) : null
  const hasta = p.fechaFinOferta ? new Date(p.fechaFinOferta) : null
  return (!desde || desde <= ahora) && (!hasta || hasta >= ahora)
}

/** Renglón nuevo para un producto, con la oferta si corresponde (la descripción lleva "OFF..." como en Sistema03). */
export function itemDesdeProducto(p: ProductoVenta, sinValidarFechas: boolean): ItemFormulario {
  const oferta = ofertaVigente(p, sinValidarFechas)
  const descripcion = p.descripcion.trim()
  return {
    idProducto: p.codigo,
    descripcion: oferta && !descripcion.toUpperCase().startsWith('OFF...') ? `OFF... ${descripcion}` : descripcion,
    idProveedor: p.idProveedor,
    precioVenta: String(oferta ? p.precioOferta : p.precioVenta),
    cantidad: '1',
  }
}

export function cuerpoVenta(f: FormularioVenta) {
  return {
    idCliente: Number(f.idCliente),
    idVendedor: Number(f.idVendedor),
    idEstado: Number(f.idEstado),
    descuentoBono: numero(f.descuentoBono),
    usarPrecioOfertaSinVigencia: f.usarPrecioOfertaSinVigencia,
    items: f.items.map((i) => ({ idProducto: i.idProducto, cantidad: numero(i.cantidad), precioVenta: numero(i.precioVenta) })),
  }
}

export function completa(f: FormularioVenta) {
  return Boolean(
    f.idCliente &&
    f.idVendedor &&
    f.idEstado &&
    f.items.length > 0 &&
    f.items.every((i) => numero(i.cantidad) > 0 && (numero(i.precioVenta) >= 0 || i.idProducto === 'REDONDEO')),
  )
}

export function formularioDesdeVenta(v: Venta): FormularioVenta {
  return {
    idCliente: String(v.idCliente),
    cliente: `${v.idCliente} - ${v.cliente.trim()}`,
    idVendedor: String(v.idVendedor),
    idEstado: String(v.idEstado),
    descuentoBono: String(v.descuentoBono),
    usarPrecioOfertaSinVigencia: false,
    items: v.items.map((i) => ({
      idProducto: i.idProducto,
      descripcion: i.descripcion ?? '',
      idProveedor: i.idProveedorVenta ?? '',
      precioVenta: String(i.precioVenta),
      cantidad: String(i.cantidad),
    })),
  }
}

const CLAVE_BORRADOR = 'crv4.venta-borrador'

export interface Borrador {
  idVenta: number | null
  formulario: FormularioVenta
}

/** Respaldo en el navegador por si se cierra la pestaña antes de que se autoguarde. */
export const borrador = {
  leer(): Borrador | null {
    try {
      const datos = JSON.parse(localStorage.getItem(CLAVE_BORRADOR) ?? 'null') as Borrador | null
      return datos && Array.isArray(datos.formulario?.items) ? datos : null
    } catch {
      return null
    }
  },
  guardar(datos: Borrador) {
    try {
      localStorage.setItem(CLAVE_BORRADOR, JSON.stringify(datos))
    } catch {
      /* sin almacenamiento */
    }
  },
  borrar() {
    try {
      localStorage.removeItem(CLAVE_BORRADOR)
    } catch {
      /* nada */
    }
  },
}

/** Tono grave cuando el código escaneado no existe, como en Sistema03. */
export function sonidoNoEncontrado() {
  const Contexto = window.AudioContext ?? (window as typeof window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
  if (!Contexto) return
  const ctx = new Contexto()
  const t = ctx.currentTime
  const volumen = ctx.createGain()
  volumen.gain.setValueAtTime(0.0001, t)
  volumen.gain.exponentialRampToValueAtTime(0.28, t + 0.015)
  volumen.gain.exponentialRampToValueAtTime(0.0001, t + 0.48)
  volumen.connect(ctx.destination)
  for (const [inicio, frecuencia] of [
    [0, 220],
    [0.16, 185],
    [0.32, 147],
  ]) {
    const osc = ctx.createOscillator()
    const g = ctx.createGain()
    osc.type = 'square'
    osc.frequency.setValueAtTime(frecuencia, t + inicio)
    g.gain.setValueAtTime(0.0001, t + inicio)
    g.gain.exponentialRampToValueAtTime(0.9, t + inicio + 0.01)
    g.gain.exponentialRampToValueAtTime(0.0001, t + inicio + 0.12)
    osc.connect(g)
    g.connect(volumen)
    osc.start(t + inicio)
    osc.stop(t + inicio + 0.13)
  }
  window.setTimeout(() => void ctx.close(), 600)
}
