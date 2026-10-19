# CRV4 Mayorista 7.0

Sistema de gestión interna de CRV4 Mayorista: productos, clientes, ventas, estadísticas, control de ventas, rubros y seguridad.

| Parte | Qué es | Dónde corre |
|---|---|---|
| `api/` | API Node.js (TypeScript + Express) + MySQL/MariaDB | Hostinger, app Node.js en `https://api.desarrollos-gv.com.ar` |
| `web/` | React 19 + Vite, instalable como PWA | Hostinger, `https://desarrollos-gv.com.ar` (se publica sola desde Git) |
| `database/` | Estructura de la base de Sistema03, para las pruebas | — |
| `docs/` | Documentación | — |

```
Celular o PC (cualquier red)                 Hostinger
┌──────────────────────────┐  HTTPS + CORS  ┌──────────────────────────────┐
│ Web PWA                  │ ─────────────▶ │ API Node.js                  │
│ desarrollos-gv.com.ar    │                │ api.desarrollos-gv.com.ar    │
└──────────────────────────┘                │        │                     │
                                            │        ▼                     │
                                            │ Base MariaDB (crv4_datos)    │
                                            └──────────────────────────────┘
```

## Estado

La API se está pasando de .NET a Node.js por etapas. La versión .NET quedó guardada en Git con la etiqueta `version-dotnet`.

| Etapa | Qué incluye | API Node.js |
|---|---|---|
| 1 | Base del proyecto, migraciones y `/api/salud` | ✅ |
| 2 | Login, sesiones y Seguridad | pendiente |
| 3 | Rubros, Productos e Inicio | pendiente |
| 4 | Clientes | pendiente |
| 5 | Ventas y Pagos | pendiente |
| 6 | La web usa un solo servidor | pendiente |

Las pantallas de la web ya están hechas para todos esos módulos (salvo WooCommerce, Estadísticas y Control Ventas).

## Documentación

- [Arquitectura y decisiones](docs/arquitectura.md)
- [Desarrollo](docs/desarrollo.md): cómo correr todo en tu PC.
- [Base de datos y migraciones](docs/base-de-datos.md)
- [Publicación en Hostinger](docs/hostinger.md): la web y la API.

## Reglas del proyecto

1. **Ninguna clave en el repositorio.** Van en las variables de entorno de Hostinger o en `api/.env.local` (no se sube).
2. **La estructura de la base solo cambia con migraciones** (`api/migraciones/*.sql`), aplicadas a mano con `npm run migrar`. La API nunca la cambia al arrancar.
3. **Los números de venta y de cliente los da la base** (`numeradores`), nunca `MAX(id)+1`.
4. **Un módulo, una carpeta**, tanto en la API (`src/modulos/<modulo>`) como en la web (`src/modulos/<modulo>`).
5. La base es `crv4_datos`, compartida con Sistema03: las migraciones solo agregan (ver `docs/base-de-datos.md`).
