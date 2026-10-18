import { useState } from 'react'
import { Permisos } from '../../auth/permisos'
import { useSesion } from '../../auth/SesionContexto'
import { RolesPanel } from './RolesPanel'
import { UsuariosPanel } from './UsuariosPanel'
import '../../ui/ui.css'

export function SeguridadPage() {
  const { tiene } = useSesion()
  const pestanas = [
    tiene(Permisos.SeguridadUsuarios) && { id: 'usuarios', titulo: 'Usuarios' },
    tiene(Permisos.SeguridadRoles) && { id: 'roles', titulo: 'Roles y permisos' },
  ].filter((p): p is { id: string; titulo: string } => !!p)
  const [activa, setActiva] = useState(pestanas[0]?.id)

  return (
    <>
      <h1 className="contenido__titulo">Seguridad</h1>
      <div className="pestanas" role="tablist">
        {pestanas.map((p) => (
          <button key={p.id} role="tab" className="pestana" aria-selected={activa === p.id} onClick={() => setActiva(p.id)}>
            {p.titulo}
          </button>
        ))}
      </div>
      {activa === 'usuarios' && <UsuariosPanel />}
      {activa === 'roles' && <RolesPanel />}
    </>
  )
}
