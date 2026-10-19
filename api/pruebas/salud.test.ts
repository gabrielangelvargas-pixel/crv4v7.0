import type { Pool } from 'mysql2/promise'
import request from 'supertest'
import { afterAll, beforeAll, describe, expect, it } from 'vitest'
import { crearApp } from '../src/app.ts'
import { crearPool } from '../src/db.ts'
import { listarMigraciones, migrar, pendientes } from '../src/migraciones.ts'
import { configPrueba, prepararBase } from './base.ts'

const config = configPrueba()
let db: Pool

beforeAll(async () => {
  await prepararBase(config)
  db = crearPool(config.base)
})
afterAll(() => db?.end())

describe('migraciones y /api/salud', () => {
  it('avisa las migraciones pendientes en una base recién copiada de Sistema03', async () => {
    const res = await request(crearApp({ config, db })).get('/api/salud')
    expect(res.status).toBe(200)
    expect(res.body).toMatchObject({ estado: 'migraciones-pendientes', baseDatos: true, migracionesPendientes: (await listarMigraciones()).length })
  })

  it('migra, queda al día y una segunda corrida no hace nada', async () => {
    expect(await migrar(db)).toEqual((await listarMigraciones()).map((m) => m.id))
    expect(await pendientes(db)).toEqual([])
    expect(await migrar(db)).toEqual([])

    const [permisos] = await db.query<any[]>('SELECT COUNT(*) AS n FROM permisos')
    expect(permisos[0].n).toBe(27)
    const res = await request(crearApp({ config, db })).get('/api/salud')
    expect(res.body).toMatchObject({ estado: 'ok', version: '7.0.0', baseDatos: true, migracionesPendientes: 0 })
    expect(Date.parse(res.body.hora)).not.toBeNaN()
  })

  it('responde sin-base-datos si no llega a la base', async () => {
    const sinBase = crearPool({ ...config.base, puerto: 1 })
    try {
      const res = await request(crearApp({ config, db: sinBase })).get('/api/salud')
      expect(res.body).toMatchObject({ estado: 'sin-base-datos', baseDatos: false })
    } finally {
      await sinBase.end()
    }
  })
})

describe('CORS', () => {
  it('deja pasar a la web propia y a nadie más', async () => {
    const app = crearApp({ config, db })
    const propia = await request(app).get('/api/salud').set('Origin', 'https://desarrollos-gv.com.ar')
    expect(propia.headers['access-control-allow-origin']).toBe('https://desarrollos-gv.com.ar')
    const otra = await request(app).options('/api/salud').set('Origin', 'https://otro.com').set('Access-Control-Request-Method', 'GET')
    expect(otra.headers['access-control-allow-origin']).toBeUndefined()
  })

  it('una dirección que no existe da 404 en JSON', async () => {
    const res = await request(crearApp({ config, db })).get('/api/nada')
    expect(res.status).toBe(404)
    expect(res.body.mensaje).toBeTruthy()
  })
})
