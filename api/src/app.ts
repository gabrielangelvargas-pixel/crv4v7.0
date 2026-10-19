import cors from 'cors'
import express, { type ErrorRequestHandler } from 'express'
import helmet from 'helmet'
import type { Pool } from 'mysql2/promise'
import type { Config } from './config.ts'
import { rutasSalud } from './rutas/salud.ts'

export type Dependencias = { config: Config; db: Pool }

export function crearApp({ config, db }: Dependencias) {
  const app = express()
  // Hostinger atiende el HTTPS y pasa la consulta por un proxy: así req.ip es la IP real del celular.
  app.set('trust proxy', 1)
  app.disable('x-powered-by')
  app.use(helmet())
  // Solo la web propia puede llamar a la API desde el navegador.
  app.use(cors({ origin: config.origenesPermitidos, maxAge: 600 }))
  app.use(express.json({ limit: '1mb' }))

  app.use('/api/salud', rutasSalud(db))

  app.use((_req, res) => {
    res.status(404).json({ mensaje: 'No existe esa dirección.' })
  })
  const alFallar: ErrorRequestHandler = (error, _req, res, _next) => {
    console.error(error)
    res.status(500).json({ mensaje: 'Error inesperado en el servidor.' })
  }
  app.use(alFallar)
  return app
}
