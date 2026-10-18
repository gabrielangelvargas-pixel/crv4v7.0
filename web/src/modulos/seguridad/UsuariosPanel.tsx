import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { KeyRound, Pencil, Plus, Unlock } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { api } from '../../api/cliente'
import { useSesion } from '../../auth/SesionContexto'
import { Aviso, Boton, Campo, Casilla, EstadoActivo, Modal, Selector, Tarjeta } from '../../ui/componentes'
import type { RolDto, UsuarioDto } from './tipos'

type Dialogo = { tipo: 'nuevo' } | { tipo: 'editar'; usuario: UsuarioDto } | { tipo: 'password'; usuario: UsuarioDto } | null

export function UsuariosPanel() {
  const cliente = useQueryClient()
  const { usuario: yo } = useSesion()
  const [dialogo, setDialogo] = useState<Dialogo>(null)
  const usuarios = useQuery({ queryKey: ['usuarios'], queryFn: () => api.get<UsuarioDto[]>('/api/seguridad/usuarios') })
  const roles = useQuery({ queryKey: ['roles'], queryFn: () => api.get<RolDto[]>('/api/seguridad/roles') })
  const desbloquear = useMutation({
    mutationFn: (id: number) => api.post(`/api/seguridad/usuarios/${id}/desbloquear`),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['usuarios'] }),
  })
  const cerrar = () => setDialogo(null)

  return (
    <Tarjeta
      titulo="Usuarios"
      acciones={
        <Boton variante="primario" onClick={() => setDialogo({ tipo: 'nuevo' })}>
          <Plus size={16} /> Nuevo usuario
        </Boton>
      }
    >
      <Aviso>{usuarios.error?.message ?? desbloquear.error?.message}</Aviso>
      <div className="tabla-contenedor">
        <table className="tabla">
          <thead>
            <tr>
              <th>Usuario</th>
              <th>Nombre</th>
              <th>Rol</th>
              <th>Estado</th>
              <th>Último ingreso</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {usuarios.data?.map((u) => (
              <tr key={u.id}>
                <td>{u.nombreUsuario}</td>
                <td>{u.nombre}</td>
                <td>{u.rol}</td>
                <td>
                  <EstadoActivo activo={u.activo} /> {u.bloqueado && <span className="etiqueta etiqueta--aviso">Bloqueado</span>}{' '}
                  {u.debeCambiarPassword && <span className="etiqueta etiqueta--aviso">Cambio de clave pendiente</span>}
                </td>
                <td>{u.ultimoIngreso ? new Date(u.ultimoIngreso).toLocaleString('es-AR') : 'Nunca'}</td>
                <td className="acciones">
                  {u.bloqueado && (
                    <Boton chico onClick={() => desbloquear.mutate(u.id)}>
                      <Unlock size={14} /> Desbloquear
                    </Boton>
                  )}{' '}
                  <Boton chico onClick={() => setDialogo({ tipo: 'password', usuario: u })}>
                    <KeyRound size={14} /> Clave
                  </Boton>{' '}
                  <Boton chico onClick={() => setDialogo({ tipo: 'editar', usuario: u })}>
                    <Pencil size={14} /> Editar
                  </Boton>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {usuarios.isPending && <div className="vacio">Cargando…</div>}
      </div>

      <Modal titulo="Nuevo usuario" abierto={dialogo?.tipo === 'nuevo'} alCerrar={cerrar}>
        <FormularioNuevo roles={roles.data ?? []} alTerminar={cerrar} />
      </Modal>
      <Modal titulo="Editar usuario" abierto={dialogo?.tipo === 'editar'} alCerrar={cerrar}>
        {dialogo?.tipo === 'editar' && <FormularioEditar usuario={dialogo.usuario} esYo={dialogo.usuario.id === yo?.id} roles={roles.data ?? []} alTerminar={cerrar} />}
      </Modal>
      <Modal titulo="Reiniciar contraseña" abierto={dialogo?.tipo === 'password'} alCerrar={cerrar}>
        {dialogo?.tipo === 'password' && <FormularioPassword usuario={dialogo.usuario} alTerminar={cerrar} />}
      </Modal>
    </Tarjeta>
  )
}

function useGuardar<T>(fn: (datos: T) => Promise<unknown>, alTerminar: () => void) {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: async () => {
      await cliente.invalidateQueries({ queryKey: ['usuarios'] })
      await cliente.invalidateQueries({ queryKey: ['roles'] })
      alTerminar()
    },
  })
}

function FormularioNuevo({ roles, alTerminar }: { roles: RolDto[]; alTerminar: () => void }) {
  const [datos, setDatos] = useState({ nombreUsuario: '', nombre: '', rolId: (roles.find((r) => !r.esSistema) ?? roles[0])?.id ?? 0, passwordInicial: '' })
  const guardar = useGuardar((d: typeof datos) => api.post('/api/seguridad/usuarios', d), alTerminar)
  const enviar = (e: FormEvent) => {
    e.preventDefault()
    guardar.mutate(datos)
  }
  return (
    <form className="formulario" onSubmit={enviar}>
      <Aviso>{guardar.error?.message}</Aviso>
      <Campo etiqueta="Usuario" value={datos.nombreUsuario} onChange={(e) => setDatos({ ...datos, nombreUsuario: e.target.value })} maxLength={50} required autoFocus />
      <Campo etiqueta="Nombre" value={datos.nombre} onChange={(e) => setDatos({ ...datos, nombre: e.target.value })} maxLength={120} required />
      <Selector etiqueta="Rol" value={datos.rolId} onChange={(e) => setDatos({ ...datos, rolId: Number(e.target.value) })}>
        {roles.map((r) => (
          <option key={r.id} value={r.id}>
            {r.nombre}
          </option>
        ))}
      </Selector>
      <Campo
        etiqueta="Contraseña inicial"
        type="password"
        value={datos.passwordInicial}
        onChange={(e) => setDatos({ ...datos, passwordInicial: e.target.value })}
        autoComplete="new-password"
        required
      />
      <Aviso tipo="info">El usuario va a tener que cambiarla en su primer ingreso.</Aviso>
      <Acciones pendiente={guardar.isPending} alCancelar={alTerminar} />
    </form>
  )
}

function FormularioEditar({ usuario, esYo, roles, alTerminar }: { usuario: UsuarioDto; esYo: boolean; roles: RolDto[]; alTerminar: () => void }) {
  const [datos, setDatos] = useState({ nombre: usuario.nombre, rolId: usuario.rolId, activo: usuario.activo })
  const guardar = useGuardar((d: typeof datos) => api.put(`/api/seguridad/usuarios/${usuario.id}`, d), alTerminar)
  const enviar = (e: FormEvent) => {
    e.preventDefault()
    guardar.mutate(datos)
  }
  return (
    <form className="formulario" onSubmit={enviar}>
      <Aviso>{guardar.error?.message}</Aviso>
      <Campo etiqueta="Usuario" value={usuario.nombreUsuario} disabled />
      <Campo etiqueta="Nombre" value={datos.nombre} onChange={(e) => setDatos({ ...datos, nombre: e.target.value })} maxLength={120} required autoFocus />
      <Selector etiqueta="Rol" value={datos.rolId} disabled={esYo} onChange={(e) => setDatos({ ...datos, rolId: Number(e.target.value) })}>
        {roles.map((r) => (
          <option key={r.id} value={r.id}>
            {r.nombre}
          </option>
        ))}
      </Selector>
      <Casilla etiqueta="Activo" checked={datos.activo} disabled={esYo} onChange={(e) => setDatos({ ...datos, activo: e.target.checked })} />
      {(datos.rolId !== usuario.rolId || datos.activo !== usuario.activo) && (
        <Aviso tipo="info">Al guardar se cierran las sesiones abiertas de este usuario.</Aviso>
      )}
      <Acciones pendiente={guardar.isPending} alCancelar={alTerminar} />
    </form>
  )
}

function FormularioPassword({ usuario, alTerminar }: { usuario: UsuarioDto; alTerminar: () => void }) {
  const [passwordTemporal, setPassword] = useState('')
  const guardar = useGuardar(() => api.post(`/api/seguridad/usuarios/${usuario.id}/reiniciar-password`, { passwordTemporal }), alTerminar)
  const enviar = (e: FormEvent) => {
    e.preventDefault()
    guardar.mutate(undefined)
  }
  return (
    <form className="formulario" onSubmit={enviar}>
      <Aviso>{guardar.error?.message}</Aviso>
      <p>
        Nueva contraseña temporal para <strong>{usuario.nombre}</strong>. Se cierran sus sesiones y la tiene que cambiar al ingresar.
      </p>
      <Campo etiqueta="Contraseña temporal" type="password" value={passwordTemporal} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" required autoFocus />
      <Acciones pendiente={guardar.isPending} alCancelar={alTerminar} />
    </form>
  )
}

function Acciones({ pendiente, alCancelar }: { pendiente: boolean; alCancelar: () => void }) {
  return (
    <div className="formulario__acciones">
      <Boton onClick={alCancelar}>Cancelar</Boton>
      <Boton type="submit" variante="primario" disabled={pendiente}>
        {pendiente ? 'Guardando…' : 'Guardar'}
      </Boton>
    </div>
  )
}
