import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { api } from '../../api/cliente'
import { Permisos } from '../../auth/permisos'
import { useSesion } from '../../auth/SesionContexto'
import { Aviso, Boton, Campo, Casilla, EstadoActivo, Modal, Selector, Tarjeta } from '../../ui/componentes'

export interface Rubro {
  id: number
  codigo: string
  nombre: string
  descripcion: string | null
  idPadre: number | null
  nombrePadre: string | null
  orden: number
  activo: boolean
  productos: number
}

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
  const arbol = ordenarPorJerarquia(rubros.data ?? [])
  const lista = arbol.filter(({ rubro: r }) => !filtro || r.nombre.toLowerCase().includes(filtro) || r.codigo.toLowerCase().includes(filtro))
  const conAcciones = puedeEditar || puedeEliminar

  return (
    <>
      <h1 className="contenido__titulo">Rubros</h1>
      <Tarjeta>
        <div className="barra">
          <input
            className="campo__control barra__busqueda"
            placeholder="Buscar por código o nombre…"
            aria-label="Buscar rubro"
            value={buscar}
            onChange={(e) => setBuscar(e.target.value)}
          />
          {puedeCrear && (
            <Boton variante="primario" onClick={() => setEditando(nuevo)}>
              <Plus size={16} /> Nuevo rubro
            </Boton>
          )}
        </div>
        <Aviso>{rubros.error?.message}</Aviso>
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead>
              <tr>
                <th>Código</th>
                <th>Nombre</th>
                <th>Rubro padre</th>
                <th>Productos</th>
                <th>Orden</th>
                <th>Estado</th>
                {conAcciones && <th />}
              </tr>
            </thead>
            <tbody>
              {lista.map(({ rubro: r, nivel }) => (
                <tr key={r.id}>
                  <td>{r.codigo}</td>
                  <td style={{ paddingLeft: 12 + nivel * 24 }}>
                    {nivel > 0 && <span className="texto-secundario">└ </span>}
                    {r.nombre}
                    {r.descripcion && <div className="texto-secundario">{r.descripcion}</div>}
                  </td>
                  <td>{r.nombrePadre}</td>
                  <td>{r.productos.toLocaleString('es-AR')}</td>
                  <td>{r.orden}</td>
                  <td>
                    <EstadoActivo activo={r.activo} />
                  </td>
                  {conAcciones && (
                    <td className="acciones">
                      {puedeEditar && (
                        <Boton chico onClick={() => setEditando(r)} aria-label={`Editar ${r.nombre}`}>
                          <Pencil size={14} /> Editar
                        </Boton>
                      )}{' '}
                      {puedeEliminar && (
                        <Boton
                          chico
                          variante="peligro"
                          disabled={r.productos > 0}
                          title={r.productos > 0 ? 'Tiene productos: desactivalo en lugar de eliminarlo' : undefined}
                          onClick={() => setEliminando(r)}
                          aria-label={`Eliminar ${r.nombre}`}
                        >
                          <Trash2 size={14} /> Eliminar
                        </Boton>
                      )}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
          {rubros.isPending && <div className="vacio">Cargando…</div>}
          {rubros.isSuccess && lista.length === 0 && <div className="vacio">No hay rubros{filtro && ' que coincidan con la búsqueda'}.</div>}
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

/**
 * Orden de árbol: cada padre seguido de sus hijos, nietos, etc., y después el siguiente padre.
 * Entre hermanos ordena por Orden (los de Orden 0, sin asignar, van al final) y luego por nombre. Un rubro cuyo padre no está en la lista va como raíz.
 */
function ordenarPorJerarquia(rubros: Rubro[]): { rubro: Rubro; nivel: number }[] {
  const ids = new Set(rubros.map((r) => r.id))
  const hijos = new Map<number | null, Rubro[]>()
  const clave = (orden: number) => (orden === 0 ? Number.MAX_SAFE_INTEGER : orden)
  for (const r of rubros) {
    const padre = r.idPadre != null && ids.has(r.idPadre) ? r.idPadre : null
    hijos.set(padre, [...(hijos.get(padre) ?? []), r])
  }
  for (const grupo of hijos.values()) grupo.sort((a, b) => clave(a.orden) - clave(b.orden) || a.nombre.localeCompare(b.nombre, 'es'))

  const resultado: { rubro: Rubro; nivel: number }[] = []
  const vistos = new Set<number>()
  const recorrer = (padre: number | null, nivel: number) => {
    for (const r of hijos.get(padre) ?? []) {
      if (vistos.has(r.id)) continue
      vistos.add(r.id)
      resultado.push({ rubro: r, nivel })
      recorrer(r.id, nivel + 1)
    }
  }
  recorrer(null, 0)
  // Si hubiera un ciclo en los datos, esos rubros no cuelgan de ninguna raíz: se agregan al final.
  for (const r of rubros) if (!vistos.has(r.id)) recorrer(r.idPadre, 0)
  return resultado
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
  const posiblesPadres = ordenarPorJerarquia(rubros).filter(({ rubro }) => !excluidos.has(rubro.id))
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
