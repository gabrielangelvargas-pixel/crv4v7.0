import { useState, type FormEvent } from 'react'
import { useSesion } from './SesionContexto'
import { Aviso, Boton, Campo } from '../ui/componentes'
import '../layout/layout.css'

export function LoginPage() {
  const { ingresar } = useSesion()
  const [usuario, setUsuario] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [enviando, setEnviando] = useState(false)

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setError('')
    setEnviando(true)
    try {
      await ingresar(usuario.trim(), password)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo ingresar.')
      setPassword('')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="acceso">
      <form className="acceso__caja formulario" onSubmit={enviar}>
        <div>
          <div className="acceso__marca">CRV4 Mayorista</div>
          <h1 className="acceso__titulo">Ingresar</h1>
        </div>
        <Aviso>{error}</Aviso>
        <Campo etiqueta="Usuario" value={usuario} onChange={(e) => setUsuario(e.target.value)} autoComplete="username" autoFocus required />
        <Campo
          etiqueta="Contraseña"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
          required
        />
        <Boton type="submit" variante="primario" disabled={enviando}>
          {enviando ? 'Ingresando…' : 'Ingresar'}
        </Boton>
        <div className="acceso__pie">Versión {__VERSION__}</div>
      </form>
    </div>
  )
}
