export interface Rubro {
  id: number
  codigo: string
  nombre: string
  descripcion: string | null
  idPadre: number | null
  nombrePadre: string | null
  orden: number
  activo: boolean
  productos: number
}

export interface Nodo {
  rubro: Rubro
  hijos: Nodo[]
}

/**
 * Árbol de rubros: cada padre con sus hijos, y cada hijo con sus nietos.
 * Entre hermanos ordena por Orden (los de Orden 0, sin asignar, van al final) y luego por nombre.
 * Un rubro cuyo padre no está en la lista va como raíz.
 */
export function armarArbol(rubros: Rubro[]): Nodo[] {
  const ids = new Set(rubros.map((r) => r.id))
  const hijos = new Map<number | null, Rubro[]>()
  const clave = (orden: number) => (orden === 0 ? Number.MAX_SAFE_INTEGER : orden)
  for (const r of rubros) {
    const padre = r.idPadre != null && ids.has(r.idPadre) ? r.idPadre : null
    hijos.set(padre, [...(hijos.get(padre) ?? []), r])
  }
  for (const grupo of hijos.values()) grupo.sort((a, b) => clave(a.orden) - clave(b.orden) || a.nombre.localeCompare(b.nombre, 'es'))

  const vistos = new Set<number>()
  const armar = (padre: number | null): Nodo[] =>
    (hijos.get(padre) ?? [])
      .filter((r) => !vistos.has(r.id) && vistos.add(r.id))
      .map((r) => ({ rubro: r, hijos: armar(r.id) }))
  const raices = armar(null)
  // Si hubiera un ciclo en los datos, esos rubros no cuelgan de ninguna raíz: se agregan al final.
  for (const r of rubros) if (!vistos.has(r.id)) raices.push(...armar(r.idPadre))
  return raices
}

/** Deja los rubros que coinciden con la búsqueda, con sus descendientes, y los padres que llevan hasta ellos. */
export function filtrarArbol(nodos: Nodo[], filtro: string): Nodo[] {
  if (!filtro) return nodos
  return nodos.flatMap((n) => {
    if (n.rubro.nombre.toLowerCase().includes(filtro) || n.rubro.codigo.toLowerCase().includes(filtro)) return [n]
    const hijos = filtrarArbol(n.hijos, filtro)
    return hijos.length ? [{ ...n, hijos }] : []
  })
}

export function aplanar(nodos: Nodo[], nivel = 0): { rubro: Rubro; nivel: number }[] {
  return nodos.flatMap((n) => [{ rubro: n.rubro, nivel }, ...aplanar(n.hijos, nivel + 1)])
}
