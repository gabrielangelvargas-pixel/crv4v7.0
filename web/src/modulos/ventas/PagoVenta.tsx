import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { DollarSign, Plus, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { api } from '../../api/cliente'
import { Aviso, Boton, Campo, Casilla, Selector } from '../../ui/componentes'
import { moneda, numero, redondear, type Opcion, type VentaResumen } from './tipos'

interface MedioPago extends Opcion {
  requiereCuentaDestino: boolean
  requiereNumeroOperacion: boolean
  requiereAutorizacion: boolean
  requiereCuotas: boolean
}

interface Renglon {
  idMedioPago: string
  idCuentaCobro: string
  importe: string
  numeroOperacion: string
  autorizacion: string
  cuotas: string
}

export interface PagoRegistrado {
  id: number
  idFactura: number
  saldoVenta: number
  estadoVenta: string
}

/** El tope del redondeo lo controla la API ($100, como en Sistema03). */
export function PagoVenta({ venta, alTerminar }: { venta: VentaResumen; alTerminar: (pago?: PagoRegistrado) => void }) {
  const consultas = useQueryClient()
  const medios = useQuery({ queryKey: ['medios-pago'], queryFn: () => api.get<MedioPago[]>('/api/medios-pago') })
  const cuentas = useQuery({ queryKey: ['cuentas-cobro'], queryFn: () => api.get<Opcion[]>('/api/cuentas-cobro') })
  const saldo = redondear(Math.max(venta.total - venta.pagado, 0))
  const [renglones, setRenglones] = useState<Renglon[]>([nuevo(saldo)])
  const [ajustar, setAjustar] = useState(false)
  const total = renglones.reduce((t, r) => t + numero(r.importe), 0)
  const diferencia = redondear(total - saldo)
  const hayDiferencia = Math.abs(diferencia) >= 0.01
  const primerMedio = medios.data?.[0] ? String(medios.data[0].id) : ''

  const registrar = useMutation({
    mutationFn: () =>
      api.post<PagoRegistrado>('/api/pagos', {
        idFactura: venta.id,
        ajustarImporte: ajustar && hayDiferencia,
        detalles: renglones.map((r) => ({
          idMedioPago: Number(r.idMedioPago || primerMedio),
          idCuentaCobro: r.idCuentaCobro ? Number(r.idCuentaCobro) : null,
          importe: numero(r.importe),
          numeroOperacion: r.numeroOperacion || null,
          autorizacion: r.autorizacion || null,
          cuotas: r.cuotas ? Number(r.cuotas) : null,
        })),
      }),
    onSuccess: async (pago) => {
      await consultas.invalidateQueries({ queryKey: ['ventas'] })
      await consultas.invalidateQueries({ queryKey: ['clientes'] })
      alTerminar(pago)
    },
  })

  const cambiar = (indice: number, campo: keyof Renglon, valor: string) =>
    setRenglones((a) => a.map((r, i) => (i === indice ? { ...r, [campo]: valor } : r)))

  function enviar(e: FormEvent) {
    e.preventDefault()
    registrar.mutate()
  }

  return (
    <form className="formulario pago-venta" onSubmit={enviar}>
      <div className="pago-venta__resumen">
        <div>
          <span>Cliente</span>
          <strong>{venta.cliente.trim()}</strong>
        </div>
        <div>
          <span>Total de la venta</span>
          <strong>{moneda.format(venta.total)}</strong>
        </div>
        {venta.pagado > 0 && (
          <div>
            <span>Ya pagado</span>
            <strong>{moneda.format(venta.pagado)}</strong>
          </div>
        )}
        <div>
          <span>A registrar</span>
          <strong>{moneda.format(total)}</strong>
        </div>
        <div className={hayDiferencia ? 'pago-venta__diferencia pago-venta__diferencia--hay' : 'pago-venta__diferencia'}>
          <span>Diferencia</span>
          <strong>{moneda.format(Math.abs(diferencia))}</strong>
          <Casilla
            etiqueta="Ajustar importe"
            checked={ajustar && hayDiferencia}
            disabled={!hayDiferencia}
            onChange={(e) => setAjustar(e.target.checked)}
          />
        </div>
      </div>
      {ajustar && hayDiferencia && <Aviso tipo="info">Se agrega a la venta un renglón de redondeo por {moneda.format(diferencia)}.</Aviso>}

      {renglones.map((r, i) => {
        const medio = medios.data?.find((m) => String(m.id) === (r.idMedioPago || primerMedio))
        return (
          <div className="pago-venta__renglon" key={i}>
            <Selector
              etiqueta="Medio"
              value={r.idMedioPago || primerMedio}
              onChange={(e) => cambiar(i, 'idMedioPago', e.target.value)}
              required
            >
              {(medios.data ?? []).map((m) => (
                <option key={m.id} value={m.id}>
                  {m.nombre}
                </option>
              ))}
            </Selector>
            <Campo
              etiqueta="Importe"
              type="number"
              min="0.01"
              step="0.01"
              inputMode="decimal"
              value={r.importe}
              onChange={(e) => cambiar(i, 'importe', e.target.value)}
              required
            />
            {medio?.requiereCuentaDestino && (
              <Selector etiqueta="Cuenta" value={r.idCuentaCobro} onChange={(e) => cambiar(i, 'idCuentaCobro', e.target.value)} required>
                <option value="">Elegí</option>
                {(cuentas.data ?? []).map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nombre}
                  </option>
                ))}
              </Selector>
            )}
            {medio?.requiereNumeroOperacion && (
              <Campo
                etiqueta="Operación"
                value={r.numeroOperacion}
                onChange={(e) => cambiar(i, 'numeroOperacion', e.target.value)}
                maxLength={50}
                required
              />
            )}
            {medio?.requiereAutorizacion && (
              <Campo
                etiqueta="Autorización"
                value={r.autorizacion}
                onChange={(e) => cambiar(i, 'autorizacion', e.target.value)}
                maxLength={50}
                required
              />
            )}
            {medio?.requiereCuotas && (
              <Campo
                etiqueta="Cuotas"
                type="number"
                min="1"
                step="1"
                value={r.cuotas}
                onChange={(e) => cambiar(i, 'cuotas', e.target.value)}
                required
              />
            )}
            <Boton
              className="boton--icono pago-venta__quitar"
              onClick={() => setRenglones((a) => a.filter((_, j) => j !== i))}
              disabled={renglones.length === 1}
              aria-label="Quitar medio de pago"
            >
              <Trash2 size={15} />
            </Boton>
          </div>
        )
      })}

      <Aviso>{registrar.error?.message ?? medios.error?.message}</Aviso>
      <div className="formulario__acciones">
        <Boton className="pago-venta__agregar" onClick={() => setRenglones((a) => [...a, nuevo(Math.max(-diferencia, 0))])}>
          <Plus size={16} /> Agregar medio
        </Boton>
        <Boton onClick={() => alTerminar()}>Cancelar</Boton>
        <Boton type="submit" variante="primario" disabled={registrar.isPending || total <= 0}>
          <DollarSign size={16} /> {registrar.isPending ? 'Registrando…' : 'Registrar pago'}
        </Boton>
      </div>
    </form>
  )
}

const nuevo = (importe: number): Renglon => ({
  idMedioPago: '',
  idCuentaCobro: '',
  importe: importe > 0 ? String(importe) : '',
  numeroOperacion: '',
  autorizacion: '',
  cuotas: '',
})
