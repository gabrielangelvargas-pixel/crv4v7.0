import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronRight, ChevronsDownUp, ChevronsUpDown, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState, type CSSProperties, type FormEvent } from 'react'
import { api } from '../../api/cliente'
import { Permisos } from '../../auth/permisos'
import { useSesion } from '../../auth/SesionContexto'
import { aplanar, armarArbol, filtrarArbol, type Nodo, type Rubro } from './arbol'
import './rubros.css'
import { Aviso, Boton, Campo, Casilla, EstadoActivo, Modal, Selector, Tarjeta } from '../../ui/componentes'


type Edicion = Pick<Rubro, 'codigo' | 'nombre' | 'descripcion' | 'idPadre' | 'orden' | 'activo'> & { id?: number; productos?: number }

const nuevo: Edicion = { codigo: '', nombre: '', descripcion: '', idPadre: null, orden: 0, activo: true }

export function RubrosPage() {
  const { tiene } = useSesion()
  const puedeCrear = tiene(Permisos.RubrosCrear)
  const puedeEditar = tiene(Permisos.RubrosEditar)
  const puedeEliminar = tiene(Permisos.RubrosEliminar)
  const cliente = useQueryClient()
  const [buscar, setBuscar] = useState('')
  const [editando, setEditando] = useState<Edicion | null>(null)
  const [eliminando, setEliminando] = useState<Rubro | null>(null)

  const rubros = useQuery({ queryKey: ['rubros'], queryFn: () => api.get<Rubro[]>('/api/rubros') })
  const eliminar = useMutation({
    mutationFn: (id: number) => api.delete(`/api/rubros/${id}`),
    onSuccess: async () => {
      await cliente.invalidateQueries({ queryKey: ['rubros'] })
      await cliente.invalidateQueries({ queryKey: ['inicio'] })
      setEliminando(null)
    },
  })
  const filtro = buscar.trim().toLowerCase()
  const arbol = filtrarArbol(armarArbol(rubros.data ?? []), filtro)
  const [abiertos, setAbiertos] = useState<Set<number>>(new Set())
  const conHijos = (rubros.data ?? []).filter((r) => (rubros.data ?? []).some((h) => h.idPadre === r.id)).map((r) => r.id)
  const alternar = (id: number) =>
    setAbiertos((actual) => {
      const nuevos = new Set(actual)
      if (!nuevos.delete(id)) nuevos.add(id)
      return nuevos
    })
  const acciones: Acciones = {
    abierto: (id) => !!filtro || abiertos.has(id),
    alternar,
    editar: puedeEditar ? setEditando : undefined,
    eliminar: puedeEliminar ? setEliminando : undefined,
  }

  return (
    <>
      <Tarjeta className="tarjeta--lista">
        <div className="barra">
          <input
            className="campo__control barra__busqueda"
            placeholder="Buscar por código o nombre…"
            aria-label="Buscar rubro"
            value={buscar}
            onChange={(e) => setBuscar(e.target.value)}
          />
          {!filtro && conHijos.length > 0 && (
            <Boton onClick={() => setAbiertos(abiertos.size ? new Set() : new Set(conHijos))}>
              {abiertos.size ? <ChevronsDownUp size={16} /> : <ChevronsUpDown size={16} />}
              {abiertos.size ? 'Contraer todo' : 'Expandir todo'}
            </Boton>
          )}
          {puedeCrear && (
            <Boton variante="primario" onClick={() => setEditando(nuevo)}>
              <Plus size={16} /> Nuevo rubro
            </Boton>
          )}
        </div>
        <Aviso>{rubros.error?.message}</Aviso>
        <div className="zona-desplazable">
          <div className="acordeon">
            {arbol.map((nodo) => (
              <ItemRubro key={nodo.rubro.id} nodo={nodo} nivel={0} acciones={acciones} />
            ))}
          </div>
          {rubros.isPending && <div className="vacio">Cargando…</div>}
          {rubros.isSuccess && arbol.length === 0 && <div className="vacio">No hay rubros{filtro && ' que coincidan con la búsqueda'}.</div>}
        </div>
      </Tarjeta>

      <Modal titulo={editando?.id ? 'Editar rubro' : 'Nuevo rubro'} abierto={!!editando} alCerrar={() => setEditando(null)}>
        {editando && <FormularioRubro inicial={editando} rubros={rubros.data ?? []} alTerminar={() => setEditando(null)} />}
      </Modal>
      <Modal titulo="Eliminar rubro" abierto={!!eliminando} alCerrar={() => setEliminando(null)}>
        {eliminando && (
          <div className="formulario">
            <Aviso>{eliminar.error?.message}</Aviso>
            <p>
              ¿Eliminar el rubro <strong>{eliminando.codigo} · {eliminando.nombre}</strong>? No se puede deshacer.
            </p>
            <div className="formulario__acciones">
              <Boton onClick={() => setEliminando(null)}>Cancelar</Boton>
              <Boton variante="peligro" disabled={eliminar.isPending} onClick={() => eliminar.mutate(eliminando.id)}>
                {eliminar.isPending ? 'Eliminando…' : 'Eliminar'}
              </Boton>
            </div>
          </div>
        )}
      </Modal>
    </>
  )
}

interface Acciones {
  abierto: (id: number) => boolean
  alternar: (id: number) => void
  editar?: (r: Rubro) => void
  eliminar?: (r: Rubro) => void
}

function ItemRubro({ nodo, nivel, acciones }: { nodo: Nodo; nivel: number; acciones: Acciones }) {
  const { rubro: r, hijos } = nodo
  const abierto = hijos.length > 0 && acciones.abierto(r.id)
  const panel = `rubro-${r.id}-hijos`
  return (
    <div className={`acordeon__item${nivel === 0 ? ' acordeon__item--raiz' : ''}`}>
      <div className="acordeon__fila" style={{ '--nivel': nivel } as CSSProperties}>
        <button
          type="button"
          className="acordeon__boton"
          onClick={() => hijos.length && acciones.alternar(r.id)}
          aria-expanded={hijos.length ? abierto : undefined}
          aria-controls={hijos.length ? panel : undefined}
        >
          <span className={`acordeon__flecha${abierto ? ' acordeon__flecha--abierta' : ''}`} aria-hidden="true">
            {hijos.length > 0 && <ChevronRight size={18} />}
          </span>
          <span className="acordeon__texto">
            <span className="acordeon__titulo">
              <span className="acordeon__codigo">{r.codigo}</span> {r.nombre}
            </span>
            {r.descripcion && <span className="texto-secundario acordeon__descripcion">{r.descripcion}</span>}
            <span className="acordeon__datos">
              <span>{r.productos.toLocaleString('es-AR')} {r.productos === 1 ? 'producto' : 'productos'}</span>
              {hijos.length > 0 && <span>{hijos.length} {hijos.length === 1 ? 'subrubro' : 'subrubros'}</span>}
              <span>Orden {r.orden}</span>
              <EstadoActivo activo={r.activo} />
            </span>
          </span>
        </button>
        {(acciones.editar || acciones.eliminar) && (
          <div className="acordeon__acciones">
            {acciones.editar && (
              <Boton chico onClick={() => acciones.editar?.(r)} aria-label={`Editar ${r.nombre}`} title="Editar">
                <Pencil size={14} /> <span className="solo-ancho">Editar</span>
              </Boton>
            )}
            {acciones.eliminar && (
              <Boton
                chico
                variante="peligro"
                disabled={r.productos > 0 || hijos.length > 0}
                title={
                  r.productos > 0
                    ? 'Tiene productos: desactivalo en lugar de eliminarlo'
                    : hijos.length > 0
                      ? 'Tiene subrubros: eliminalos o movelos primero'
                      : 'Eliminar'
                }
                onClick={() => acciones.eliminar?.(r)}
                aria-label={`Eliminar ${r.nombre}`}
              >
                <Trash2 size={14} /> <span className="solo-ancho">Eliminar</span>
              </Boton>
            )}
          </div>
        )}
      </div>
      {abierto && (
        <div className="acordeon__panel" id={panel}>
          {hijos.map((h) => (
            <ItemRubro key={h.rubro.id} nodo={h} nivel={nivel + 1} acciones={acciones} />
          ))}
        </div>
      )}
    </div>
  )
}

/** Ids del rubro y de todos sus descendientes: no pueden ser su padre. */
function descendientes(id: number, rubros: Rubro[]): Set<number> {
  const resultado = new Set([id])
  let agregados = true
  while (agregados) {
    agregados = false
    for (const r of rubros)
      if (r.idPadre != null && resultado.has(r.idPadre) && !resultado.has(r.id)) {
        resultado.add(r.id)
        agregados = true
      }
  }
  return resultado
}

function FormularioRubro({ inicial, rubros, alTerminar }: { inicial: Edicion; rubros: Rubro[]; alTerminar: () => void }) {
  const cliente = useQueryClient()
  const [datos, setDatos] = useState(inicial)
  const excluidos = inicial.id ? descendientes(inicial.id, rubros) : new Set<number>()
  const posiblesPadres = aplanar(armarArbol(rubros)).filter(({ rubro }) => !excluidos.has(rubro.id))
  const guardar = useMutation({
    mutationFn: (d: Edicion) => {
      const cuerpo = { codigo: d.codigo, nombre: d.nombre, descripcion: d.descripcion || null, idPadre: d.idPadre, orden: d.orden, activo: d.activo }
      return d.id ? api.put(`/api/rubros/${d.id}`, cuerpo) : api.post('/api/rubros', cuerpo)
    },
    onSuccess: async () => {
      await cliente.invalidateQueries({ queryKey: ['rubros'] })
      await cliente.invalidateQueries({ queryKey: ['inicio'] })
      alTerminar()
    },
  })

  function enviar(e: FormEvent) {
    e.preventDefault()
    guardar.mutate(datos)
  }

  return (
    <form className="formulario" onSubmit={enviar}>
      <Aviso>{guardar.error?.message}</Aviso>
      <Campo
        etiqueta="Código"
        value={datos.codigo}
        onChange={(e) => setDatos({ ...datos, codigo: e.target.value.toUpperCase() })}
        maxLength={6}
        required
        autoFocus
        disabled={!!inicial.productos}
        title={inicial.productos ? 'El rubro tiene productos: el código no se puede cambiar' : undefined}
      />
      <Campo etiqueta="Nombre" value={datos.nombre} onChange={(e) => setDatos({ ...datos, nombre: e.target.value })} maxLength={45} required />
      <Campo etiqueta="Descripción" value={datos.descripcion ?? ''} onChange={(e) => setDatos({ ...datos, descripcion: e.target.value })} maxLength={255} />
      <Selector
        etiqueta="Rubro padre"
        value={datos.idPadre ?? ''}
        onChange={(e) => setDatos({ ...datos, idPadre: e.target.value ? Number(e.target.value) : null })}
      >
        <option value="">(ninguno)</option>
        {posiblesPadres.map(({ rubro: r, nivel }) => (
          <option key={r.id} value={r.id}>
            {'   '.repeat(nivel)}
            {r.codigo} · {r.nombre}
          </option>
        ))}
      </Selector>
      <Campo etiqueta="Orden" type="number" value={datos.orden} onChange={(e) => setDatos({ ...datos, orden: Number(e.target.value) })} />
      <Casilla etiqueta="Activo" checked={datos.activo} onChange={(e) => setDatos({ ...datos, activo: e.target.checked })} />
      <div className="formulario__acciones">
        <Boton onClick={alTerminar}>Cancelar</Boton>
        <Boton type="submit" variante="primario" disabled={guardar.isPending}>
          {guardar.isPending ? 'Guardando…' : 'Guardar'}
        </Boton>
      </div>
    </form>
  )
}
