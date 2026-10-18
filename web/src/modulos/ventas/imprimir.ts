import { fechaHora, moneda, type Venta } from './tipos'

const LINEAS_PRIMERA = 38
const LINEAS_SIGUIENTES = 48

/** Comprobante A4 igual al de Sistema03: se abre en una ventana nueva y llama a imprimir. */
export function imprimirVenta(venta: Venta) {
  const ventana = window.open('', '_blank', 'width=900,height=700')
  if (!ventana) {
    window.alert('No se pudo abrir la ventana de impresión. Revisá si el navegador bloqueó las ventanas emergentes.')
    return
  }
  ventana.document.write(htmlComprobante(venta))
  ventana.document.close()
}

export function htmlComprobante(venta: Venta) {
  const items = [...venta.items].sort((a, b) =>
    soloDigitos(a.idProducto).localeCompare(soloDigitos(b.idProducto), 'es-AR', { numeric: true }),
  )
  const unidades = items.reduce((t, i) => t + i.cantidad, 0)
  const bruto = items.reduce((t, i) => t + (i.subtotalBrutoVenta ?? i.subTotalVenta), 0)
  const participan = venta.subtotalProveedoresBonificados
  const noParticipan = Math.max(bruto - participan, 0)
  const linea = (texto: string, importe: number, negativo = false) =>
    `<span>${texto}</span><strong>${negativo ? '-' : ''}${moneda.format(importe)}</strong>`
  const resumen = [
    participan > 0 ? linea('Productos participan 02 y 46', participan) : '',
    noParticipan > 0 ? linea('Productos no participan', noParticipan) : '',
    venta.descuentoEscalon1 > 0 ? linea('Descuento 6%', venta.descuentoEscalon1, true) : '',
    venta.descuentoEscalon2 > 0 ? linea('Descuento adicional 5%', venta.descuentoEscalon2, true) : '',
    venta.descuentoEscalon3 > 0 ? linea('Descuento adicional 5%', venta.descuentoEscalon3, true) : '',
    venta.descuentoBono > 0 ? linea('Descuento bono', venta.descuentoBono, true) : '',
  ].join('')

  const paginas = [items.slice(0, LINEAS_PRIMERA)]
  for (let i = LINEAS_PRIMERA; i < items.length; i += LINEAS_SIGUIENTES) paginas.push(items.slice(i, i + LINEAS_SIGUIENTES))

  const html = paginas
    .map((renglones, indice) => {
      const primera = indice === 0
      const ultima = indice === paginas.length - 1
      const filas = renglones
        .map(
          (i) => `
      <tr>
        <td class="code">${escaparHtml(codigoImpreso(i.idProducto))}</td>
        <td class="number qty">${i.cantidad.toLocaleString('es-AR')}</td>
        <td class="description">${escaparHtml(i.descripcion ?? '')}</td>
        <td class="number">${moneda.format(i.precioVenta)}</td>
        <td class="number">${moneda.format(i.subTotalVenta)}</td>
      </tr>`,
        )
        .join('')
      const cabecera = primera
        ? `
          <header>
            <div>
              <div class="brand">CRV4 Mayorista</div>
              <h1>Comprobante de venta</h1>
            </div>
            <div class="sale-meta">
              <strong>Venta ${venta.id}</strong>
              <span>${escaparHtml(fechaHora(venta.fecha))}</span>
              <span>${escaparHtml(venta.vendedor)} · ${escaparHtml(venta.estado)}</span>
            </div>
          </header>
          <section class="client-box">
            <div><span>Cliente</span><strong>${escaparHtml(venta.cliente)}</strong></div>
            <div><span>Código</span><strong>${venta.idCliente}</strong></div>
            <div><span>Página</span><strong>${indice + 1} / ${paginas.length}</strong></div>
          </section>`
        : `
          <div class="continuation-head">
            <strong>Venta ${venta.id}</strong>
            <span>Página ${indice + 1} / ${paginas.length}</span>
          </div>`
      const pie = ultima
        ? `
            <tr class="total-row">
              <td colspan="5">
                <div class="total-box">
                  <div class="total-summary">
                    <span>Productos: ${items.length.toLocaleString('es-AR')}</span>
                    <span>Unidades: ${unidades.toLocaleString('es-AR')}</span>
                  </div>
                  <div class="discount-summary">${resumen}</div>
                  <span class="total-label">Total venta</span>
                  <strong>${moneda.format(venta.totalVenta)}</strong>
                </div>
              </td>
            </tr>`
        : ''
      return `
      <section class="print-page ${primera ? '' : 'continuation-page'}">
        ${cabecera}
        <table>
          <thead>
            <tr><th>Código</th><th class="number">Cant.</th><th>Descripción</th><th class="number">Precio</th><th class="number">Subtotal</th></tr>
          </thead>
          <tbody>${filas}${pie}</tbody>
        </table>
      </section>`
    })
    .join('')

  return `<!doctype html>
<html lang="es">
  <head>
    <meta charset="utf-8" />
    <title>Venta ${venta.id} · CRV4 Mayorista</title>
    <style>
      @page { size: A4 portrait; margin: 8mm; }
      * { box-sizing: border-box; }
      body {
        margin: 0;
        color: #162629;
        background: #ffffff;
        font-family: Arial, Helvetica, sans-serif;
        font-size: 11px;
      }
      .print-page {
        min-height: 281mm;
        display: grid;
        grid-template-rows: auto auto auto;
        align-content: start;
        page-break-after: always;
      }
      .continuation-page {
        padding-top: 0;
      }
      .print-page:last-child { page-break-after: auto; }
      header {
        display: flex;
        justify-content: space-between;
        gap: 24px;
        padding-bottom: 7px;
        border-bottom: 2px solid #174b58;
      }
      .brand {
        color: #174b58;
        font-size: 12px;
        font-weight: 800;
        letter-spacing: 0;
        text-transform: uppercase;
      }
      h1 {
        margin: 2px 0 0;
        font-size: 20px;
        line-height: 1.1;
      }
      .sale-meta {
        display: grid;
        gap: 3px;
        align-content: start;
        min-width: 170px;
        text-align: right;
      }
      .sale-meta span,
      .client-box span {
        color: #607174;
        font-size: 10px;
      }
      .client-box {
        display: grid;
        grid-template-columns: minmax(0, 1fr) 92px 82px;
        gap: 8px;
        margin: 7px 0;
      }
      .client-box div {
        display: grid;
        gap: 2px;
        padding: 6px 8px;
        border: 1px solid #c9d8d6;
        border-radius: 6px;
        background: #f4f8f7;
      }
      .client-box strong {
        font-size: 13px;
        color: #000;
        font-weight: 400;
      }
      .sale-meta strong {
        color: #000;
        font-weight: 400;
      }
      .continuation-head {
        display: flex;
        justify-content: space-between;
        gap: 12px;
        margin-bottom: 5px;
        padding-bottom: 5px;
        border-bottom: 1px solid #c9d8d6;
        color: #607174;
        font-size: 10px;
      }
      table {
        width: 100%;
        border-collapse: collapse;
        table-layout: fixed;
        border: 1px solid #c9d8d6;
      }
      th,
      td {
        height: 4.8mm;
        padding: 1px 5px;
        border-bottom: 1px solid #d7e2e0;
        vertical-align: middle;
        text-align: left;
      }
      th {
        height: 5.4mm;
        color: #174b58;
        background: #eaf2f1;
        font-size: 10px;
        font-weight: 800;
        text-transform: uppercase;
      }
      th:nth-child(1), td:nth-child(1) { width: 78px; }
      th:nth-child(2), td:nth-child(2) { width: 44px; }
      th:nth-child(4), td:nth-child(4) { width: 82px; }
      th:nth-child(5), td:nth-child(5) { width: 92px; }
      .code {
        color: #000;
        font-family: Consolas, 'Courier New', monospace;
        font-weight: 400;
        white-space: nowrap;
      }
      .description {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        font-weight: 400;
      }
      .number {
        text-align: right;
        white-space: nowrap;
      }
      tbody td {
        color: #000;
        font-weight: 400;
      }
      .qty {
        text-align: center;
      }
      .total-row td {
        height: auto;
        padding: 0;
        border-top: 2px solid #174b58;
        border-bottom: 0;
        background: #f4f8f7;
        color: #000;
        font-weight: 400;
      }
      .total-box {
        display: grid;
        grid-template-columns: minmax(0, 1fr) auto;
        align-items: center;
        gap: 3px 14px;
        width: 100%;
        padding: 4px 8px;
      }
      .total-summary {
        grid-column: 1 / -1;
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        color: #000;
        font-size: 10px;
        font-weight: 400;
      }
      .discount-summary {
        grid-column: 1 / -1;
        display: grid;
        grid-template-columns: minmax(0, 1fr) auto;
        gap: 1px 16px;
        padding-top: 3px;
        border-top: 1px solid #c8d7d5;
        color: #000;
        font-size: 10px;
        font-weight: 400;
        line-height: 1.2;
      }
      .discount-summary strong {
        font-size: 10px;
        text-align: right;
        white-space: nowrap;
        color: #000;
        font-weight: 400;
      }
      .total-label {
        color: #000;
        font-size: 11px;
        text-align: right;
        white-space: nowrap;
        font-weight: 400;
      }
      .total-box > strong {
        padding-right: 3px;
        font-size: 15px;
        text-align: right;
        white-space: nowrap;
        color: #000;
        font-weight: 400;
      }
      @media screen {
        body { background: #e8eeee; padding: 16px; }
        .print-page {
          width: 210mm;
          min-height: 281mm;
          margin: 0 auto 16px;
          padding: 8mm;
          background: #ffffff;
          box-shadow: 0 8px 30px rgba(15, 23, 42, 0.18);
        }
      }
    </style>
  </head>
  <body>
    ${html}
    <script>window.addEventListener('load', () => window.print())</script>
  </body>
</html>`
}

const soloDigitos = (codigo: string) => codigo.replace(/\D/g, '') || codigo

/** Los códigos de 8 dígitos se imprimen como 1234-5678. */
function codigoImpreso(codigo: string) {
  const digitos = codigo.replace(/\D/g, '')
  return digitos.length === 8 ? `${digitos.slice(0, 4)}-${digitos.slice(4)}` : codigo
}

const escaparHtml = (texto: string) =>
  texto.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#039;')
