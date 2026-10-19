import { Router } from 'express'
import type { Pool } from 'mysql2/promise'
import { pendientes } from '../migraciones.ts'
import { version } from '../version.ts'

export type EstadoSalud = {
  estado: 'ok' | 'migraciones-pendientes' | 'sin-base-datos'
  version: string
  baseDatos: boolean
  migracionesPendientes: number
  hora: string
}

/**
 * Sin login: la web lo usa para saber si llega al servidor y si la base está al día.
 * No expone datos sensibles.
 */
export function rutasSalud(db: Pool) {
  const rutas = Router()
  rutas.get('/', async (_req, res) => {
    let estado: EstadoSalud
    try {
      const cantidad = (await pendientes(db)).length
      estado = { estado: cantidad === 0 ? 'ok' : 'migraciones-pendientes', version, baseDatos: true, migracionesPendientes: cantidad, hora: new Date().toISOString() }
    } catch (error) {
      console.error('Salud: no se pudo consultar la base.', error)
      estado = { estado: 'sin-base-datos', version, baseDatos: false, migracionesPendientes: 0, hora: new Date().toISOString() }
    }
    res.set('Cache-Control', 'no-store').json(estado)
  })
  return rutas
}
