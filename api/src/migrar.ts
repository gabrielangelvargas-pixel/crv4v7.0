import { leerConfig } from './config.ts'
import { crearPool } from './db.ts'
import { migrar, pendientes } from './migraciones.ts'

/** `npm run migrar`: muestra y aplica los cambios de estructura pendientes. Con --ver solo los muestra. */
const config = leerConfig()
const db = crearPool(config.base)
try {
  const faltan = await pendientes(db)
  console.log(`Base ${config.base.base} en ${config.base.servidor}: ${faltan.length} migraciones pendientes.`)
  for (const m of faltan) console.log(`  ${m.id}`)
  if (faltan.length > 0 && !process.argv.includes('--ver')) {
    await migrar(db, (texto) => console.log(texto))
    console.log('Base al día.')
  }
} catch (error) {
  console.error('No se pudo migrar:', error instanceof Error ? error.message : error)
  process.exitCode = 1
} finally {
  await db.end()
}
