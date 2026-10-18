import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { DollarSign, Eye, Pencil, Plus, Printer, SlidersHorizontal, Trash2, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
import { api } from '../../api/cliente'
import { Permisos } from '../../auth/permisos'
import { useSesion } from '../../auth/SesionContexto'
import { Aviso, Boton, Modal, Paginador, Selector, Tarjeta } from '../../ui/componentes'
import type { Cliente } from '../clientes/ClientesPage'
import '../productos/productos.css'
import { EditorVenta } from './EditorVenta'
import { imprimirVenta } from './imprimir'
import { PagoVenta } from './PagoVenta'
import {
  borrador,
  Estados,
  fechaHora,
  formularioDesdeVenta,
  MESES,
  moneda,
  type FormularioVenta,
  type Opcion,
  type Paginado,
  type Venta,
  type VentaResumen,
} from './tipos'
import './ventas.css'

const TAMANO_PAGINA = 100

type Edicion = { idVenta: number | null; formulario: FormularioVenta; recuperado?: boolean }

export function VentasPage() {
  const { tiene } = useSesion()
  const consultas = useQueryClient()
  const puedeCrear = tiene(Permisos.VentasCrear)
  const puedeEditar = tiene(Permisos.VentasEditar)
  const puedeAnular = tiene(Permisos.VentasAnular)
  const [parametrosUrl, setParametrosUrl] = useSearchParams()
  const idCliente = Number(parametrosUrl.get('cliente')) || null
  const [hoy] = useState(() => new Date())
  const [buscar, setBuscar] = useState('')
  const [texto, setTexto] = useState('')
  const [mes, setMes] = useState(hoy.getMonth() + 1)
  const [anio, setAnio] = useState(hoy.getFullYear())
  const [pagina, setPagina] = useState(1)
  const [verFiltros, setVerFiltros] = useState(false)
  const [edicion, setEdicion] = useState<Edicion | null>(null)
  const [detalle, setDetalle] = useState<number | null>(null)
  const [cobro, setCobro] = useState<VentaResumen | null>(null)
  const [baja, setBaja] = useState<VentaResumen | null>(null)
  const [aImprimir, setAImprimir] = useState<Venta | null>(null)
  const [mensaje, setMensaje] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const espera = window.setTimeout(() => setTexto(buscar.trim()), 250)
    return () => window.clearTimeout(espera)
  }, [buscar])

  const vendedores = useQuery({ queryKey: ['vendedores'], queryFn: () => api.get<Opcion[]>('/api/vendedores') })
  const estados = useQuery({ queryKey: ['estados-venta'], queryFn: () => api.get<Opcion[]>('/api/estados-venta') })
  const cliente = useQuery({
    queryKey: ['cliente', idCliente],
    queryFn: () => api.get<Cliente>(`/api/clientes/${idCliente}`),
    enabled: idCliente != null && tiene(Permisos.ClientesVer),
  })
  const parametros = new URLSearchParams({ pagina: String(pagina), tamano: String(TAMANO_PAGINA) })
  if (idCliente) parametros.set('idCliente', String(idCliente))
  else {
    parametros.set('mes', String(mes))
    parametros.set('anio', String(anio))
  }
  if (texto) parametros.set('buscar', texto)
  const ventas = useQuery({
    queryKey: ['ventas', parametros.toString()],
    queryFn: () => api.get<Paginado<VentaResumen>>(`/api/ventas?${parametros}`),
    placeholderData: keepPreviousData,
    // Como en Sistema03: se actualiza sola si otra PC carga o cobra, salvo mientras se trabaja en una venta.
    refetchInterval: edicion || detalle || cobro ? false : 10_000,
  })

  const eliminar = useMutation({
    mutationFn: (v: VentaResumen) => api.delete<{ accion: string; mensaje: string }>(`/api/ventas/${v.id}`),
    onSuccess: async (r) => {
      setBaja(null)
      setMensaje(r.mensaje)
      await consultas.invalidateQueries({ queryKey: ['ventas'] })
      await consultas.invalidateQueries({ queryKey: ['clientes'] })
    },
  })

  const datos = ventas.data
  const paginas = datos ? Math.max(1, Math.ceil(datos.total / datos.tamanoPagina)) : 1
  const anios = Array.from(
    { length: Math.max(anio, hoy.getFullYear()) + 1 - (Math.min(anio, hoy.getFullYear()) - 3) + 1 },
    (_, i) => Math.max(anio, hoy.getFullYear()) + 1 - i,
  )
  const filtrar =
    <T,>(cambiar: (valor: T) => void) =>
    (valor: T) => {
      cambiar(valor)
      setPagina(1)
    }

  function nueva() {
    setMensaje(null)
    const guardado = borrador.leer()
    if (guardado) {
      setEdicion({ idVenta: guardado.idVenta, formulario: guardado.formulario, recuperado: true })
      return
    }
    const pendiente = estados.data?.find((e) => e.id === Estados.Pendiente) ?? estados.data?.[0]
    setEdicion({
      idVenta: null,
      formulario: {
        idCliente: idCliente ? String(idCliente) : '',
        cliente: idCliente ? `${idCliente}${cliente.data ? ` - ${cliente.data.nombre.trim()}` : ''}` : '',
        idVendedor: vendedores.data?.[0] ? String(vendedores.data[0].id) : '',
        idEstado: pendiente ? String(pendiente.id) : '',
        descuentoBono: '0',
        usarPrecioOfertaSinVigencia: false,
        items: [],
      },
    })
  }

  async function editar(id: number) {
    setMensaje(null)
    setError(null)
    try {
      const venta = await api.get<Venta>(`/api/ventas/${id}`)
      setDetalle(null)
      setEdicion({ idVenta: venta.id, formulario: formularioDesdeVenta(venta) })
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo abrir la venta.')
    }
  }

  if (edicion) {
    return (
      <Tarjeta className="tarjeta--lista">
        <EditorVenta
          idVenta={edicion.idVenta}
          inicial={edicion.formulario}
          recuperado={edicion.recuperado}
          vendedores={vendedores.data ?? []}
          estados={estados.data ?? []}
          alSalir={(guardada) => {
            setEdicion(null)
            if (guardada) {
              setMensaje(`Venta ${guardada.id} guardada.`)
              setAImprimir(guardada)
            }
          }}
        />
      </Tarjeta>
    )
  }

  return (
    <>
      <Tarjeta className="tarjeta--lista">
        {idCliente && (
          <div className="ventas-cliente">
            <div>
              <span>Ventas del cliente</span>
              <strong>
                {idCliente}
                {cliente.data && ` - ${cliente.data.nombre.trim()}`}
              </strong>
            </div>
            <Boton chico onClick={() => setParametrosUrl({})}>
              <X size={14} /> Ver todas
            </Boton>
          </div>
        )}
        <div className={`filtros-productos filtros-ventas${idCliente ? ' filtros-ventas--cliente' : ''}`}>
          <input
            className="campo__control filtros-productos__busqueda"
            placeholder={idCliente ? 'Buscar por número de venta…' : 'Buscar por número o nombre del cliente…'}
            aria-label="Buscar venta"
            value={buscar}
            onChange={(e) => filtrar(setBuscar)(e.target.value)}
          />
          {!idCliente && (
            <>
              <Boton
                className="solo-movil filtros-productos__alternar"
                aria-expanded={verFiltros}
                aria-label="Filtros"
                onClick={() => setVerFiltros(!verFiltros)}
              >
                <SlidersHorizontal size={16} />
              </Boton>
              <div className={`filtros-productos__extra${verFiltros ? ' filtros-productos__extra--abierto' : ''}`}>
                <Selector etiqueta="Mes" value={mes} onChange={(e) => filtrar(setMes)(Number(e.target.value))}>
                  {MESES.map((nombre, i) => (
                    <option key={nombre} value={i + 1}>
                      {nombre}
                    </option>
                  ))}
                </Selector>
                <Selector etiqueta="Año" value={anio} onChange={(e) => filtrar(setAnio)(Number(e.target.value))}>
                  {anios.map((a) => (
                    <option key={a} value={a}>
                      {a}
                    </option>
                  ))}
                </Selector>
              </div>
            </>
          )}
          {puedeCrear && (
            <Boton variante="primario" className="filtros-productos__nuevo" aria-label="Nueva venta" onClick={nueva}>
              <Plus size={16} /> <span className="solo-ancho">Nueva venta</span>
            </Boton>
          )}
        </div>
        <Aviso tipo="exito">{mensaje}</Aviso>
        <Aviso>{error ?? ventas.error?.message}</Aviso>
        {datos && (
          <div className="productos-resumen">
            <span>
              {idCliente ? 'Ventas del cliente' : `Ventas de ${MESES[mes - 1].toLowerCase()} ${anio}`}:{' '}
              <strong>{datos.total.toLocaleString('es-AR')}</strong>
            </span>
            {paginas > 1 && (
              <div className="solo-ancho">
                <Paginador pagina={datos.pagina} paginas={paginas} cambiar={setPagina} />
              </div>
            )}
          </div>
        )}
        <div className="tabla-contenedor zona-desplazable">
          <table className="tabla tabla-ventas">
            <thead>
              <tr>
                <th>Número</th>
                <th>Cliente</th>
                <th>Vendedor</th>
                <th>Estado</th>
                <th className="numero">Total</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {datos?.items.map((v) => {
                const cobrable = v.idEstado !== Estados.Pagada && v.idEstado !== Estados.Anulada
                return (
                  <tr key={v.id} className={v.idEstado === Estados.Anulada ? 'tabla-ventas__anulada' : undefined}>
                    <td className="tabla-ventas__numero">
                      <strong>{v.id}</strong>
                      <span>{fechaHora(v.fecha)}</span>
                    </td>
                    <td className="tabla-ventas__cliente">
                      <strong>{v.cliente.trim()}</strong>
                      <span>{v.idCliente}</span>
                    </td>
                    <td className="tabla-ventas__vendedor">{v.vendedor}</td>
                    <td className="tabla-ventas__estado">
                      <span className={`etiqueta ${claseEstado(v.idEstado)}`}>{v.estado}</span>
                    </td>
                    <td className="numero tabla-ventas__total">
                      {moneda.format(v.total)}
                      {v.pagado > 0 && v.idEstado === Estados.Parcial && <span>Pagado {moneda.format(v.pagado)}</span>}
                    </td>
                    <td className="acciones tabla-ventas__acciones">
                      {puedeEditar && (
                        <Boton
                          chico
                          onClick={() => setCobro(v)}
                          disabled={!cobrable}
                          aria-label={`Cobrar la venta ${v.id}`}
                          title="Registrar pago"
                        >
                          <DollarSign size={14} />
                        </Boton>
                      )}
                      {puedeEditar && (
                        <Boton chico onClick={() => void editar(v.id)} aria-label={`Editar la venta ${v.id}`} title="Editar">
                          <Pencil size={14} />
                        </Boton>
                      )}
                      <Boton chico onClick={() => setDetalle(v.id)} aria-label={`Ver la venta ${v.id}`} title="Ver">
                        <Eye size={14} />
                      </Boton>
                      {puedeAnular && (
                        <Boton
                          chico
                          variante="peligro"
                          onClick={() => setBaja(v)}
                          disabled={v.idEstado === Estados.Anulada}
                          aria-label={`Eliminar o anular la venta ${v.id}`}
                          title="Eliminar o anular"
                        >
                          <Trash2 size={14} />
                        </Boton>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
          {ventas.isPending && <div className="vacio">Cargando…</div>}
          {datos && datos.items.length === 0 && <div className="vacio">No hay ventas para mostrar.</div>}
        </div>
        {datos && paginas > 1 && (
          <div className="productos-resumen productos-resumen--pie">
            <Paginador pagina={datos.pagina} paginas={paginas} cambiar={setPagina} />
          </div>
        )}
      </Tarjeta>

      <Modal titulo={detalle ? `Venta ${detalle}` : 'Venta'} abierto={detalle != null} alCerrar={() => setDetalle(null)} grande>
        {detalle != null && <DetalleVenta id={detalle} puedeEditar={puedeEditar} editar={() => void editar(detalle)} />}
      </Modal>

      <Modal
        titulo={cobro ? `Registrar pago · Venta ${cobro.id}` : 'Registrar pago'}
        abierto={!!cobro}
        alCerrar={() => setCobro(null)}
        grande
      >
        {cobro && (
          <PagoVenta
            venta={cobro}
            alTerminar={(pago) => {
              setCobro(null)
              if (pago) setMensaje(`Pago ${pago.id} registrado. La venta ${pago.idFactura} quedó ${pago.estadoVenta.toLowerCase()}.`)
            }}
          />
        )}
      </Modal>

      <Modal titulo={baja ? `Eliminar venta ${baja.id}` : 'Eliminar venta'} abierto={!!baja} alCerrar={() => setBaja(null)}>
        {baja && (
          <div className="formulario">
            <p>Si la venta {baja.id} es la última, se elimina. Si no, se anula y queda con total $ 0 para no perder el número.</p>
            <Aviso>{eliminar.error?.message}</Aviso>
            <div className="formulario__acciones">
              <Boton onClick={() => setBaja(null)}>No</Boton>
              <Boton variante="peligro" disabled={eliminar.isPending} onClick={() => eliminar.mutate(baja)}>
                {eliminar.isPending ? 'Procesando…' : 'Sí, continuar'}
              </Boton>
            </div>
          </div>
        )}
      </Modal>

      <Modal titulo="Venta guardada" abierto={!!aImprimir} alCerrar={() => setAImprimir(null)}>
        {aImprimir && (
          <div className="formulario">
            <p>¿Querés imprimir la venta {aImprimir.id}?</p>
            <div className="formulario__acciones">
              <Boton onClick={() => setAImprimir(null)}>No imprimir</Boton>
              <Boton
                variante="primario"
                onClick={() => {
                  imprimirVenta(aImprimir)
                  setAImprimir(null)
                }}
              >
                <Printer size={16} /> Imprimir
              </Boton>
            </div>
          </div>
        )}
      </Modal>
    </>
  )
}

function claseEstado(idEstado: number) {
  if (idEstado === Estados.Pagada) return 'etiqueta--activo'
  if (idEstado === Estados.Anulada) return 'etiqueta--inactivo'
  return 'etiqueta--aviso'
}

function DetalleVenta({ id, puedeEditar, editar }: { id: number; puedeEditar: boolean; editar: () => void }) {
  const venta = useQuery({ queryKey: ['venta', id], queryFn: () => api.get<Venta>(`/api/ventas/${id}`), gcTime: 0 })
  if (venta.error) return <Aviso>{venta.error.message}</Aviso>
  const v = venta.data
  if (!v) return <div className="vacio">Cargando venta…</div>
  return (
    <div className="detalle-venta">
      <div className="detalle-venta__cabecera">
        <div>
          <strong>{v.cliente.trim()}</strong>
          <span>
            Cliente {v.idCliente} · {fechaHora(v.fecha)}
          </span>
        </div>
        <div>
          <span className={`etiqueta ${claseEstado(v.idEstado)}`}>{v.estado}</span>
          <span>{v.vendedor}</span>
        </div>
      </div>
      <div className="detalle-venta__renglones">
        <table className="tabla tabla-detalle">
          <thead>
            <tr>
              <th>Producto</th>
              <th className="numero">Cant.</th>
              <th className="numero">Precio</th>
              <th className="numero">Subtotal</th>
            </tr>
          </thead>
          <tbody>
            {v.items.map((i) => (
              <tr key={i.id}>
                <td>
                  <div className="tabla-detalle__descripcion">{i.descripcion}</div>
                  <div className="tabla-detalle__codigo">{i.idProducto}</div>
                </td>
                <td className="numero">{i.cantidad}</td>
                <td className="numero">{moneda.format(i.precioVenta)}</td>
                <td className="numero">{moneda.format(i.subTotalVenta)}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {v.items.length === 0 && <div className="vacio">La venta no tiene productos.</div>}
      </div>
      <dl className="detalle-venta__totales">
        {v.descuentoProveedores > 0 && (
          <>
            <dt>Descuento por escalones</dt>
            <dd>-{moneda.format(v.descuentoProveedores)}</dd>
          </>
        )}
        {v.descuentoBono > 0 && (
          <>
            <dt>Descuento bono</dt>
            <dd>-{moneda.format(v.descuentoBono)}</dd>
          </>
        )}
        {v.pagado > 0 && (
          <>
            <dt>Pagado</dt>
            <dd>{moneda.format(v.pagado)}</dd>
          </>
        )}
        {v.pagado > 0 && v.totalVenta - v.pagado >= 0.01 && (
          <>
            <dt>Saldo</dt>
            <dd>{moneda.format(v.totalVenta - v.pagado)}</dd>
          </>
        )}
        <dt className="detalle-venta__total">Total</dt>
        <dd className="detalle-venta__total">{moneda.format(v.totalVenta)}</dd>
      </dl>
      <div className="formulario__acciones">
        {puedeEditar && (
          <Boton onClick={editar}>
            <Pencil size={16} /> Editar
          </Boton>
        )}
        <Boton variante="primario" onClick={() => imprimirVenta(v)}>
          <Printer size={16} /> Imprimir
        </Boton>
      </div>
    </div>
  )
}
