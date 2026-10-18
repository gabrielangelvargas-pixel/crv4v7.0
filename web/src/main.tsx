import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ErrorApi } from './api/cliente'
import { App } from './App'
import { ProveedorSesion } from './auth/SesionContexto'
import { cargarConfiguracion } from './config'
import { registrarPwa } from './pwa/registrar'
import './estilos/base.css'
import './ui/ui.css'

const consultas = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: true,
      // No reintentar errores que no se arreglan solos (permisos, validación, no encontrado).
      retry: (intentos, error) => !(error instanceof ErrorApi && error.estado >= 400 && error.estado < 500) && intentos < 2,
    },
  },
})

const raiz = createRoot(document.getElementById('root')!)

cargarConfiguracion()
  .then(() => {
    raiz.render(
      <StrictMode>
        <QueryClientProvider client={consultas}>
          <ProveedorSesion>
            <App />
          </ProveedorSesion>
        </QueryClientProvider>
      </StrictMode>,
    )
  })
  .catch((error: unknown) => {
    raiz.render(<div className="cargando-pantalla">No se pudo iniciar CRV4: {error instanceof Error ? error.message : String(error)}</div>)
  })

registrarPwa()
