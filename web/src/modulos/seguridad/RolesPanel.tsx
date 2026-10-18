import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { api } from '../../api/cliente'
import { Aviso, Boton, Campo, Casilla, Modal, Tarjeta } from '../../ui/componentes'
import type { PermisoDto, RolDto } from './tipos'

type Edicion = { id?: number; nombre: string; descripcion: string; permisos: string[] }

export function RolesPanel() {
  const cliente = useQueryClient()
  const [editando, setEditando] = useState<Edicion | null>(null)
  const roles = useQuery({ queryKey: ['roles'], queryFn: () => api.get<RolDto[]>('/api/seguridad/roles') })
  const permisos = useQuery({ queryKey: ['permisos'], queryFn: () => api.get<PermisoDto[]>('/api/seguridad/permisos') })
  const borrar = useMutation({
    mutationFn: (id: number) => api.delete(`/api/seguridad/roles/${id}`),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['roles'] }),
  })

  return (
    <Tarjeta
      titulo="Roles y permisos"
      subtitulo="Los cambios de permisos llegan a cada usuario en pocos minutos."
      acciones={
        <Boton variante="primario" onClick={() => setEditando({ nombre: '', descripcion: '', permisos: [] })}>
          <Plus size={16} /> Nuevo rol
        </Boton>
      }
    >
      <Aviso>{roles.error?.message ?? borrar.error?.message}</Aviso>
      <div className="tabla-contenedor">
        <table className="tabla">
          <thead>
            <tr>
              <th>Rol</th>
              <th>Descripción</th>
              <th>Permisos</th>
              <th>Usuarios</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {roles.data?.map((r) => (
              <tr key={r.id}>
                <td>{r.nombre}</td>
                <td>{r.descripcion}</td>
                <td>{r.permisos.length}</td>
                <td>{r.usuarios}</td>
                <td className="acciones">
                  {r.esSistema ? (
                    <span className="etiqueta etiqueta--inactivo">Todos los permisos</span>
                  ) : (
                    <>
                      <Boton chico onClick={() => setEditando({ id: r.id, nombre: r.nombre, descripcion: r.descripcion ?? '', permisos: r.permisos })}>
                        <Pencil size={14} /> Editar
                      </Boton>{' '}
                      <Boton chico variante="peligro" disabled={r.usuarios > 0} title={r.usuarios > 0 ? 'Tiene usuarios asignados' : undefined} onClick={() => borrar.mutate(r.id)}>
                        <Trash2 size={14} /> Borrar
                      </Boton>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Modal titulo={editando?.id ? 'Editar rol' : 'Nuevo rol'} abierto={!!editando} alCerrar={() => setEditando(null)}>
        {editando && <FormularioRol inicial={editando} permisos={permisos.data ?? []} alTerminar={() => setEditando(null)} />}
      </Modal>
    </Tarjeta>
  )
}

function FormularioRol({ inicial, permisos, alTerminar }: { inicial: Edicion; permisos: PermisoDto[]; alTerminar: () => void }) {
  const cliente = useQueryClient()
  const [datos, setDatos] = useState(inicial)
  const guardar = useMutation({
    mutationFn: (d: Edicion) => {
      const cuerpo = { nombre: d.nombre, descripcion: d.descripcion, permisos: d.permisos }
      return d.id ? api.put(`/api/seguridad/roles/${d.id}`, cuerpo) : api.post('/api/seguridad/roles', cuerpo)
    },
    onSuccess: async () => {
      await cliente.invalidateQueries({ queryKey: ['roles'] })
      alTerminar()
    },
  })
  const modulos = [...new Set(permisos.map((p) => p.modulo))]
  const alternar = (codigo: string, marcado: boolean) =>
    setDatos({ ...datos, permisos: marcado ? [...datos.permisos, codigo] : datos.permisos.filter((p) => p !== codigo) })

  const enviar = (e: FormEvent) => {
    e.preventDefault()
    guardar.mutate(datos)
  }

  return (
    <form className="formulario" onSubmit={enviar}>
      <Aviso>{guardar.error?.message}</Aviso>
      <Campo etiqueta="Nombre" value={datos.nombre} onChange={(e) => setDatos({ ...datos, nombre: e.target.value })} maxLength={60} required autoFocus />
      <Campo etiqueta="Descripción" value={datos.descripcion} onChange={(e) => setDatos({ ...datos, descripcion: e.target.value })} maxLength={250} />
      {modulos.map((m) => (
        <fieldset key={m} className="formulario" style={{ border: '1px solid var(--color-borde)', borderRadius: 8, padding: 12, gap: 6 }}>
          <legend className="campo__etiqueta">{m}</legend>
          {permisos
            .filter((p) => p.modulo === m)
            .map((p) => (
              <Casilla key={p.codigo} etiqueta={p.descripcion} checked={datos.permisos.includes(p.codigo)} onChange={(e) => alternar(p.codigo, e.target.checked)} />
            ))}
        </fieldset>
      ))}
      <div className="formulario__acciones">
        <Boton onClick={alTerminar}>Cancelar</Boton>
        <Boton type="submit" variante="primario" disabled={guardar.isPending}>
          {guardar.isPending ? 'Guardando…' : 'Guardar'}
        </Boton>
      </div>
    </form>
  )
}
