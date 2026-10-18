import { Construction } from 'lucide-react'
import { Tarjeta } from '../ui/componentes'

export function EnConstruccion({ titulo }: { titulo: string }) {
  return (
    <>
      <h1 className="contenido__titulo">{titulo}</h1>
      <Tarjeta>
        <div className="vacio">
          <Construction size={32} />
          <p>Este módulo se arma en la próxima etapa, cuando tengamos la estructura de datos definida.</p>
        </div>
      </Tarjeta>
    </>
  )
}

export function NoEncontrado() {
  return (
    <Tarjeta titulo="Página no encontrada">
      <p>La dirección no existe. Usá el menú para elegir una sección.</p>
    </Tarjeta>
  )
}
