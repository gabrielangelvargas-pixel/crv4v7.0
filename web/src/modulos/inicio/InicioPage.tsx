import { useQuery } from '@tanstack/react-query'
import { Boxes, PackageSearch, Tag, Users, type LucideIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { api } from '../../api/cliente'
import { useSesion } from '../../auth/SesionContexto'
import { Selector, Tarjeta } from '../../ui/componentes'
import './inicio.css'

interface ResumenInicio {
  productosActivos: number
  clientes: number
  rubros: number
}

const MESES = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre']

/** El tablero con los números del negocio es solo para administradores; el resto ve un reloj. */
export function InicioPage() {
  const { usuario } = useSesion()
  const esAdministrador = usuario?.roles.some((r) => r.trim().toLowerCase() === 'administrador') ?? false
  return esAdministrador ? <Tablero /> : <Reloj nombre={usuario?.nombre ?? ''} />
}

function Reloj({ nombre }: { nombre: string }) {
  const [ahora, setAhora] = useState(() => new Date())
  useEffect(() => {
    const intervalo = window.setInterval(() => setAhora(new Date()), 1000)
    return () => window.clearInterval(intervalo)
  }, [])
  const hora = ahora.getHours()
  const saludo = hora < 12 ? 'Buen día' : hora < 20 ? 'Buenas tardes' : 'Buenas noches'
  const fecha = ahora.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
  const dos = (n: number) => String(n).padStart(2, '0')
  return (
    <div className="reloj">
      <div className="reloj__saludo">
        {saludo}
        {nombre && `, ${nombre}`}
      </div>
      <div className="reloj__hora" role="timer" aria-live="off">
        {dos(hora)}:{dos(ahora.getMinutes())}
        <span className="reloj__segundos">{dos(ahora.getSeconds())}</span>
      </div>
      <div className="reloj__fecha">{fecha.charAt(0).toUpperCase() + fecha.slice(1)}</div>
    </div>
  )
}

function Tablero() {
  const { usuario } = useSesion()
  const resumen = useQuery({ queryKey: ['inicio', 'resumen'], queryFn: () => api.get<ResumenInicio>('/api/inicio/resumen') })
  const hoy = new Date()
  const anios = [0, 1, 2, 3].map((n) => hoy.getFullYear() - n)
  const [anioRef, setAnioRef] = useState(hoy.getFullYear())
  const [mes, setMes] = useState(hoy.getMonth())
  const [anioSemanal, setAnioSemanal] = useState(hoy.getFullYear())

  const valor = (n: number | null | undefined) => (resumen.isPending ? '…' : n == null ? '—' : n.toLocaleString('es-AR'))
  const pie = resumen.isError ? 'Sin conexión' : 'Actualizado desde MySQL'

  return (
    <>
      <h1 className="contenido__titulo">Inicio</h1>
      <div className="inicio-tarjetas">
        <Indicador icono={PackageSearch} titulo="Productos activos" valor={valor(resumen.data?.productosActivos)} pie={pie} />
        <Indicador icono={Users} titulo="Clientes" valor={valor(resumen.data?.clientes)} pie={pie} />
        <Indicador icono={Tag} titulo="Rubros" valor={valor(resumen.data?.rubros)} pie={pie} />
        <Indicador icono={Boxes} titulo="Permisos" valor={String(usuario?.permisos.length ?? 0)} pie={usuario?.roles.join(', ') ?? ''} />
      </div>

      <div className="inicio-graficos">
        <Tarjeta
          titulo="Importe mensual"
          subtitulo="Comparativo últimos 3 años"
          acciones={
            <div className="filtros">
              <Selector etiqueta="Año ref." value={anioRef} onChange={(e) => setAnioRef(Number(e.target.value))}>
                {anios.map((a) => (
                  <option key={a}>{a}</option>
                ))}
              </Selector>
            </div>
          }
        >
          <div className="grafico-vacio">El gráfico aparece cuando esté el módulo de Ventas.</div>
        </Tarjeta>
        <Tarjeta
          titulo="Importe semanal"
          subtitulo={`${MESES[mes]} ${anioSemanal}`}
          acciones={
            <div className="filtros">
              <Selector etiqueta="Mes" value={mes} onChange={(e) => setMes(Number(e.target.value))}>
                {MESES.map((m, i) => (
                  <option key={m} value={i}>
                    {m}
                  </option>
                ))}
              </Selector>
              <Selector etiqueta="Año" value={anioSemanal} onChange={(e) => setAnioSemanal(Number(e.target.value))}>
                {anios.map((a) => (
                  <option key={a}>{a}</option>
                ))}
              </Selector>
            </div>
          }
        >
          <div className="grafico-vacio">El gráfico aparece cuando esté el módulo de Ventas.</div>
        </Tarjeta>
      </div>
    </>
  )
}

function Indicador({ icono: Icono, titulo, valor, pie }: { icono: LucideIcon; titulo: string; valor: string; pie: string }) {
  return (
    <section className="tarjeta indicador">
      <div className="indicador__titulo">
        <Icono size={18} /> {titulo}
      </div>
      <div className="indicador__valor">{valor}</div>
      <div className="indicador__pie">{pie}</div>
    </section>
  )
}
