import { z } from 'zod'

/**
 * Configuración de la API. Todo sale de variables de entorno con prefijo CRV4_:
 * en Hostinger se cargan en el panel de la app Node.js; en una PC, en api/.env.local (nunca va al repositorio).
 */
const esquema = z.object({
  PORT: z.coerce.number().int().positive().default(3000),
  CRV4_DB_SERVIDOR: z.string().min(1).default('localhost'),
  CRV4_DB_PUERTO: z.coerce.number().int().positive().default(3306),
  CRV4_DB_BASE: z.string().min(1),
  CRV4_DB_USUARIO: z.string().min(1),
  CRV4_DB_CLAVE: z.string(),
  CRV4_CORS_ORIGENES: z
    .string()
    .default('')
    .transform((texto) =>
      texto
        .split(',')
        .map((origen) => origen.trim().replace(/\/+$/, ''))
        .filter(Boolean),
    ),
})

export type Config = {
  puerto: number
  base: { servidor: string; puerto: number; base: string; usuario: string; clave: string }
  origenesPermitidos: string[]
}

export function leerConfig(entorno: NodeJS.ProcessEnv = process.env): Config {
  const resultado = esquema.safeParse(entorno)
  if (!resultado.success) {
    const faltan = resultado.error.issues.map((i) => i.path.join('.')).join(', ')
    throw new Error(`Faltan o son inválidas estas variables de entorno: ${faltan}`)
  }
  const v = resultado.data
  return {
    puerto: v.PORT,
    base: { servidor: v.CRV4_DB_SERVIDOR, puerto: v.CRV4_DB_PUERTO, base: v.CRV4_DB_BASE, usuario: v.CRV4_DB_USUARIO, clave: v.CRV4_DB_CLAVE },
    origenesPermitidos: v.CRV4_CORS_ORIGENES,
  }
}
