import type { ReactNode } from 'react'
import type { Permiso } from './permisos'
import { useSesion } from './SesionContexto'
import { Tarjeta } from '../ui/componentes'

/** Muestra el contenido solo si el usuario tiene alguno de los permisos. */
export function ConPermiso({ permisos, children }: { permisos: Permiso[]; children: ReactNode }) {
  const { tiene } = useSesion()
  if (tiene(...permisos)) return children
  return (
    <Tarjeta titulo="Sin permiso">
      <p>Tu usuario no tiene permiso para ver esta pantalla. Pedíselo a un administrador.</p>
    </Tarjeta>
  )
}
