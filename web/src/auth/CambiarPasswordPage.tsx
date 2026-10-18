import { useState, type FormEvent } from 'react'
import { api } from '../api/cliente'
import type { SesionRespuesta } from '../api/tipos'
import { Aviso, Boton, Campo } from '../ui/componentes'
import { useSesion } from './SesionContexto'
import '../layout/layout.css'

/** Se muestra sola cuando el usuario tiene que cambiar la contraseña (primer ingreso o reinicio). */
export function CambiarPasswordPage() {
  const { usuario, aplicarSesion, salir } = useSesion()
  const [actual, setActual] = useState('')
  const [nueva, setNueva] = useState('')
  const [repetida, setRepetida] = useState('')
  const [error, setError] = useState('')
  const [enviando, setEnviando] = useState(false)

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setError('')
    if (nueva !== repetida) return setError('Las contraseñas nuevas no coinciden.')
    setEnviando(true)
    try {
      const sesion = await api.post<SesionRespuesta>('/api/auth/cambiar-password', { passwordActual: actual, passwordNueva: nueva })
      aplicarSesion(sesion)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo cambiar la contraseña.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="acceso">
      <form className="acceso__caja formulario" onSubmit={enviar}>
        <div>
          <div className="acceso__marca">CRV4 Mayorista</div>
          <h1 className="acceso__titulo">Cambiá tu contraseña</h1>
          <p>Hola {usuario?.nombre}. Antes de seguir tenés que elegir una contraseña nueva (al menos 8 caracteres, con letras y números).</p>
        </div>
        <Aviso>{error}</Aviso>
        <Campo etiqueta="Contraseña actual" type="password" value={actual} onChange={(e) => setActual(e.target.value)} autoComplete="current-password" required />
        <Campo etiqueta="Contraseña nueva" type="password" value={nueva} onChange={(e) => setNueva(e.target.value)} autoComplete="new-password" required />
        <Campo etiqueta="Repetir contraseña nueva" type="password" value={repetida} onChange={(e) => setRepetida(e.target.value)} autoComplete="new-password" required />
        <Boton type="submit" variante="primario" disabled={enviando}>
          {enviando ? 'Guardando…' : 'Guardar y continuar'}
        </Boton>
        <Boton onClick={salir}>Salir</Boton>
      </form>
    </div>
  )
}
