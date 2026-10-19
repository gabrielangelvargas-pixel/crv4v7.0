import { readFile } from 'node:fs/promises'
import mysql from 'mysql2/promise'
import type { Config } from '../src/config.ts'

/**
 * Base de prueba: CRV4_TEST_MYSQL=mysql://usuario:clave@servidor:3306/crv4_test (el nombre tiene que terminar en _test).
 * Cada corrida la borra y la arma de nuevo con la estructura de Sistema03 (database/crv4_datos-estructura.sql).
 */
export function configPrueba(): Config {
  const texto = process.env.CRV4_TEST_MYSQL
  if (!texto) throw new Error('Para las pruebas falta CRV4_TEST_MYSQL (por ejemplo mysql://usuario:clave@localhost:3306/crv4_test).')
  const url = new URL(texto)
  const base = decodeURIComponent(url.pathname.slice(1))
  if (!base.endsWith('_test')) throw new Error(`La base de prueba tiene que terminar en _test (es "${base}"): se borra en cada corrida.`)
  return {
    puerto: 0,
    base: { servidor: url.hostname, puerto: Number(url.port || 3306), base, usuario: decodeURIComponent(url.username), clave: decodeURIComponent(url.password) },
    origenesPermitidos: ['https://desarrollos-gv.com.ar'],
  }
}

export async function prepararBase(config: Config) {
  const { servidor, puerto, base, usuario, clave } = config.base
  const conexion = await mysql.createConnection({ host: servidor, port: puerto, user: usuario, password: clave, multipleStatements: true })
  try {
    await conexion.query(`DROP DATABASE IF EXISTS \`${base}\``)
    await conexion.query(`CREATE DATABASE \`${base}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci`)
    await conexion.query(`USE \`${base}\``)
    await conexion.query(await readFile(new URL('../../database/crv4_datos-estructura.sql', import.meta.url), 'utf8'))
  } finally {
    await conexion.end()
  }
}
