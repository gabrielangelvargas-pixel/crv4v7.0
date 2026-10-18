import { X } from 'lucide-react'
import { useEffect, useId, useRef, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from 'react'
import './ui.css'

type Variante = 'primario' | 'peligro' | 'normal'

export function Boton({
  variante = 'normal',
  chico,
  className,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variante?: Variante; chico?: boolean }) {
  const clases = ['boton', variante !== 'normal' && `boton--${variante}`, chico && 'boton--chico', className].filter(Boolean)
  return <button type="button" {...props} className={clases.join(' ')} />
}

export function Campo({ etiqueta, error, ...props }: InputHTMLAttributes<HTMLInputElement> & { etiqueta: string; error?: string }) {
  const id = useId()
  return (
    <div className="campo">
      <label className="campo__etiqueta" htmlFor={id}>
        {etiqueta}
      </label>
      <input id={id} className="campo__control" aria-invalid={!!error} {...props} />
      {error && <span className="campo__error">{error}</span>}
    </div>
  )
}

export function Selector({
  etiqueta,
  children,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement> & { etiqueta: string; children: ReactNode }) {
  const id = useId()
  return (
    <div className="campo">
      <label className="campo__etiqueta" htmlFor={id}>
        {etiqueta}
      </label>
      <select id={id} className="campo__control" {...props}>
        {children}
      </select>
    </div>
  )
}

export function Casilla({ etiqueta, ...props }: InputHTMLAttributes<HTMLInputElement> & { etiqueta: string }) {
  return (
    <label className="campo--check">
      <input type="checkbox" {...props} />
      {etiqueta}
    </label>
  )
}

export function Aviso({ tipo = 'error', children }: { tipo?: 'error' | 'exito' | 'info'; children: ReactNode }) {
  if (!children) return null
  return (
    <div className={`aviso aviso--${tipo}`} role={tipo === 'error' ? 'alert' : 'status'}>
      {children}
    </div>
  )
}

export function Tarjeta({
  titulo,
  subtitulo,
  acciones,
  children,
  className,
}: {
  titulo?: ReactNode
  subtitulo?: ReactNode
  acciones?: ReactNode
  children: ReactNode
  className?: string
}) {
  return (
    <section className={['tarjeta', className].filter(Boolean).join(' ')}>
      {(titulo || acciones) && (
        <header className="tarjeta__encabezado">
          <div>
            {titulo && <h2 className="tarjeta__titulo">{titulo}</h2>}
            {subtitulo && <div className="tarjeta__subtitulo">{subtitulo}</div>}
          </div>
          {acciones}
        </header>
      )}
      <div className="tarjeta__cuerpo">{children}</div>
    </section>
  )
}

/** Modal accesible basado en <dialog>. */
export function Modal({ titulo, abierto, alCerrar, children }: { titulo: string; abierto: boolean; alCerrar: () => void; children: ReactNode }) {
  const ref = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const dialogo = ref.current
    if (!dialogo) return
    if (abierto && !dialogo.open) dialogo.showModal()
    if (!abierto && dialogo.open) dialogo.close()
  }, [abierto])

  return (
    <dialog ref={ref} className="modal" onClose={alCerrar} onCancel={alCerrar}>
      <div className="modal__encabezado">
        <h2 className="modal__titulo">{titulo}</h2>
        <Boton className="boton--icono" aria-label="Cerrar" onClick={alCerrar}>
          <X size={18} />
        </Boton>
      </div>
      <div className="modal__cuerpo">{abierto && children}</div>
    </dialog>
  )
}

export function EstadoActivo({ activo }: { activo: boolean }) {
  return <span className={`etiqueta etiqueta--${activo ? 'activo' : 'inactivo'}`}>{activo ? 'Activo' : 'Inactivo'}</span>
}
