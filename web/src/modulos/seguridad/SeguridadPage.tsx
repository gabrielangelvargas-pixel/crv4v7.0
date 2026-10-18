import { useState } from 'react'
import { RolesPanel } from './RolesPanel'
import { UsuariosPanel } from './UsuariosPanel'
import '../../ui/ui.css'

export function SeguridadPage() {
  // La ruta ya exige usuarios.administrar, que habilita las dos pestañas.
  const pestanas = [
    { id: 'usuarios', titulo: 'Usuarios' },
    { id: 'roles', titulo: 'Roles y permisos' },
  ]
  const [activa, setActiva] = useState(pestanas[0].id)

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
