import { readFileSync } from 'node:fs'

/** La versión de package.json, la misma que muestra /api/salud. */
export const version: string = JSON.parse(readFileSync(new URL('../package.json', import.meta.url), 'utf8')).version
