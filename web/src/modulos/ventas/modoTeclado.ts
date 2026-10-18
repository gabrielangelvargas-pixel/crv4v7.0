import { useState } from 'react'

const CLAVE_MODO = 'crv4.teclado-codigo'

export type ModoTeclado = 'propio' | 'sistema'

/** Celular o tablet: la pantalla táctil es el puntero principal. */
export function esTactil() {
  return typeof window !== 'undefined' && window.matchMedia('(pointer: coarse)').matches
}

/** Modo elegido para el campo de código; se recuerda en el equipo. En táctiles arranca con el teclado propio. */
export function useModoTeclado() {
  const [tactil] = useState(esTactil)
  const [modo, setModo] = useState<ModoTeclado>(() => {
    try {
      return localStorage.getItem(CLAVE_MODO) === 'sistema' ? 'sistema' : 'propio'
    } catch {
      return 'propio'
    }
  })
  const cambiar = (nuevo: ModoTeclado) => {
    setModo(nuevo)
    try {
      localStorage.setItem(CLAVE_MODO, nuevo)
    } catch {
      // Sin almacenamiento: se usa el modo solo en esta pantalla.
    }
  }
  return { tactil, propio: tactil && modo === 'propio', cambiar }
}
