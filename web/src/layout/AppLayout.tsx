import { LogOut, Menu as IconoMenu, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router'
import { useSesion } from '../auth/SesionContexto'
import { Boton } from '../ui/componentes'
import { BandaConexion } from './BandaConexion'
import { menu } from './menu'
import './layout.css'

export function AppLayout() {
  const { usuario, salir, tiene } = useSesion()
  const [abierto, setAbierto] = useState(false)
  const { pathname } = useLocation()
  const opciones = menu.filter((item) => tiene(...item.permisos))
  const actual = menu.find((item) => (item.ruta === '/' ? pathname === '/' : pathname.startsWith(item.ruta)))

  useEffect(() => setAbierto(false), [pathname])
  useEffect(() => {
    document.title = actual ? `${actual.titulo} · CRV4 Mayorista` : 'CRV4 Mayorista'
  }, [actual])

  return (
    <div className="app">
      <BandaConexion />
      <header className="encabezado">
        <Boton className="boton--icono" aria-label="Abrir menú" aria-expanded={abierto} onClick={() => setAbierto(true)}>
          <IconoMenu size={20} />
        </Boton>
        <div>
          <div className="encabezado__marca">CRV4 Mayorista</div>
          <div className="encabezado__titulo">{actual?.titulo ?? ''}</div>
        </div>
        <div className="encabezado__usuario">
          <span className="encabezado__nombre">{usuario?.nombre}</span>
          <Boton className="boton--icono" aria-label="Salir" title="Salir" onClick={salir}>
            <LogOut size={18} />
          </Boton>
        </div>
      </header>

      {abierto && <div className="menu-fondo" onClick={() => setAbierto(false)} />}
      <nav className={`menu ${abierto ? 'menu--abierto' : ''}`} aria-label="Menú principal" inert={!abierto}>
        <div className="menu__cabecera">
          <div>
            <div className="encabezado__marca">CRV4 Mayorista</div>
            <div className="menu__titulo">Gestión interna</div>
          </div>
          <Boton className="boton--icono" aria-label="Cerrar menú" onClick={() => setAbierto(false)}>
            <X size={18} />
          </Boton>
        </div>
        <ul className="menu__lista">
          {opciones.map(({ ruta, titulo, icono: Icono }) => (
            <li key={ruta}>
              <NavLink to={ruta} end={ruta === '/'} className="menu__enlace">
                <Icono size={18} />
                {titulo}
              </NavLink>
            </li>
          ))}
        </ul>
        <div className="menu__pie">
          <strong>{usuario?.nombre}</strong>
          <span>{usuario?.rol}</span>
          <Boton onClick={salir}>
            <LogOut size={16} /> Salir
          </Boton>
        </div>
      </nav>

      <main className="contenido">
        <Outlet />
      </main>
    </div>
  )
}
