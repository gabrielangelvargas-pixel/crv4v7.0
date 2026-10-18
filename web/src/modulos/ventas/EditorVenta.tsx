import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Grid3x3, Plus, Save, Search, Trash2, X } from 'lucide-react'
import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { api } from '../../api/cliente'
import { Permisos } from '../../auth/permisos'
import { useSesion } from '../../auth/SesionContexto'
import { Aviso, Boton, Casilla, Modal, Selector } from '../../ui/componentes'
import type { Cliente } from '../clientes/ClientesPage'
import { esTactil, useModoTeclado } from './modoTeclado'
import { TecladoCodigo } from './TecladoCodigo'
import {
  borrador,
  calcularTotales,
  completa,
  cuerpoVenta,
  itemDesdeProducto,
  moneda,
  numero,
  sonidoNoEncontrado,
  type FormularioVenta,
  type ItemFormulario,
  type Opcion,
  type Paginado,
  type ProductoVenta,
  type Venta,
} from './tipos'

type EstadoAutoguardado = 'quieto' | 'guardando' | 'guardado' | 'error'

/**
 * Carga de una venta, como en Sistema03: se escanea o escribe el código y Enter (0 abre el buscador).
 * Cuando la venta está completa se autoguarda sola; además queda un respaldo en el navegador.
 */
export function EditorVenta({
  idVenta: idInicial,
  inicial,
  recuperado,
  vendedores,
  estados,
  alSalir,
}: {
  idVenta: number | null
  inicial: FormularioVenta
  recuperado?: boolean
  vendedores: Opcion[]
  estados: Opcion[]
  alSalir: (guardada?: Venta) => void
}) {
  const { tiene } = useSesion()
  const consultas = useQueryClient()
  const [f, setF] = useState(inicial)
  const [idVenta, setIdVenta] = useState(idInicial)
  const [codigo, setCodigo] = useState('')
  const [avisoProducto, setAvisoProducto] = useState<string | null>(null)
  const [agregando, setAgregando] = useState(false)
  const [buscador, setBuscador] = useState(false)
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [autoguardado, setAutoguardado] = useState<EstadoAutoguardado>('quieto')
  const refCodigo = useRef<HTMLInputElement>(null)
  const teclado = useModoTeclado()
  const [tecladoAbierto, setTecladoAbierto] = useState(false)
  const refRenglones = useRef<HTMLDivElement>(null)
  const refId = useRef(idInicial)
  const ultimoGuardado = useRef<string | null>(idInicial ? JSON.stringify(cuerpoVenta(inicial)) : null)
  const enCurso = useRef(false)
  const pendiente = useRef<ReturnType<typeof cuerpoVenta> | null>(null)
  const activo = useRef(true)
  // Autoguardar una venta nueva hace un alta y después ediciones: hacen falta los dos permisos.
  const puedeAutoguardar = tiene(Permisos.VentasEditar) && (idInicial != null || tiene(Permisos.VentasCrear))
  const totales = calcularTotales(f)

  useEffect(() => {
    activo.current = true
    // En celular no se enfoca solo: abriría el teclado y taparía la venta.
    if (!esTactil()) refCodigo.current?.focus()
    return () => {
      activo.current = false
    }
  }, [])

  useEffect(() => {
    if (f.idCliente || f.items.length > 0) borrador.guardar({ idVenta, formulario: f })
  }, [f, idVenta])

  const autoguardar = useCallback(
    async (cuerpo: ReturnType<typeof cuerpoVenta>) => {
      if (enCurso.current) {
        pendiente.current = cuerpo
        return
      }
      enCurso.current = true
      let actual: typeof cuerpo | null = cuerpo
      while (actual && activo.current) {
        pendiente.current = null
        setAutoguardado('guardando')
        try {
          const id = refId.current
          const venta = id ? await api.put<Venta>(`/api/ventas/${id}`, actual) : await api.post<Venta>('/api/ventas', actual)
          refId.current = venta.id
          ultimoGuardado.current = JSON.stringify(actual)
          if (!activo.current) break
          setIdVenta(venta.id)
          setAutoguardado('guardado')
          void consultas.invalidateQueries({ queryKey: ['ventas'] })
        } catch {
          if (activo.current) setAutoguardado('error')
          break
        }
        actual = pendiente.current
      }
      enCurso.current = false
    },
    [consultas],
  )

  useEffect(() => {
    if (!puedeAutoguardar || guardando || !completa(f)) return
    const cuerpo = cuerpoVenta(f)
    if (JSON.stringify(cuerpo) === ultimoGuardado.current) return
    const espera = window.setTimeout(() => void autoguardar(cuerpo), 850)
    return () => window.clearTimeout(espera)
  }, [f, guardando, puedeAutoguardar, autoguardar])

  const poner = <K extends keyof FormularioVenta>(campo: K, valor: FormularioVenta[K]) => setF((a) => ({ ...a, [campo]: valor }))

  function agregar(p: ProductoVenta) {
    setF((a) => {
      const nuevo = itemDesdeProducto(p, a.usarPrecioOfertaSinVigencia)
      const existente = a.items.find((i) => i.idProducto === p.codigo)
      const items = existente
        ? a.items.map((i) =>
            i === existente
              ? {
                  ...i,
                  cantidad: String(numero(i.cantidad) + 1),
                  // Con "oferta sin validar fecha" se fuerza el precio de oferta también en lo ya cargado.
                  ...(a.usarPrecioOfertaSinVigencia ? { precioVenta: nuevo.precioVenta, descripcion: nuevo.descripcion } : {}),
                }
              : i,
          )
        : [...a.items, nuevo]
      return { ...a, items }
    })
    setAvisoProducto(null)
    // Espera a que se dibuje el renglón nuevo para mostrarlo.
    window.requestAnimationFrame(() => {
      if (refRenglones.current) refRenglones.current.scrollTop = refRenglones.current.scrollHeight
    })
  }

  async function agregarPorCodigo() {
    const texto = codigo.trim()
    if (agregando) return
    if (texto === '0') {
      setCodigo('')
      setBuscador(true)
      return
    }
    if (!texto) {
      setAvisoProducto('Ingresá un código de producto.')
      return
    }
    setAgregando(true)
    try {
      agregar(await api.get<ProductoVenta>(`/api/productos/${encodeURIComponent(texto)}`))
      setCodigo('')
      refCodigo.current?.focus()
    } catch (e) {
      sonidoNoEncontrado()
      setAvisoProducto(e instanceof Error && !/no existe/i.test(e.message) ? e.message : `No existe el producto ${texto}.`)
      window.requestAnimationFrame(() => refCodigo.current?.select())
    } finally {
      setAgregando(false)
    }
  }

  const cambiarItem = (codigoItem: string, campo: 'cantidad' | 'precioVenta', valor: string) =>
    setF((a) => ({ ...a, items: a.items.map((i) => (i.idProducto === codigoItem ? { ...i, [campo]: valor } : i)) }))
  const quitarItem = (codigoItem: string) => setF((a) => ({ ...a, items: a.items.filter((i) => i.idProducto !== codigoItem) }))

  async function guardar(e: FormEvent) {
    e.preventDefault()
    if (!completa(f)) {
      setError('Elegí el cliente, el vendedor y el estado, y cargá al menos un producto.')
      return
    }
    setGuardando(true)
    setError(null)
    try {
      const cuerpo = cuerpoVenta(f)
      const id = refId.current
      const venta = id ? await api.put<Venta>(`/api/ventas/${id}`, cuerpo) : await api.post<Venta>('/api/ventas', cuerpo)
      borrador.borrar()
      activo.current = false
      await consultas.invalidateQueries({ queryKey: ['ventas'] })
      await consultas.invalidateQueries({ queryKey: ['clientes'] })
      alSalir(venta)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo guardar la venta.')
    } finally {
      setGuardando(false)
    }
  }

  function salir() {
    borrador.borrar()
    activo.current = false
    void consultas.invalidateQueries({ queryKey: ['clientes'] })
    alSalir()
  }

  return (
    <form className="editor-venta" onSubmit={guardar}>
      <div className="editor-venta__datos">
        <BuscadorCliente
          valor={f.cliente}
          elegido={!!f.idCliente}
          alEscribir={(texto) => setF((a) => ({ ...a, cliente: texto, idCliente: '' }))}
          alElegir={(c) => setF((a) => ({ ...a, idCliente: String(c.id), cliente: `${c.id} - ${c.nombre.trim()}` }))}
        />
        <Selector etiqueta="Vendedor" value={f.idVendedor} onChange={(e) => poner('idVendedor', e.target.value)} required>
          <option value="">Elegí</option>
          {vendedores.map((v) => (
            <option key={v.id} value={v.id}>
              {v.nombre}
            </option>
          ))}
        </Selector>
        <Selector etiqueta="Estado" value={f.idEstado} onChange={(e) => poner('idEstado', e.target.value)} required>
          <option value="">Elegí</option>
          {estados.map((x) => (
            <option key={x.id} value={x.id}>
              {x.nombre}
            </option>
          ))}
        </Selector>
        <div className="editor-venta__oferta">
          <Casilla
            etiqueta="Usar oferta sin validar fecha"
            checked={f.usarPrecioOfertaSinVigencia}
            onChange={(e) => poner('usarPrecioOfertaSinVigencia', e.target.checked)}
          />
        </div>
      </div>

      <div className="editor-venta__cuerpo">
        <div className="editor-venta__renglones zona-desplazable" ref={refRenglones}>
          <table className="tabla tabla-renglones">
            <thead>
              <tr>
                <th>Producto</th>
                <th className="numero">Cant.</th>
                <th className="numero">Precio</th>
                <th className="numero">Subtotal</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {f.items.map((i) => (
                <Renglon key={i.idProducto} item={i} cambiar={cambiarItem} quitar={quitarItem} />
              ))}
            </tbody>
          </table>
          {f.items.length === 0 && <div className="vacio">Escaneá o escribí el código de un producto para empezar.</div>}
        </div>

        <aside className="editor-venta__resumen" aria-label="Resumen de la venta">
          <div className="resumen-venta__conteo">
            <div>
              <span>Productos</span>
              <strong>{totales.productos.toLocaleString('es-AR')}</strong>
            </div>
            <div>
              <span>Unidades</span>
              <strong>{totales.unidades.toLocaleString('es-AR')}</strong>
            </div>
          </div>
          <dl className="resumen-venta__detalle">
            {totales.participan > 0 && <Linea texto="Participan 02 y 46" importe={totales.participan} />}
            {totales.noParticipan > 0 && <Linea texto="No participan" importe={totales.noParticipan} />}
            {totales.escalon1 > 0 && <Linea texto="Descuento 6%" importe={-totales.escalon1} />}
            {totales.escalon2 > 0 && <Linea texto="Descuento adicional 5%" importe={-totales.escalon2} />}
            {totales.escalon3 > 0 && <Linea texto="Descuento adicional 5%" importe={-totales.escalon3} />}
            {numero(f.descuentoBono) > 0 && <Linea texto="Descuento bono" importe={-numero(f.descuentoBono)} />}
          </dl>
          <div className="resumen-venta__total">
            <span>Total</span>
            <strong>{moneda.format(totales.total)}</strong>
          </div>
        </aside>
      </div>

      {recuperado && <Aviso tipo="info">Se recuperó la venta que habías dejado sin terminar.</Aviso>}
      <Aviso>{error}</Aviso>

      <div className="editor-venta__pie">
        <div className="editor-venta__codigo">
          <input
            ref={refCodigo}
            className="campo__control"
            value={codigo}
            onChange={(e) => setCodigo(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault()
                void agregarPorCodigo()
              }
            }}
            onFocus={() => teclado.propio && setTecladoAbierto(true)}
            onClick={() => teclado.propio && setTecladoAbierto(true)}
            onBlur={() => setTecladoAbierto(false)}
            placeholder={agregando ? 'Buscando producto…' : 'Escaneá o escribí el código y Enter (0 para buscar)'}
            aria-label="Código de producto"
            inputMode={teclado.propio ? 'none' : teclado.tactil ? 'text' : 'numeric'}
            enterKeyHint="enter"
            autoComplete="off"
          />
          {teclado.tactil && !teclado.propio && (
            <Boton
              onClick={() => {
                teclado.cambiar('propio')
                window.requestAnimationFrame(() => refCodigo.current?.focus())
              }}
              aria-label="Usar el teclado numérico"
              title="Usar el teclado numérico"
            >
              <Grid3x3 size={16} />
            </Boton>
          )}
          <Boton onClick={() => void agregarPorCodigo()} aria-label="Agregar producto" title="Agregar producto">
            <Plus size={16} />
          </Boton>
          <Boton onClick={() => setBuscador(true)} aria-label="Buscar producto" title="Buscar producto">
            <Search size={16} />
          </Boton>
        </div>
        {teclado.propio && tecladoAbierto && (
          <TecladoCodigo
            refCampo={refCodigo}
            valor={codigo}
            alCambiar={setCodigo}
            alAgregar={() => void agregarPorCodigo()}
            alUsarSistema={() => {
              teclado.cambiar('sistema')
              const campo = refCodigo.current
              campo?.blur()
              window.requestAnimationFrame(() => campo?.focus())
            }}
            alOcultar={() => refCodigo.current?.blur()}
            deshabilitado={agregando}
          />
        )}
        {avisoProducto && <div className="editor-venta__aviso-producto">{avisoProducto}</div>}
        <div className="editor-venta__acciones">
          <span className={`editor-venta__autoguardado${autoguardado === 'error' ? ' editor-venta__autoguardado--error' : ''}`}>
            {mensajeAutoguardado(autoguardado, idVenta)}
          </span>
          <Boton onClick={salir} disabled={guardando}>
            <X size={16} /> Cerrar
          </Boton>
          <Boton
            type="submit"
            variante="primario"
            disabled={guardando || autoguardado === 'guardando' || agregando || f.items.length === 0}
          >
            <Save size={16} /> {guardando ? 'Guardando…' : 'Guardar venta'}
          </Boton>
        </div>
      </div>

      <Modal titulo="Buscar producto" abierto={buscador} alCerrar={() => setBuscador(false)} grande>
        <BuscadorProducto
          alElegir={(p) => {
            agregar(p)
            setBuscador(false)
            window.requestAnimationFrame(() => refCodigo.current?.focus())
          }}
        />
      </Modal>
    </form>
  )
}

function mensajeAutoguardado(estado: EstadoAutoguardado, id: number | null) {
  if (estado === 'guardando') return id ? `Autoguardando la venta ${id}…` : 'Creando la venta…'
  if (estado === 'guardado') return `Venta ${id} autoguardada.`
  if (estado === 'error') return 'No se pudo autoguardar. Queda un respaldo en este navegador.'
  return id ? `Venta ${id}` : 'Venta nueva'
}

function Linea({ texto, importe }: { texto: string; importe: number }) {
  return (
    <>
      <dt>{texto}</dt>
      <dd>{importe < 0 ? `-${moneda.format(-importe)}` : moneda.format(importe)}</dd>
    </>
  )
}

function Renglon({
  item,
  cambiar,
  quitar,
}: {
  item: ItemFormulario
  cambiar: (codigo: string, campo: 'cantidad' | 'precioVenta', valor: string) => void
  quitar: (codigo: string) => void
}) {
  return (
    <tr>
      <td className="tabla-renglones__producto">
        <div className="tabla-renglones__descripcion">{item.descripcion}</div>
        <div className="tabla-renglones__codigo">{item.idProducto}</div>
      </td>
      <td className="numero tabla-renglones__cantidad">
        <input
          className="campo__control"
          type="number"
          min="1"
          step="1"
          inputMode="numeric"
          value={item.cantidad}
          onChange={(e) => cambiar(item.idProducto, 'cantidad', e.target.value)}
          aria-label={`Cantidad de ${item.descripcion}`}
        />
      </td>
      <td className="numero tabla-renglones__precio">
        <input
          className="campo__control"
          type="number"
          step="0.01"
          inputMode="decimal"
          value={item.precioVenta}
          onChange={(e) => cambiar(item.idProducto, 'precioVenta', e.target.value)}
          aria-label={`Precio de ${item.descripcion}`}
        />
      </td>
      <td className="numero tabla-renglones__subtotal">{moneda.format(numero(item.cantidad) * numero(item.precioVenta))}</td>
      <td className="acciones tabla-renglones__quitar">
        <Boton chico onClick={() => quitar(item.idProducto)} aria-label={`Quitar ${item.descripcion}`} title="Quitar">
          <Trash2 size={14} />
        </Boton>
      </td>
    </tr>
  )
}

function BuscadorCliente({
  valor,
  elegido,
  alEscribir,
  alElegir,
}: {
  valor: string
  elegido: boolean
  alEscribir: (texto: string) => void
  alElegir: (c: Cliente) => void
}) {
  const [texto, setTexto] = useState('')
  const [abierto, setAbierto] = useState(false)
  useEffect(() => {
    const espera = window.setTimeout(() => setTexto(valor.trim()), 250)
    return () => window.clearTimeout(espera)
  }, [valor])
  const buscar = !elegido && texto.length >= 2
  const resultados = useQuery({
    queryKey: ['clientes', 'buscar-venta', texto],
    queryFn: () => api.get<Paginado<Cliente>>(`/api/clientes?tamano=20&buscar=${encodeURIComponent(texto)}`),
    enabled: buscar,
  })

  return (
    <div className="campo buscador-cliente">
      <label className="campo__etiqueta" htmlFor="venta-cliente">
        Cliente
      </label>
      <div className="buscador-cliente__control">
        <input
          id="venta-cliente"
          className="campo__control"
          value={valor}
          autoComplete="off"
          placeholder="Buscar por nombre, código, CUIT/DNI o teléfono"
          onChange={(e) => {
            alEscribir(e.target.value)
            setAbierto(true)
          }}
          onFocus={() => setAbierto(true)}
          onBlur={() => window.setTimeout(() => setAbierto(false), 150)}
          required
        />
        {elegido && (
          <Boton className="boton--icono" onClick={() => alEscribir('')} aria-label="Cambiar cliente" title="Cambiar cliente">
            <X size={15} />
          </Boton>
        )}
      </div>
      {abierto && buscar && (
        <div className="buscador-cliente__lista" role="listbox">
          {resultados.isPending && <div className="buscador-cliente__mensaje">Buscando clientes…</div>}
          {resultados.error && <div className="buscador-cliente__mensaje">No se pudieron buscar clientes.</div>}
          {resultados.data?.items.length === 0 && <div className="buscador-cliente__mensaje">No hay clientes con ese dato.</div>}
          {resultados.data?.items.map((c) => (
            <button
              type="button"
              role="option"
              aria-selected={false}
              key={c.id}
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => {
                alElegir(c)
                setAbierto(false)
              }}
            >
              <strong>{c.nombre.trim()}</strong>
              <span>
                {c.id} · {c.direccion?.trim() || c.zona}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

function BuscadorProducto({ alElegir }: { alElegir: (p: ProductoVenta) => void }) {
  const [buscar, setBuscar] = useState('')
  const [texto, setTexto] = useState('')
  const [error, setError] = useState<string | null>(null)
  useEffect(() => {
    const espera = window.setTimeout(() => setTexto(buscar.trim()), 220)
    return () => window.clearTimeout(espera)
  }, [buscar])
  const resultados = useQuery({
    queryKey: ['productos', 'buscar-venta', texto],
    queryFn: () => api.get<Paginado<ProductoVenta>>(`/api/productos?tamano=30&activo=true&buscar=${encodeURIComponent(texto)}`),
    enabled: texto.length >= 2,
  })
  const items = resultados.data?.items ?? []

  async function elegir(p: ProductoVenta) {
    try {
      // Se vuelve a leer por si cambió el precio desde la búsqueda.
      alElegir(await api.get<ProductoVenta>(`/api/productos/${encodeURIComponent(p.codigo)}`))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo cargar el producto.')
    }
  }

  return (
    <div className="buscador-producto">
      <input
        className="campo__control"
        value={buscar}
        onChange={(e) => setBuscar(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && items[0]) {
            e.preventDefault()
            void elegir(items[0])
          }
        }}
        placeholder="Buscar por código o descripción"
        aria-label="Buscar producto"
        autoFocus
      />
      <Aviso>{error ?? resultados.error?.message}</Aviso>
      <div className="buscador-producto__resultados">
        {texto.length < 2 && <div className="vacio">Escribí al menos 2 letras o números.</div>}
        {texto.length >= 2 && resultados.isPending && <div className="vacio">Buscando…</div>}
        {resultados.data && items.length === 0 && <div className="vacio">No hay productos con ese dato.</div>}
        {items.length > 0 && (
          <table className="tabla tabla-buscador">
            <thead>
              <tr>
                <th>Código</th>
                <th>Descripción</th>
                <th className="numero">Venta</th>
                <th className="numero">Oferta</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {items.map((p) => (
                <tr key={p.codigo} onDoubleClick={() => void elegir(p)}>
                  <td className="tabla-buscador__codigo">{p.codigo}</td>
                  <td>
                    <div className="tabla-buscador__nombre">{p.descripcion.trim()}</div>
                    <div className="tabla-buscador__rubro">{p.rubro}</div>
                  </td>
                  <td className="numero">{moneda.format(p.precioVenta)}</td>
                  <td className="numero">{p.precioOferta ? moneda.format(p.precioOferta) : '-'}</td>
                  <td className="acciones">
                    <Boton chico onClick={() => void elegir(p)} aria-label={`Agregar ${p.descripcion}`} title="Agregar">
                      <Plus size={14} />
                    </Boton>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
