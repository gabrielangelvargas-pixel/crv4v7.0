import { lazy, Suspense } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router'
import { CambiarPasswordPage } from './auth/CambiarPasswordPage'
import { ConPermiso } from './auth/Guardias'
import { LoginPage } from './auth/LoginPage'
import { Permisos } from './auth/permisos'
import { useSesion } from './auth/SesionContexto'
import { AppLayout } from './layout/AppLayout'
import { EnConstruccion, NoEncontrado } from './modulos/EnConstruccion'
import { InicioPage } from './modulos/inicio/InicioPage'

// Cada módulo se descarga recién cuando se abre por primera vez.
const ProductosPage = lazy(() => import('./modulos/productos/ProductosPage').then((m) => ({ default: m.ProductosPage })))
const RubrosPage = lazy(() => import('./modulos/rubros/RubrosPage').then((m) => ({ default: m.RubrosPage })))
const SeguridadPage = lazy(() => import('./modulos/seguridad/SeguridadPage').then((m) => ({ default: m.SeguridadPage })))

export function App() {
  const { usuario, cargando } = useSesion()

  if (cargando) return <div className="cargando-pantalla">Cargando…</div>
  if (!usuario) return <LoginPage />
  if (usuario.debeCambiarPassword) return <CambiarPasswordPage />

  return (
    <BrowserRouter>
      <Suspense fallback={<div className="vacio">Cargando…</div>}>
        <Routes>
          <Route element={<AppLayout />}>
            <Route index element={<InicioPage />} />
            <Route path="productos" element={<ConPermiso permisos={[Permisos.ProductosVer]}><ProductosPage /></ConPermiso>} />
            <Route path="clientes" element={<ConPermiso permisos={[Permisos.ClientesVer]}><EnConstruccion titulo="Clientes" /></ConPermiso>} />
            <Route path="ventas" element={<ConPermiso permisos={[Permisos.VentasVer]}><EnConstruccion titulo="Ventas" /></ConPermiso>} />
            <Route path="estadisticas" element={<ConPermiso permisos={[Permisos.ReportesVer]}><EnConstruccion titulo="Estadísticas" /></ConPermiso>} />
            <Route path="control-ventas" element={<ConPermiso permisos={[Permisos.ReportesVer]}><EnConstruccion titulo="Control Ventas" /></ConPermiso>} />
            <Route path="rubros" element={<ConPermiso permisos={[Permisos.RubrosVer]}><RubrosPage /></ConPermiso>} />
            <Route path="seguridad" element={<ConPermiso permisos={[Permisos.UsuariosAdministrar]}><SeguridadPage /></ConPermiso>} />
            <Route path="*" element={<NoEncontrado />} />
          </Route>
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
