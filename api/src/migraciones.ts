import { readdir, readFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import type { Pool, PoolConnection, RowDataPacket } from 'mysql2/promise'

/**
 * Cambios de estructura de la base: archivos .sql numerados en api/migraciones (AAAAMMDDhhmmss_Nombre.sql).
 * Solo AGREGAN: la base crv4_datos la sigue usando Sistema03.
 * Se aplican a propósito con `npm run migrar`; la API nunca los aplica sola al arrancar.
 * El historial es la tabla crv4_migraciones, la misma que usaba la versión .NET.
 */
const carpeta = fileURLToPath(new URL('../migraciones/', import.meta.url))
const BLOQUEO = 'crv4_migraciones'

export type Migracion = { id: string; archivo: string }

export async function listarMigraciones(): Promise<Migracion[]> {
  const archivos = (await readdir(carpeta)).filter((a) => /^\d{14}_\w+\.sql$/.test(a)).sort()
  return archivos.map((archivo) => ({ id: archivo.replace(/\.sql$/, ''), archivo }))
}

async function aplicadas(db: Pool | PoolConnection): Promise<Set<string>> {
  const [tablas] = await db.query<RowDataPacket[]>(
    "SELECT 1 FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'crv4_migraciones'",
  )
  if (tablas.length === 0) return new Set()
  const [filas] = await db.query<RowDataPacket[]>('SELECT MigrationId FROM crv4_migraciones')
  return new Set(filas.map((f) => String(f.MigrationId)))
}

export async function pendientes(db: Pool): Promise<Migracion[]> {
  const hechas = await aplicadas(db)
  return (await listarMigraciones()).filter((m) => !hechas.has(m.id))
}

/** Separa un archivo en sentencias por los ";" de fin de línea (las migraciones no usan procedimientos ni DELIMITER). */
export function sentencias(sql: string): string[] {
  return sql
    .replace(/^\s*--.*$/gm, '')
    .split(/;\s*$/m)
    .map((s) => s.trim())
    .filter(Boolean)
}

/** Aplica las pendientes en orden. Devuelve las que aplicó. */
export async function migrar(db: Pool, aviso: (texto: string) => void = () => {}): Promise<string[]> {
  const conexion = await db.getConnection()
  try {
    // Un solo "migrar" a la vez. GET_LOCK funciona igual en MySQL y en MariaDB con una espera positiva.
    const [bloqueo] = await conexion.query<RowDataPacket[]>('SELECT GET_LOCK(?, 3600) AS ok', [BLOQUEO])
    if (Number(bloqueo[0]?.ok) !== 1) throw new Error('No se pudo bloquear la base para migrar: puede haber otra migración en curso.')
    try {
      await conexion.query(
        'CREATE TABLE IF NOT EXISTS crv4_migraciones (MigrationId varchar(150) NOT NULL, ProductVersion varchar(32) NOT NULL, PRIMARY KEY (MigrationId)) CHARACTER SET utf8mb4',
      )
      const hechas = await aplicadas(conexion)
      const aplicar = (await listarMigraciones()).filter((m) => !hechas.has(m.id))
      for (const m of aplicar) {
        aviso(`Aplicando ${m.id}...`)
        // MySQL confirma solo cada ALTER/CREATE: si una falla a la mitad, se corrige el archivo y se vuelve a correr.
        for (const sentencia of sentencias(await readFile(carpeta + m.archivo, 'utf8'))) await conexion.query(sentencia)
        await conexion.query('INSERT INTO crv4_migraciones (MigrationId, ProductVersion) VALUES (?, ?)', [m.id, 'crv4-node'])
      }
      return aplicar.map((m) => m.id)
    } finally {
      await conexion.query('SELECT RELEASE_LOCK(?)', [BLOQUEO])
    }
  } finally {
    conexion.release()
  }
}
