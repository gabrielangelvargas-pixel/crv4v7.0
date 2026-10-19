import mysql, { type Pool } from 'mysql2/promise'
import type { Config } from './config.ts'

/**
 * Conexión a MySQL o MariaDB (Hostinger usa MariaDB).
 * Las fechas se guardan y se leen en la hora local del servidor, como hace Sistema03.
 */
export function crearPool(base: Config['base']): Pool {
  return mysql.createPool({
    host: base.servidor,
    port: base.puerto,
    database: base.base,
    user: base.usuario,
    password: base.clave,
    charset: 'utf8mb4_unicode_ci',
    timezone: 'local',
    // DECIMAL y BIGINT llegan como número (los importes y códigos de este sistema entran en un double sin perder precisión).
    decimalNumbers: true,
    supportBigNumbers: true,
    bigNumberStrings: false,
    connectionLimit: 5,
    waitForConnections: true,
    enableKeepAlive: true,
  })
}
