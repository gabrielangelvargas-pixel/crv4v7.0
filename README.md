# CRV4 Mayorista 7.0

Sistema de gestión interna de CRV4 Mayorista: productos, clientes, ventas, estadísticas, control de ventas, rubros y seguridad.

| Parte | Qué es | Dónde corre |
|---|---|---|
| `api/` | API ASP.NET Core (.NET 10) + EF Core + MySQL | PC servidor de la red local, como servicio de Windows con HTTPS |
| `web/` | React 19 + Vite, instalable como PWA | Hostinger (se publica sola desde Git) |
| `deploy/` | Scripts de instalación del servidor | PC servidor |
| `docs/` | Documentación | — |

```
Navegador (cualquier PC)                          PC servidor (red local)
┌──────────────────────────┐   HTTPS + CORS    ┌────────────────────────┐
│ Web PWA desde Hostinger  │ ───────────────▶  │ CRV4Api (servicio)     │
│ https://<dominio>        │  config.json dice │ https://192.168.100.128│
└──────────────────────────┘  a dónde llamar   │ :5443  ──▶  MySQL      │
                                               └────────────────────────┘
```

## Estado

| Módulo | API | Web |
|---|---|---|
| Login, sesiones, cambio de contraseña | ✅ | ✅ |
| Seguridad (usuarios, roles, permisos) | ✅ | ✅ |
| Rubros | ✅ | ✅ |
| Inicio (tarjetas) | ✅ parcial | ✅ |
| Productos, Clientes, Ventas, Estadísticas, Control Ventas | pendiente: falta definir el esquema a partir de `crv4_datos` | pantalla "en construcción" |

## Documentación

- [Arquitectura y decisiones](docs/arquitectura.md): por qué está armado así (y qué se corrigió respecto de Sistema03).
- [Desarrollo](docs/desarrollo.md): cómo correr todo en tu PC.
- [Base de datos y migraciones](docs/base-de-datos.md)
- [Instalación del servidor](docs/servidor.md)
- [Publicación de la web en Hostinger](docs/hostinger.md)

## Reglas del proyecto

1. **Ninguna clave en el repositorio.** Ni en `appsettings.json`, ni en scripts, ni en archivos `.http`.
2. **La estructura de la base solo cambia con migraciones** (`dotnet ef migrations add ...`), aplicadas a mano con `Aplicar-Migraciones.ps1`. La API nunca la cambia al arrancar.
3. **Los números de venta y de cliente los da la base** (`numeradores`), nunca `MAX(id)+1`.
4. **Un módulo, una carpeta**, tanto en la API (`Modulos/<Modulo>`) como en la web (`src/modulos/<modulo>`).
5. Cada permiso nuevo se agrega en `Permisos.cs` y en `web/src/auth/permisos.ts`, con una migración.
