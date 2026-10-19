import { crearApp } from './app.ts'
import { leerConfig } from './config.ts'
import { crearPool } from './db.ts'
import { pendientes } from './migraciones.ts'
import { version } from './version.ts'

const config = leerConfig()
const db = crearPool(config.base)

const servidor = crearApp({ config, db }).listen(config.puerto, () => {
  console.log(`CRV4 API ${version} escuchando en el puerto ${config.puerto}.`)
})

// Avisa en el registro si falta migrar; la web lo muestra con /api/salud.
pendientes(db)
  .then((faltan) => {
    if (faltan.length > 0) console.warn(`Hay ${faltan.length} migraciones pendientes. Corré "npm run migrar".`)
  })
  .catch((error: unknown) => console.error('No se pudo conectar a la base de datos.', error))

function cerrar() {
  servidor.close(() => void db.end().finally(() => process.exit(0)))
}
process.on('SIGTERM', cerrar)
process.on('SIGINT', cerrar)
