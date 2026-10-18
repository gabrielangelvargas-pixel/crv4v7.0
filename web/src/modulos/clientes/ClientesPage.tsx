import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, ShoppingCart, SlidersHorizontal, Trash2 } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { api } from '../../api/cliente'
import { Permisos } from '../../auth/permisos'
import { useSesion } from '../../auth/SesionContexto'
import { Aviso, Boton, Campo, Modal, Paginador, Selector, Tarjeta } from '../../ui/componentes'
// Comparte con Productos los filtros, el resumen y la grilla del formulario.
import '../productos/productos.css'
import './clientes.css'

export interface Cliente {
  id: number
  nombre: string
  direccion: string | null
  idZona: number
  zona: string
  idFiscal: number
  condicionFiscal: string
  numeroFiscal: string | null
  celular1: string | null
  celular2: string | null
  telefonoFijo: string | null
  saldoCuenta: number
  fechaAlta: string
}

interface Opcion {
  id: number
  nombre: string
}

interface Paginado<T> {
  items: T[]
  total: number
  pagina: number
  tamanoPagina: number
}

interface Formulario {
  id: string
  nombre: string
  direccion: string
  idZona: string
  idFiscal: string
  numeroFiscal: string
  celular1: string
  celular2: string
  telefonoFijo: string
  saldoCuenta: string
}

type Edicion = { modo: 'nuevo' } | { modo: 'editar'; id: number }

const TAMANO_PAGINA = 100
const vacio: Formulario = {
  id: '',
  nombre: '',
  direccion: '',
  idZona: '',
  idFiscal: '',
  numeroFiscal: '',
  celular1: '',
  celular2: '',
  telefonoFijo: '',
  saldoCuenta: '0',
}
const moneda = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })

export function ClientesPage() {
  const { tiene } = useSesion()
  const puedeCrear = tiene(Permisos.ClientesCrear)
  const puedeEditar = tiene(Permisos.ClientesEditar)
  const verVentas = tiene(Permisos.VentasVer)
  const navegar = useNavigate()
  const [buscar, setBuscar] = useState('')
  const [texto, setTexto] = useState('')
  const [idZona, setIdZona] = useState('')
  const [idFiscal, setIdFiscal] = useState('')
  const [conSaldo, setConSaldo] = useState(false)
  const [pagina, setPagina] = useState(1)
  const [edicion, setEdicion] = useState<Edicion | null>(null)
  const [verFiltros, setVerFiltros] = useState(false)

  // Espera a que se deje de escribir para no consultar con cada tecla.
  useEffect(() => {
    const espera = window.setTimeout(() => setTexto(buscar.trim()), 250)
    return () => window.clearTimeout(espera)
  }, [buscar])

  const zonas = useQuery({ queryKey: ['zonas'], queryFn: () => api.get<Opcion[]>('/api/zonas') })
  const fiscales = useQuery({ queryKey: ['condiciones-fiscales'], queryFn: () => api.get<Opcion[]>('/api/condiciones-fiscales') })
  const parametros = new URLSearchParams({ pagina: String(pagina), tamano: String(TAMANO_PAGINA) })
  if (texto) parametros.set('buscar', texto)
  if (idZona) parametros.set('idZona', idZona)
  if (idFiscal) parametros.set('idFiscal', idFiscal)
  if (conSaldo) parametros.set('conSaldo', 'true')
  const clientes = useQuery({
    queryKey: ['clientes', parametros.toString()],
    queryFn: () => api.get<Paginado<Cliente>>(`/api/clientes?${parametros}`),
    placeholderData: keepPreviousData,
  })

  const datos = clientes.data
  const paginas = datos ? Math.max(1, Math.ceil(datos.total / datos.tamanoPagina)) : 1
  const filtrosActivos = [idZona, idFiscal, conSaldo].filter(Boolean).length
  const filtrar =
    <T,>(cambiar: (valor: T) => void) =>
    (valor: T) => {
      cambiar(valor)
      setPagina(1)
    }

  return (
    <>
      <Tarjeta className="tarjeta--lista">
        <div className="filtros-productos">
          <input
            className="campo__control filtros-productos__busqueda"
            placeholder="Buscar por código, nombre, CUIT/DNI o teléfono…"
            aria-label="Buscar cliente"
            value={buscar}
            onChange={(e) => filtrar(setBuscar)(e.target.value)}
          />
          <Boton
            className="solo-movil filtros-productos__alternar"
            aria-expanded={verFiltros}
            aria-label="Filtros"
            onClick={() => setVerFiltros(!verFiltros)}
          >
            <SlidersHorizontal size={16} />
            {filtrosActivos > 0 && <span className="filtros-productos__cantidad">{filtrosActivos}</span>}
          </Boton>
          <div className={`filtros-productos__extra${verFiltros ? ' filtros-productos__extra--abierto' : ''}`}>
            <Selector etiqueta="Zona" value={idZona} onChange={(e) => filtrar(setIdZona)(e.target.value)}>
              <option value="">Todas las zonas</option>
              {(zonas.data ?? []).map((z) => (
                <option key={z.id} value={z.id}>
                  {z.nombre}
                </option>
              ))}
            </Selector>
            <Selector etiqueta="Condición fiscal" value={idFiscal} onChange={(e) => filtrar(setIdFiscal)(e.target.value)}>
              <option value="">Todas</option>
              {(fiscales.data ?? []).map((f) => (
                <option key={f.id} value={f.id}>
                  {f.nombre}
                </option>
              ))}
            </Selector>
            <Selector etiqueta="Saldo" value={conSaldo ? 'si' : ''} onChange={(e) => filtrar(setConSaldo)(e.target.value === 'si')}>
              <option value="">Todos</option>
              <option value="si">Con saldo</option>
            </Selector>
          </div>
          {puedeCrear && (
            <Boton
              variante="primario"
              className="filtros-productos__nuevo"
              aria-label="Nuevo cliente"
              onClick={() => setEdicion({ modo: 'nuevo' })}
            >
              <Plus size={16} /> <span className="solo-ancho">Nuevo cliente</span>
            </Boton>
          )}
        </div>
        <Aviso>{clientes.error?.message}</Aviso>
        {datos && (
          <div className="productos-resumen">
            <span>
              Clientes encontrados: <strong>{datos.total.toLocaleString('es-AR')}</strong>
            </span>
            {paginas > 1 && (
              <div className="solo-ancho">
                <Paginador pagina={datos.pagina} paginas={paginas} cambiar={setPagina} />
              </div>
            )}
          </div>
        )}
        <div className="tabla-contenedor zona-desplazable">
          <table className="tabla tabla-clientes">
            <thead>
              <tr>
                <th>Código</th>
                <th>Cliente</th>
                <th>Zona</th>
                <th>Condición fiscal</th>
                <th>Teléfono</th>
                <th className="numero">Saldo</th>
                {(puedeEditar || verVentas) && <th />}
              </tr>
            </thead>
            <tbody>
              {datos?.items.map((c) => {
                const telefono = c.celular1 ?? c.celular2 ?? c.telefonoFijo
                return (
                  <tr key={c.id}>
                    <td className="tabla-clientes__codigo">{c.id}</td>
                    <td className="tabla-clientes__cliente">
                      <div className="tabla-clientes__nombre">{c.nombre.trim()}</div>
                      <div className="tabla-clientes__detalle">
                        {c.direccion?.trim() && <span title="Dirección">{c.direccion.trim()}</span>}
                        <span className="solo-movil" title="Condición fiscal">
                          {c.condicionFiscal}
                        </span>
                        {c.numeroFiscal?.trim() && <span title="CUIT/DNI">{c.numeroFiscal.trim()}</span>}
                      </div>
                    </td>
                    <td className="tabla-clientes__zona">{c.zona}</td>
                    <td className="tabla-clientes__fiscal">{c.condicionFiscal}</td>
                    <td className={`tabla-clientes__telefono${telefono ? '' : ' tabla-clientes__telefono--sin'}`}>{telefono ?? '-'}</td>
                    <td className={`numero tabla-clientes__saldo${c.saldoCuenta > 0 ? ' tabla-clientes__saldo--debe' : ''}`}>
                      {moneda.format(c.saldoCuenta)}
                    </td>
                    {(puedeEditar || verVentas) && (
                      <td className="acciones">
                        {verVentas && (
                          <Boton
                            chico
                            onClick={() => navegar(`/ventas?cliente=${c.id}`)}
                            aria-label={`Ver las ventas de ${c.nombre}`}
                            title="Ver ventas"
                          >
                            <ShoppingCart size={14} />
                          </Boton>
                        )}
                        {puedeEditar && (
                          <Boton
                            chico
                            onClick={() => setEdicion({ modo: 'editar', id: c.id })}
                            aria-label={`Editar ${c.nombre}`}
                            title="Editar"
                          >
                            <Pencil size={14} /> <span className="solo-ancho">Editar</span>
                          </Boton>
                        )}
                      </td>
                    )}
                  </tr>
                )
              })}
            </tbody>
          </table>
          {clientes.isPending && <div className="vacio">Cargando…</div>}
          {datos && datos.items.length === 0 && <div className="vacio">No hay clientes con esos filtros.</div>}
        </div>
        {datos && paginas > 1 && (
          <div className="productos-resumen productos-resumen--pie">
            <Paginador pagina={datos.pagina} paginas={paginas} cambiar={setPagina} />
          </div>
        )}
      </Tarjeta>

      <Modal
        titulo={edicion?.modo === 'editar' ? 'Editar cliente' : 'Nuevo cliente'}
        abierto={!!edicion}
        alCerrar={() => setEdicion(null)}
        grande
      >
        {edicion && (
          <EditorCliente edicion={edicion} zonas={zonas.data ?? []} fiscales={fiscales.data ?? []} alTerminar={() => setEdicion(null)} />
        )}
      </Modal>
    </>
  )
}

function EditorCliente(props: { edicion: Edicion; zonas: Opcion[]; fiscales: Opcion[]; alTerminar: () => void }) {
  const { edicion } = props
  const id = edicion.modo === 'editar' ? edicion.id : null
  const detalle = useQuery({
    queryKey: ['cliente', id],
    queryFn: () => api.get<Cliente>(`/api/clientes/${id}`),
    enabled: id != null,
    gcTime: 0,
  })
  if (id == null) return <FormularioCliente {...props} inicial={vacio} />
  if (detalle.error) return <Aviso>{detalle.error.message}</Aviso>
  if (!detalle.data) return <div className="vacio">Cargando cliente…</div>
  return <FormularioCliente {...props} inicial={desdeCliente(detalle.data)} idActual={detalle.data.id} />
}

function FormularioCliente({
  inicial,
  idActual,
  zonas,
  fiscales,
  alTerminar,
}: {
  inicial: Formulario
  idActual?: number
  zonas: Opcion[]
  fiscales: Opcion[]
  alTerminar: () => void
}) {
  const { tiene } = useSesion()
  const cliente = useQueryClient()
  const [f, setF] = useState(inicial)
  const [confirmarBorrado, setConfirmarBorrado] = useState(false)
  const poner = (campo: keyof Formulario, valor: string) => setF((actual) => ({ ...actual, [campo]: valor }))

  const refrescar = async () => {
    await cliente.invalidateQueries({ queryKey: ['clientes'] })
    await cliente.invalidateQueries({ queryKey: ['inicio'] })
  }
  const guardar = useMutation({
    mutationFn: () => {
      const cuerpo = {
        id: f.id.trim() ? Number(f.id) : null,
        nombre: f.nombre.trim(),
        direccion: f.direccion.trim() || null,
        idZona: Number(f.idZona),
        idFiscal: Number(f.idFiscal),
        numeroFiscal: f.numeroFiscal.trim() || null,
        celular1: f.celular1.trim() || null,
        celular2: f.celular2.trim() || null,
        telefonoFijo: f.telefonoFijo.trim() || null,
        saldoCuenta: Number(f.saldoCuenta || 0),
      }
      return idActual ? api.put(`/api/clientes/${idActual}`, cuerpo) : api.post('/api/clientes', cuerpo)
    },
    onSuccess: async () => {
      await refrescar()
      alTerminar()
    },
  })
  const eliminar = useMutation({
    mutationFn: () => api.delete(`/api/clientes/${idActual}`),
    onSuccess: async () => {
      await refrescar()
      alTerminar()
    },
  })

  function enviar(e: FormEvent) {
    e.preventDefault()
    guardar.mutate()
  }

  return (
    <form className="formulario" onSubmit={enviar}>
      <Aviso>{guardar.error?.message ?? eliminar.error?.message}</Aviso>
      <div className="formulario-producto">
        <div className="fp-2">
          <Campo
            etiqueta="Código"
            type="number"
            min="1"
            step="1"
            value={f.id}
            onChange={(e) => poner('id', e.target.value)}
            placeholder="Se asigna al guardar"
          />
        </div>
        <div className="fp-todo">
          <Campo etiqueta="Nombre" value={f.nombre} onChange={(e) => poner('nombre', e.target.value)} maxLength={150} required autoFocus />
        </div>
        <div className="fp-todo">
          <Campo etiqueta="Dirección" value={f.direccion} onChange={(e) => poner('direccion', e.target.value)} maxLength={150} />
        </div>
        <div className="fp-3">
          <Selector etiqueta="Zona" value={f.idZona} onChange={(e) => poner('idZona', e.target.value)} required>
            <option value="">Elegí la zona</option>
            {zonas.map((z) => (
              <option key={z.id} value={z.id}>
                {z.nombre}
              </option>
            ))}
          </Selector>
        </div>
        <div className="fp-3">
          <Selector etiqueta="Condición fiscal" value={f.idFiscal} onChange={(e) => poner('idFiscal', e.target.value)} required>
            <option value="">Elegí la condición</option>
            {fiscales.map((x) => (
              <option key={x.id} value={x.id}>
                {x.nombre}
              </option>
            ))}
          </Selector>
        </div>
        <div className="fp-2">
          <Campo etiqueta="CUIT/DNI" value={f.numeroFiscal} onChange={(e) => poner('numeroFiscal', e.target.value)} maxLength={13} />
        </div>
        <div className="fp-2">
          <Campo etiqueta="Celular 1" type="tel" value={f.celular1} onChange={(e) => poner('celular1', e.target.value)} maxLength={10} />
        </div>
        <div className="fp-2">
          <Campo etiqueta="Celular 2" type="tel" value={f.celular2} onChange={(e) => poner('celular2', e.target.value)} maxLength={10} />
        </div>
        <div className="fp-2">
          <Campo
            etiqueta="Teléfono fijo"
            type="tel"
            value={f.telefonoFijo}
            onChange={(e) => poner('telefonoFijo', e.target.value)}
            maxLength={15}
          />
        </div>
        <div className="fp-2">
          <Campo
            etiqueta="Saldo de cuenta"
            type="number"
            min="0"
            step="0.01"
            value={f.saldoCuenta}
            onChange={(e) => poner('saldoCuenta', e.target.value)}
          />
        </div>
      </div>
      {confirmarBorrado ? (
        <div className="aviso aviso--error fp-borrar">
          <span>¿Eliminar el cliente {idActual}? No se puede deshacer.</span>
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
          {idActual && tiene(Permisos.ClientesEliminar) && (
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

function desdeCliente(c: Cliente): Formulario {
  return {
    id: String(c.id),
    nombre: c.nombre.trim(),
    direccion: c.direccion ?? '',
    idZona: String(c.idZona),
    idFiscal: String(c.idFiscal),
    numeroFiscal: c.numeroFiscal ?? '',
    celular1: c.celular1 ?? '',
    celular2: c.celular2 ?? '',
    telefonoFijo: c.telefonoFijo ?? '',
    saldoCuenta: String(c.saldoCuenta),
  }
}
