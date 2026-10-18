import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { api } from '../../api/cliente'
import { Permisos } from '../../auth/permisos'
import { useSesion } from '../../auth/SesionContexto'
import { Aviso, Boton, Campo, Casilla, EstadoActivo, Modal, Tarjeta } from '../../ui/componentes'

export interface Rubro {
  id: number
  nombre: string
  descripcion: string | null
  orden: number
  activo: boolean
}

type Edicion = Omit<Rubro, 'id'> & { id?: number }

const nuevo: Edicion = { nombre: '', descripcion: '', orden: 0, activo: true }

export function RubrosPage() {
  const { tiene } = useSesion()
  const puedeEditar = tiene(Permisos.RubrosEditar)
  const [buscar, setBuscar] = useState('')
  const [editando, setEditando] = useState<Edicion | null>(null)

  const rubros = useQuery({ queryKey: ['rubros'], queryFn: () => api.get<Rubro[]>('/api/rubros') })
  const filtro = buscar.trim().toLowerCase()
  const lista = (rubros.data ?? []).filter((r) => !filtro || r.nombre.toLowerCase().includes(filtro))

  return (
    <>
      <h1 className="contenido__titulo">Rubros</h1>
      <Tarjeta>
        <div className="barra">
          <input
            className="campo__control barra__busqueda"
            placeholder="Buscar rubro…"
            aria-label="Buscar rubro"
            value={buscar}
            onChange={(e) => setBuscar(e.target.value)}
          />
          {puedeEditar && (
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
                <th>Orden</th>
                <th>Nombre</th>
                <th>Descripción</th>
                <th>Estado</th>
                {puedeEditar && <th />}
              </tr>
            </thead>
            <tbody>
              {lista.map((r) => (
                <tr key={r.id}>
                  <td>{r.orden}</td>
                  <td>{r.nombre}</td>
                  <td>{r.descripcion}</td>
                  <td>
                    <EstadoActivo activo={r.activo} />
                  </td>
                  {puedeEditar && (
                    <td className="acciones">
                      <Boton chico onClick={() => setEditando(r)} aria-label={`Editar ${r.nombre}`}>
                        <Pencil size={14} /> Editar
                      </Boton>
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
        {editando && <FormularioRubro inicial={editando} alTerminar={() => setEditando(null)} />}
      </Modal>
    </>
  )
}

function FormularioRubro({ inicial, alTerminar }: { inicial: Edicion; alTerminar: () => void }) {
  const cliente = useQueryClient()
  const [datos, setDatos] = useState(inicial)
  const guardar = useMutation({
    mutationFn: (d: Edicion) => {
      const cuerpo = { nombre: d.nombre, descripcion: d.descripcion, orden: d.orden, activo: d.activo }
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
      <Campo etiqueta="Nombre" value={datos.nombre} onChange={(e) => setDatos({ ...datos, nombre: e.target.value })} maxLength={100} required autoFocus />
      <Campo etiqueta="Descripción" value={datos.descripcion ?? ''} onChange={(e) => setDatos({ ...datos, descripcion: e.target.value })} maxLength={500} />
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
