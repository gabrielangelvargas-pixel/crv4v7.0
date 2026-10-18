import { useQuery } from '@tanstack/react-query'
import { api } from '../api/cliente'
import type { EstadoSalud } from '../api/tipos'
import { obtenerConfiguracion } from '../config'

/** Avisa arriba de todo si no se llega al servidor o si falta aplicar migraciones. */
export function BandaConexion() {
  const { data, isError } = useQuery({
    queryKey: ['salud'],
    queryFn: () => api.publico<EstadoSalud>('GET', '/api/salud'),
    refetchInterval: 60_000,
    retry: 1,
  })

  if (isError)
    return (
      <div className="banda-conexion" role="alert">
        No hay conexión con el servidor ({obtenerConfiguracion().apiUrl}). Revisá que la PC servidor esté encendida y, si es una PC
        nueva, que tenga instalado el certificado de CRV4.
      </div>
    )
  if (data && data.estado !== 'ok')
    return (
      <div className="banda-conexion" role="alert">
        {data.estado === 'sin-base-datos' ? 'El servidor no llega a la base de datos MySQL.' : 'El servidor tiene actualizaciones de base de datos pendientes.'}
      </div>
    )
  return null
}
