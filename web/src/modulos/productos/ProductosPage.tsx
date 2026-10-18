import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Copy, Pencil, Plus, Trash2 } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../../api/cliente'
import { Permisos } from '../../auth/permisos'
import { useSesion } from '../../auth/SesionContexto'
import { Aviso, Boton, Campo, Casilla, EstadoActivo, Modal, Selector, Tarjeta } from '../../ui/componentes'
import { aplanar, armarArbol, type Rubro } from '../rubros/arbol'
import './productos.css'

export interface Producto {
  codigo: string
  descripcion: string
  idRubro: string
  rubro: string
  idProveedor: string
  proveedor: string
  /** Solo la ve el rol Administrador. */
  referenciaProveedor: string | null
  /** Los costos solo los ve quien puede crear o editar productos. */
  precioCompra: number | null
  descuento: number | null
  precioCosto: number | null
  utilidad: number | null
  precioVenta: number
  precioOferta: number | null
  fechaInicioOferta: string | null
  fechaFinOferta: string | null
  fechaAlta: string
  activo: boolean
  imagenPrincipal: string | null
}

interface Proveedor {
  codigo: string
  nombre: string
}

interface Paginado<T> {
  items: T[]
  total: number
  pagina: number
  tamanoPagina: number
}

type Estado = 'activos' | 'todos' | 'inactivos'

/** Los números van como texto para poder editarlos libremente; se convierten al guardar. */
interface Formulario {
  codigo: string
  referenciaProveedor: string
  descripcion: string
  idRubro: string
  idProveedor: string
  precioCompra: string
  descuento: string
  utilidad: string
  precioVenta: string
  precioOferta: string
  fechaInicioOferta: string
  fechaFinOferta: string
  activo: boolean
  imagenPrincipal: string
}

type Edicion = { modo: 'nuevo' | 'duplicar'; formulario: Formulario; origen?: string } | { modo: 'editar'; codigo: string }

const TAMANO_PAGINA = 100
const vacio: Formulario = {
  codigo: '',
  referenciaProveedor: '',
  descripcion: '',
  idRubro: '',
  idProveedor: '',
  precioCompra: '0',
  descuento: '0',
  utilidad: '0',
  precioVenta: '0',
  precioOferta: '',
  fechaInicioOferta: '',
  fechaFinOferta: '',
  activo: true,
  imagenPrincipal: '',
}
const moneda = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })

export function ProductosPage() {
  const { usuario, tiene } = useSesion()
  const puedeCrear = tiene(Permisos.ProductosCrear)
  const puedeEditar = tiene(Permisos.ProductosEditar)
  const verCostos = puedeCrear || puedeEditar
  const verReferencia = usuario?.roles.some((r) => r.trim().toLowerCase() === 'administrador') ?? false
  const [buscar, setBuscar] = useState('')
  const [texto, setTexto] = useState('')
  const [idRubro, setIdRubro] = useState('')
  const [idProveedor, setIdProveedor] = useState('')
  const [estado, setEstado] = useState<Estado>('activos')
  const [pagina, setPagina] = useState(1)
  const [edicion, setEdicion] = useState<Edicion | null>(null)

  // Espera a que se deje de escribir para no consultar con cada tecla.
  useEffect(() => {
    const espera = window.setTimeout(() => setTexto(buscar.trim()), 250)
    return () => window.clearTimeout(espera)
  }, [buscar])

  const rubros = useQuery({ queryKey: ['rubros'], queryFn: () => api.get<Rubro[]>('/api/rubros') })
  const proveedores = useQuery({ queryKey: ['proveedores'], queryFn: () => api.get<Proveedor[]>('/api/proveedores') })
  const parametros = new URLSearchParams({ pagina: String(pagina), tamano: String(TAMANO_PAGINA) })
  if (texto) parametros.set('buscar', texto)
  if (idRubro) parametros.set('idRubro', idRubro)
  if (idProveedor) parametros.set('idProveedor', idProveedor)
  if (estado !== 'todos') parametros.set('activo', String(estado === 'activos'))
  const productos = useQuery({
    queryKey: ['productos', parametros.toString()],
    queryFn: () => api.get<Paginado<Producto>>(`/api/productos?${parametros}`),
    placeholderData: keepPreviousData,
    // Como en Sistema03: se mantiene al día si otro usuario cambia precios, salvo mientras se edita.
    refetchInterval: edicion ? false : 5000,
  })

  const opcionesRubro = aplanar(armarArbol(rubros.data ?? []))
  const datos = productos.data
  const paginas = datos ? Math.max(1, Math.ceil(datos.total / datos.tamanoPagina)) : 1
  const filtrar = <T,>(cambiar: (valor: T) => void) => (valor: T) => {
    cambiar(valor)
    setPagina(1)
  }

  async function duplicar(p: Producto) {
    const detalle = await api.get<Producto>(`/api/productos/${encodeURIComponent(p.codigo)}`)
    setEdicion({ modo: 'duplicar', formulario: { ...desdeProducto(detalle), codigo: '' }, origen: detalle.codigo })
  }

  return (
    <>
      <Tarjeta className="tarjeta--lista">
        <div className="filtros-productos">
          <input
            className="campo__control filtros-productos__busqueda"
            placeholder="Buscar por código, descripción o referencia…"
            aria-label="Buscar producto"
            value={buscar}
            onChange={(e) => filtrar(setBuscar)(e.target.value)}
          />
          <Selector etiqueta="Rubro" value={idRubro} onChange={(e) => filtrar(setIdRubro)(e.target.value)}>
            <option value="">Todos los rubros</option>
            {opcionesRubro.map(({ rubro: r, nivel }) => (
              <option key={r.id} value={r.codigo}>
                {'   '.repeat(nivel)}
                {r.codigo} · {r.nombre}
              </option>
            ))}
          </Selector>
          <Selector etiqueta="Proveedor" value={idProveedor} onChange={(e) => filtrar(setIdProveedor)(e.target.value)}>
            <option value="">Todos los proveedores</option>
            {(proveedores.data ?? []).map((p) => (
              <option key={p.codigo} value={p.codigo}>
                {p.nombre}
              </option>
            ))}
          </Selector>
          <Selector etiqueta="Estado" value={estado} onChange={(e) => filtrar(setEstado)(e.target.value as Estado)}>
            <option value="activos">Activos</option>
            <option value="todos">Todos</option>
            <option value="inactivos">Inactivos</option>
          </Selector>
          {puedeCrear && (
            <Boton variante="primario" className="filtros-productos__nuevo" onClick={() => setEdicion({ modo: 'nuevo', formulario: vacio })}>
              <Plus size={16} /> Nuevo producto
            </Boton>
          )}
        </div>
        <Aviso>{productos.error?.message}</Aviso>
        {datos && (
          <div className="productos-resumen">
            <span>
              Productos encontrados: <strong>{datos.total.toLocaleString('es-AR')}</strong>
            </span>
            {paginas > 1 && (
              <Paginador pagina={datos.pagina} paginas={paginas} cambiar={setPagina} />
            )}
          </div>
        )}
        <div className="tabla-contenedor zona-desplazable">
          <table className="tabla tabla-productos">
            <thead>
              <tr>
                <th>Código</th>
                <th>Descripción</th>
                {verCostos && <th className="numero">Compra</th>}
                <th className="numero">Venta</th>
                <th className="numero">Oferta</th>
                <th>Estado</th>
                {(puedeEditar || puedeCrear) && <th />}
              </tr>
            </thead>
            <tbody>
              {datos?.items.map((p) => (
                <tr key={p.codigo}>
                  <td className="tabla-productos__codigo" data-etiqueta="Código">{p.codigo}</td>
                  <td className="tabla-productos__descripcion" data-etiqueta="Descripción">
                    <div className="tabla-productos__nombre">{p.descripcion.trim()}</div>
                    <div className="tabla-productos__detalle">
                      <span title="Rubro">{p.rubro}</span>
                      <span title="Proveedor">{p.proveedor}</span>
                      {verReferencia && p.referenciaProveedor?.trim() && <span title="Referencia del proveedor">Ref. {p.referenciaProveedor.trim()}</span>}
                    </div>
                  </td>
                  {verCostos && <td className="numero" data-etiqueta="Compra">{moneda.format(p.precioCompra ?? 0)}</td>}
                  <td className="numero tabla-productos__venta" data-etiqueta="Venta">{moneda.format(p.precioVenta)}</td>
                  <td className="numero" data-etiqueta="Oferta">{p.precioOferta ? moneda.format(p.precioOferta) : '-'}</td>
                  <td data-etiqueta="Estado">
                    <EstadoActivo activo={p.activo} />
                  </td>
                  {(puedeEditar || puedeCrear) && (
                    <td className="acciones">
                      {puedeEditar && (
                        <Boton chico onClick={() => setEdicion({ modo: 'editar', codigo: p.codigo })} aria-label={`Editar ${p.descripcion}`} title="Editar">
                          <Pencil size={14} /> <span className="solo-ancho">Editar</span>
                        </Boton>
                      )}{' '}
                      {puedeCrear && (
                        <Boton chico onClick={() => void duplicar(p)} aria-label={`Duplicar ${p.descripcion}`} title="Duplicar">
                          <Copy size={14} /> <span className="solo-ancho">Duplicar</span>
                        </Boton>
                      )}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
          {productos.isPending && <div className="vacio">Cargando…</div>}
          {datos && datos.items.length === 0 && <div className="vacio">No hay productos con esos filtros.</div>}
        </div>
        {datos && paginas > 1 && (
          <div className="productos-resumen productos-resumen--pie">
            <Paginador pagina={datos.pagina} paginas={paginas} cambiar={setPagina} />
          </div>
        )}
      </Tarjeta>

      <Modal titulo={tituloEdicion(edicion)} abierto={!!edicion} alCerrar={() => setEdicion(null)} grande>
        {edicion && (
          <EditorProducto
            edicion={edicion}
            rubros={opcionesRubro}
            proveedores={proveedores.data ?? []}
            verReferencia={verReferencia}
            alTerminar={() => setEdicion(null)}
          />
        )}
      </Modal>
    </>
  )
}

function tituloEdicion(edicion: Edicion | null) {
  if (edicion?.modo === 'editar') return 'Editar producto'
  if (edicion?.modo === 'duplicar') return 'Duplicar producto'
  return 'Nuevo producto'
}

function Paginador({ pagina, paginas, cambiar }: { pagina: number; paginas: number; cambiar: (p: number) => void }) {
  return (
    <div className="paginador">
      <Boton chico disabled={pagina <= 1} onClick={() => cambiar(pagina - 1)} aria-label="Página anterior">
        <ChevronLeft size={16} />
      </Boton>
      <span>
        Página {pagina} de {paginas}
      </span>
      <Boton chico disabled={pagina >= paginas} onClick={() => cambiar(pagina + 1)} aria-label="Página siguiente">
        <ChevronRight size={16} />
      </Boton>
    </div>
  )
}

/** Carga el producto si se está editando; al duplicar o crear ya viene el formulario armado. */
function EditorProducto(props: {
  edicion: Edicion
  rubros: { rubro: Rubro; nivel: number }[]
  proveedores: Proveedor[]
  verReferencia: boolean
  alTerminar: () => void
}) {
  const { edicion } = props
  const codigo = edicion.modo === 'editar' ? edicion.codigo : null
  const detalle = useQuery({
    queryKey: ['producto', codigo],
    queryFn: () => api.get<Producto>(`/api/productos/${encodeURIComponent(codigo!)}`),
    enabled: codigo != null,
    gcTime: 0,
  })
  if (edicion.modo !== 'editar') return <FormularioProducto {...props} inicial={edicion.formulario} origen={edicion.origen} />
  if (detalle.error) return <Aviso>{detalle.error.message}</Aviso>
  if (!detalle.data) return <div className="vacio">Cargando producto…</div>
  return <FormularioProducto {...props} inicial={desdeProducto(detalle.data)} codigoActual={detalle.data.codigo} />
}

function FormularioProducto({
  inicial,
  codigoActual,
  origen,
  rubros,
  proveedores,
  verReferencia,
  alTerminar,
}: {
  inicial: Formulario
  codigoActual?: string
  origen?: string
  rubros: { rubro: Rubro; nivel: number }[]
  proveedores: Proveedor[]
  verReferencia: boolean
  alTerminar: () => void
}) {
  const { tiene } = useSesion()
  const cliente = useQueryClient()
  const [f, setF] = useState(inicial)
  const [confirmarBorrado, setConfirmarBorrado] = useState(false)
  const costo = calcularCosto(f)
  const poner = <K extends keyof Formulario>(campo: K, valor: Formulario[K]) => setF((actual) => ({ ...actual, [campo]: valor }))

  const refrescar = async () => {
    await cliente.invalidateQueries({ queryKey: ['productos'] })
    await cliente.invalidateQueries({ queryKey: ['rubros'] })
    await cliente.invalidateQueries({ queryKey: ['inicio'] })
  }
  const guardar = useMutation({
    mutationFn: () => {
      const cuerpo = {
        codigo: f.codigo.trim(),
        referenciaProveedor: f.referenciaProveedor.trim() || null,
        descripcion: f.descripcion.trim(),
        idRubro: f.idRubro,
        idProveedor: f.idProveedor,
        precioCompra: numero(f.precioCompra),
        descuento: numero(f.descuento),
        utilidad: numero(f.utilidad),
        precioVenta: numero(f.precioVenta),
        precioOferta: f.precioOferta === '' ? null : numero(f.precioOferta),
        fechaInicioOferta: f.fechaInicioOferta || null,
        fechaFinOferta: f.fechaFinOferta || null,
        activo: f.activo,
        imagenPrincipal: f.imagenPrincipal.trim() || null,
      }
      return codigoActual ? api.put(`/api/productos/${encodeURIComponent(codigoActual)}`, cuerpo) : api.post('/api/productos', cuerpo)
    },
    onSuccess: async () => {
      await refrescar()
      alTerminar()
    },
  })
  const eliminar = useMutation({
    mutationFn: () => api.delete(`/api/productos/${encodeURIComponent(codigoActual!)}`),
    onSuccess: async () => {
      await refrescar()
      alTerminar()
    },
  })

  /** Compra, descuento y utilidad recalculan la venta; la venta recalcula la utilidad (como en Sistema03). */
  function ponerPrecio(campo: 'precioCompra' | 'descuento' | 'utilidad' | 'precioVenta', valor: string) {
    setF((actual) => {
      const siguiente = { ...actual, [campo]: valor }
      const c = calcularCosto(siguiente)
      if (campo === 'precioVenta') siguiente.utilidad = redondear(c > 0 ? ((numero(valor) - c) / c) * 100 : 0, 3)
      else siguiente.precioVenta = redondear(Math.max(c + (c * numero(siguiente.utilidad)) / 100, 0), 2)
      return siguiente
    })
  }

  function enviar(e: FormEvent) {
    e.preventDefault()
    guardar.mutate()
  }

  return (
    <form className="formulario" onSubmit={enviar}>
      {origen && <Aviso tipo="info">Datos copiados de {origen}. Poné un código nuevo para guardarlo como otro producto.</Aviso>}
      <Aviso>{guardar.error?.message ?? eliminar.error?.message}</Aviso>
      <div className="formulario-producto">
        <div className="fp-2">
          <Campo etiqueta="Código" value={f.codigo} onChange={(e) => poner('codigo', e.target.value)} maxLength={13} required autoFocus />
        </div>
        <div className="fp-casilla">
          <Casilla etiqueta="Activo" checked={f.activo} onChange={(e) => poner('activo', e.target.checked)} />
        </div>
        <div className="fp-todo">
          <Campo etiqueta="Descripción" value={f.descripcion} onChange={(e) => poner('descripcion', e.target.value)} maxLength={100} required />
        </div>
        <div className="fp-3">
          <Selector etiqueta="Proveedor" value={f.idProveedor} onChange={(e) => poner('idProveedor', e.target.value)} required>
            <option value="">Elegí el proveedor</option>
            {proveedores.map((p) => (
              <option key={p.codigo} value={p.codigo}>
                {p.codigo} · {p.nombre}
              </option>
            ))}
          </Selector>
        </div>
        {verReferencia && (
          <div className="fp-3">
            <Campo etiqueta="Referencia del proveedor" value={f.referenciaProveedor} onChange={(e) => poner('referenciaProveedor', e.target.value)} maxLength={45} />
          </div>
        )}
        <div className="fp-todo">
          <Selector etiqueta="Rubro" value={f.idRubro} onChange={(e) => poner('idRubro', e.target.value)} required>
            <option value="">Elegí el rubro</option>
            {rubros.map(({ rubro: r, nivel }) => (
              <option key={r.id} value={r.codigo}>
                {'   '.repeat(nivel)}
                {r.codigo} · {r.nombre}
              </option>
            ))}
          </Selector>
        </div>
        <div className="fp-2">
          <Campo etiqueta="Precio de compra" type="number" min="0" step="0.01" value={f.precioCompra} onChange={(e) => ponerPrecio('precioCompra', e.target.value)} required />
        </div>
        <div className="fp-2">
          <Campo
            etiqueta={numero(f.descuento) < 0 ? 'Recargo %' : 'Descuento %'}
            type="number"
            min="-100"
            max="100"
            step="0.01"
            value={f.descuento}
            onChange={(e) => ponerPrecio('descuento', e.target.value)}
          />
        </div>
        <div className="fp-2">
          <Campo etiqueta="Costo" value={moneda.format(costo)} readOnly tabIndex={-1} />
        </div>
        <div className="fp-2">
          <Campo etiqueta="Utilidad %" type="number" step="0.001" value={f.utilidad} onChange={(e) => ponerPrecio('utilidad', e.target.value)} />
        </div>
        <div className="fp-2">
          <Campo etiqueta="Precio de venta" type="number" min="0" step="0.01" value={f.precioVenta} onChange={(e) => ponerPrecio('precioVenta', e.target.value)} required />
        </div>
        <div className="fp-2">
          <Campo etiqueta="Precio de oferta" type="number" min="0" step="0.01" value={f.precioOferta} onChange={(e) => poner('precioOferta', e.target.value)} />
        </div>
        <div className="fp-3">
          <Campo etiqueta="Oferta desde" type="datetime-local" value={f.fechaInicioOferta} onChange={(e) => poner('fechaInicioOferta', e.target.value)} />
        </div>
        <div className="fp-3">
          <Campo etiqueta="Oferta hasta" type="datetime-local" value={f.fechaFinOferta} onChange={(e) => poner('fechaFinOferta', e.target.value)} />
        </div>
        <div className="fp-todo fp-imagen">
          <Campo etiqueta="Imagen (dirección web)" value={f.imagenPrincipal} onChange={(e) => poner('imagenPrincipal', e.target.value)} maxLength={255} />
          <div className="fp-imagen__vista">{f.imagenPrincipal ? <img key={f.imagenPrincipal} src={f.imagenPrincipal} alt="" /> : <span>Sin imagen</span>}</div>
        </div>
      </div>
      {confirmarBorrado ? (
        <div className="aviso aviso--error fp-borrar">
          <span>¿Eliminar el producto {codigoActual}? No se puede deshacer.</span>
          <div className="formulario__acciones">
            <Boton chico onClick={() => setConfirmarBorrado(false)}>
              No
            </Boton>
            <Boton chico variante="peligro" disabled={eliminar.isPending} onClick={() => eliminar.mutate()}>
              {eliminar.isPending ? 'Eliminando…' : 'Sí, eliminar'}
            </Boton>
          </div>
        </div>
      ) : (
        <div className="formulario__acciones">
          {codigoActual && tiene(Permisos.ProductosEliminar) && (
            <Boton variante="peligro" className="fp-acciones__eliminar" onClick={() => setConfirmarBorrado(true)}>
              <Trash2 size={16} /> Eliminar
            </Boton>
          )}
          <Boton onClick={alTerminar}>Cancelar</Boton>
          <Boton type="submit" variante="primario" disabled={guardar.isPending}>
            {guardar.isPending ? 'Guardando…' : 'Guardar'}
          </Boton>
        </div>
      )}
    </form>
  )
}

function desdeProducto(p: Producto): Formulario {
  return {
    codigo: p.codigo,
    referenciaProveedor: p.referenciaProveedor ?? '',
    descripcion: p.descripcion.trim(),
    idRubro: p.idRubro,
    idProveedor: p.idProveedor,
    precioCompra: String(p.precioCompra ?? 0),
    descuento: String(p.descuento ?? 0),
    utilidad: String(p.utilidad ?? 0),
    precioVenta: String(p.precioVenta),
    precioOferta: p.precioOferta == null ? '' : String(p.precioOferta),
    // La API manda la hora local del servidor sin zona: alcanza con recortar a minutos.
    fechaInicioOferta: p.fechaInicioOferta?.slice(0, 16) ?? '',
    fechaFinOferta: p.fechaFinOferta?.slice(0, 16) ?? '',
    activo: p.activo,
    imagenPrincipal: p.imagenPrincipal ?? '',
  }
}

const numero = (texto: string) => {
  const valor = Number(texto || 0)
  return Number.isFinite(valor) ? valor : 0
}
const redondear = (valor: number, decimales: number) => String(Number(valor.toFixed(decimales)))
const calcularCosto = (f: Formulario) => Math.max(numero(f.precioCompra) - (numero(f.precioCompra) * numero(f.descuento)) / 100, 0)
