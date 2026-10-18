import { ChevronDown, CornerDownLeft, Delete } from 'lucide-react'
import { useEffect, useRef, type ButtonHTMLAttributes, type PointerEvent, type RefObject } from 'react'

/**
 * Teclado numérico propio para el código de producto en pantallas táctiles.
 * El campo usa inputMode="none" para que no aparezca el teclado del sistema; el lector de código de barras sigue andando.
 */
export function TecladoCodigo({
  refCampo,
  valor,
  alCambiar,
  alAgregar,
  alUsarSistema,
  alOcultar,
  deshabilitado,
}: {
  refCampo: RefObject<HTMLInputElement | null>
  valor: string
  alCambiar: (valor: string) => void
  alAgregar: () => void
  alUsarSistema: () => void
  alOcultar: () => void
  deshabilitado?: boolean
}) {
  const refTeclado = useRef<HTMLDivElement>(null)

  // Que se vean el campo y el teclado completos al abrirlo.
  useEffect(() => {
    refTeclado.current?.scrollIntoView({ block: 'end', behavior: 'smooth' })
  }, [])

  // Al apretar una tecla el foco sigue en el campo, así el lector de código y el cursor no se pierden.
  const sinPerderFoco = (e: PointerEvent) => e.preventDefault()

  function escribir(texto: string | null) {
    const campo = refCampo.current
    const inicio = campo?.selectionStart ?? valor.length
    const fin = campo?.selectionEnd ?? valor.length
    let nuevo: string
    let cursor: number
    if (texto == null) {
      if (inicio === fin && inicio === 0) return
      const desde = inicio === fin ? inicio - 1 : inicio
      nuevo = valor.slice(0, desde) + valor.slice(fin)
      cursor = desde
    } else {
      nuevo = valor.slice(0, inicio) + texto + valor.slice(fin)
      cursor = inicio + texto.length
    }
    navigator.vibrate?.(8)
    alCambiar(nuevo)
    window.requestAnimationFrame(() => campo?.setSelectionRange(cursor, cursor))
  }

  return (
    <div ref={refTeclado} className="teclado-codigo" role="group" aria-label="Teclado numérico">
      {['1', '2', '3'].map((n) => (
        <Tecla key={n} onPointerDown={sinPerderFoco} onClick={() => escribir(n)}>
          {n}
        </Tecla>
      ))}
      <Tecla className="teclado-codigo__borrar" onPointerDown={sinPerderFoco} onClick={() => escribir(null)} aria-label="Borrar">
        <Delete size={22} />
      </Tecla>
      {['4', '5', '6'].map((n) => (
        <Tecla key={n} onPointerDown={sinPerderFoco} onClick={() => escribir(n)}>
          {n}
        </Tecla>
      ))}
      <Tecla
        className="teclado-codigo__abc"
        onPointerDown={sinPerderFoco}
        onClick={alUsarSistema}
        aria-label="Usar el teclado del celular"
        title="Usar el teclado del celular"
      >
        ABC
      </Tecla>
      {['7', '8', '9'].map((n) => (
        <Tecla key={n} onPointerDown={sinPerderFoco} onClick={() => escribir(n)}>
          {n}
        </Tecla>
      ))}
      <Tecla
        className="teclado-codigo__agregar"
        onPointerDown={sinPerderFoco}
        onClick={alAgregar}
        disabled={deshabilitado}
        aria-label="Agregar producto"
      >
        <CornerDownLeft size={22} />
      </Tecla>
      <Tecla
        className="teclado-codigo__ocultar"
        onPointerDown={sinPerderFoco}
        onClick={alOcultar}
        aria-label="Ocultar teclado"
        title="Ocultar teclado"
      >
        <ChevronDown size={22} />
      </Tecla>
      <Tecla className="teclado-codigo__cero" onPointerDown={sinPerderFoco} onClick={() => escribir('0')}>
        0
      </Tecla>
    </div>
  )
}

function Tecla({ className, ...props }: ButtonHTMLAttributes<HTMLButtonElement>) {
  return <button type="button" tabIndex={-1} {...props} className={['teclado-codigo__tecla', className].filter(Boolean).join(' ')} />
}
