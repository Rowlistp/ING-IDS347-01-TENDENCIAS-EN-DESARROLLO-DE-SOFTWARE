/**
 * Utilidades para descarga de archivos con nombres incrementales únicos
 * y generación de comprobantes oficiales impresos para FuelTrack.
 */

/**
 * Genera un nombre de archivo único con número incremental secuencial
 * para evitar que los archivos descargados se solapen o sobreescriban.
 *
 * Ejemplo para Cierre Diario con fecha 2026-09-18 e ID 3:
 * 1ª descarga: cierre-diario-2026-09-18-ID3-01.pdf
 * 2ª descarga: cierre-diario-2026-09-18-ID3-02.pdf
 *
 * Ejemplo para Reportes:
 * 1ª descarga: reporte-despachos-2026-09-18-01.pdf
 * 2ª descarga: reporte-despachos-2026-09-18-02.pdf
 *
 * @param {string} basePrefix - Prefijo del archivo (ej: "cierre-diario-2026-09-18-ID1")
 * @param {string} extension - Extensión sin punto (ej: "pdf", "xlsx", "csv", "html")
 * @returns {string} Nombre de archivo secuencial
 */
export function getSequentialFilename(basePrefix, extension = 'pdf') {
  const cleanPrefix = String(basePrefix).replace(/[^a-zA-Z0-9_-]/g, '-')
  const storageKey = `ft_dl_seq_${cleanPrefix}`
  let currentSeq = 0

  try {
    const saved = localStorage.getItem(storageKey)
    if (saved) {
      currentSeq = parseInt(saved, 10) || 0
    }
  } catch {
    // Si localStorage no está disponible
  }

  currentSeq += 1

  try {
    localStorage.setItem(storageKey, currentSeq.toString())
  } catch {
    // Ignorar si el almacenamiento está restringido
  }

  const seqStr = String(currentSeq).padStart(2, '0')
  const ext = extension.startsWith('.') ? extension.slice(1) : extension
  return `${cleanPrefix}-${seqStr}.${ext}`
}

/**
 * Dispara la descarga de un Blob en el navegador con el nombre especificado.
 *
 * @param {Blob} blob - El objeto Blob descargado del API
 * @param {string} filename - Nombre deseado del archivo
 */
export function downloadBlob(blob, filename) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  // Pequeña pausa antes de revocar para asegurar que el navegador inició la descarga
  setTimeout(() => {
    URL.revokeObjectURL(url)
  }, 1500)
}

/**
 * Genera el documento HTML completo y auto-contenido del Comprobante de Recepción
 * listo para imprimir o descargar sin depender del árbol DOM de la SPA.
 */
export function generarHtmlComprobanteRecepcion(detalle, rnc = '') {
  const recNumero = `#REC-${String(detalle.id || 0).padStart(5, '0')}`
  const fechaDescarga = detalle.fecha ? new Date(detalle.fecha).toLocaleString('es-DO', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }) : '—'
  const fechaImpresion = new Date().toLocaleString('es-DO', {
    dateStyle: 'medium',
    timeStyle: 'medium',
  })
  const volumen = typeof detalle.volumenRecibido === 'number' 
    ? detalle.volumenRecibido.toFixed(2) 
    : String(detalle.volumenRecibido || '0.00')

  return `<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <title>Comprobante de Recepción ${recNumero} - FuelTrack</title>
  <style>
    @page {
      size: letter portrait;
      margin: 15mm 12mm;
    }
    * {
      box-sizing: border-box;
      margin: 0;
      padding: 0;
    }
    body {
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
      color: #12181a;
      background-color: #ffffff;
      padding: 24px;
      line-height: 1.4;
      -webkit-print-color-adjust: exact;
      print-color-adjust: exact;
    }
    .voucher-card {
      max-width: 680px;
      margin: 0 auto;
      border: 2px solid #16333a;
      border-radius: 8px;
      padding: 28px;
      background: #ffffff;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      border-bottom: 2px solid #16333a;
      padding-bottom: 16px;
      margin-bottom: 20px;
    }
    .title-sub {
      font-size: 10px;
      font-weight: 700;
      letter-spacing: 1.5px;
      text-transform: uppercase;
      color: #4a5a63;
    }
    .title-main {
      font-size: 18px;
      font-weight: 900;
      color: #16333a;
      margin-top: 4px;
      letter-spacing: -0.3px;
    }
    .title-desc {
      font-size: 11px;
      color: #4a5a63;
      margin-top: 2px;
    }
    .code-badge {
      text-align: right;
    }
    .code-number {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, monospace;
      font-size: 16px;
      font-weight: 800;
      color: #16333a;
    }
    .code-date {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, monospace;
      font-size: 11px;
      color: #4a5a63;
      margin-top: 4px;
    }
    .kpi-volumen {
      background: #f0f7f4;
      border: 1.5px solid #2e7d5b;
      border-radius: 6px;
      padding: 16px;
      text-align: center;
      margin-bottom: 20px;
    }
    .kpi-label {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 1.2px;
      color: #4a5a63;
    }
    .kpi-value {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, monospace;
      font-size: 32px;
      font-weight: 900;
      color: #16333a;
      margin-top: 2px;
    }
    .kpi-unit {
      font-size: 14px;
      font-weight: normal;
      color: #4a5a63;
    }
    .kpi-status {
      font-size: 11px;
      font-weight: 600;
      color: #2e7d5b;
      margin-top: 4px;
    }
    .grid-info {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 14px;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      padding: 16px;
      margin-bottom: 24px;
      font-size: 12px;
    }
    .info-label {
      font-size: 10px;
      text-transform: uppercase;
      font-weight: 700;
      color: #4a5a63;
      margin-bottom: 2px;
    }
    .info-value {
      font-weight: 600;
      color: #12181a;
    }
    .info-value-mono {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, monospace;
      font-weight: 700;
    }
    .col-span-2 {
      grid-column: span 2;
      border-top: 1px dashed #d1d5db;
      padding-top: 10px;
      display: flex;
      justify-content: space-between;
    }
    .signatures {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 40px;
      margin-top: 40px;
      margin-bottom: 24px;
      text-align: center;
      font-size: 11px;
    }
    .signature-line {
      border-top: 1.5px solid #16333a;
      padding-top: 6px;
      font-weight: 700;
      color: #12181a;
    }
    .signature-desc {
      font-size: 9px;
      color: #4a5a63;
      margin-top: 2px;
    }
    .footer {
      border-top: 1px solid #d1d5db;
      padding-top: 10px;
      font-size: 10px;
      color: #4a5a63;
      display: flex;
      justify-content: space-between;
    }
    @media print {
      body {
        padding: 0;
      }
      .voucher-card {
        border: none;
        padding: 0;
      }
    }
  </style>
</head>
<body>
  <div class="voucher-card">
    <div class="header">
      <div>
        <div class="title-sub">FuelTrack · Módulo de Suministro e Inventario</div>
        <h1 class="title-main">COMPROBANTE OFICIAL DE RECEPCIÓN</h1>
        <p class="title-desc">Control inmutable de abastecimiento a tanques de almacenamiento</p>
      </div>
      <div class="code-badge">
        <div class="code-number">${recNumero}</div>
        <div class="code-date">${fechaDescarga}</div>
      </div>
    </div>

    <div class="kpi-volumen">
      <div class="kpi-label">Volumen Total Descargado</div>
      <div class="kpi-value">${volumen} <span class="kpi-unit">galones</span></div>
      <div class="kpi-status">✔ Carga verificada e ingresada a existencia de inventario</div>
    </div>

    <div class="grid-info">
      <div>
        <div class="info-label">Proveedor Suplidor</div>
        <div class="info-value">${detalle.proveedorNombre || '—'}</div>
      </div>
      <div>
        <div class="info-label">RNC Suplidor</div>
        <div class="info-value info-value-mono">${rnc || '—'}</div>
      </div>
      <div>
        <div class="info-label">Factura / Conduce Suplidor</div>
        <div class="info-value info-value-mono">${detalle.numeroFactura || '—'}</div>
      </div>
      <div>
        <div class="info-label">Tanque Receptor de Destino</div>
        <div class="info-value info-value-mono">${detalle.tanqueIdentificacion || '—'}</div>
      </div>
      <div class="col-span-2">
        <div>
          <div class="info-label">Fecha y Hora de Descarga</div>
          <div class="info-value">${fechaDescarga}</div>
        </div>
        <div style="text-align: right;">
          <div class="info-label">Fecha y Hora de Impresión</div>
          <div class="info-value info-value-mono">${fechaImpresion}</div>
        </div>
      </div>
    </div>

    <div class="signatures">
      <div>
        <div style="height: 48px;"></div>
        <div class="signature-line">Transportista / Chofer Suplidor</div>
        <div class="signature-desc">Cédula y Ficha del Camión Cisterna</div>
      </div>
      <div>
        <div style="height: 48px;"></div>
        <div class="signature-line">Operador de Estación / Receptor</div>
        <div class="signature-desc">Firma y Sello de Recepción Conforme</div>
      </div>
    </div>

    <div class="footer">
      <span>FuelTrack v2.1 · Registro inmutable de abastecimiento de combustible</span>
      <span style="font-family: monospace;">Página 1 de 1</span>
    </div>
  </div>
</body>
</html>`
}

/**
 * Función compartida para imprimir HTML en ventana emergente o iframe aislado
 */
export function imprimirHtml(html) {
  try {
    const printWindow = window.open('', '_blank', 'width=800,height=900')
    if (printWindow && !printWindow.closed) {
      printWindow.document.write(html)
      printWindow.document.close()
      printWindow.focus()
      setTimeout(() => {
        try {
          printWindow.print()
        } catch {
          // Fallback si print() falla
        }
      }, 300)
      return
    }
  } catch {
    // Si window.open es bloqueado
  }

  // Fallback con iframe aislado para Electron o bloqueadores de ventanas emergentes
  const iframe = document.createElement('iframe')
  iframe.setAttribute('style', 'position:fixed;right:0;bottom:0;width:0;height:0;border:0;visibility:hidden;')
  document.body.appendChild(iframe)

  const doc = iframe.contentWindow.document
  doc.open()
  doc.write(html)
  doc.close()

  iframe.contentWindow.focus()
  setTimeout(() => {
    try {
      iframe.contentWindow.print()
    } catch {
      const blob = new Blob([html], { type: 'text/html;charset=utf-8' })
      const url = URL.createObjectURL(blob)
      window.open(url, '_blank')
    }
    setTimeout(() => {
      iframe.remove()
    }, 2000)
  }, 350)
}

/**
 * Imprime el comprobante de recepción utilizando un visor aislado.
 */
export function imprimirComprobanteRecepcion(detalle, rnc = '') {
  const html = generarHtmlComprobanteRecepcion(detalle, rnc)
  imprimirHtml(html)
}

/**
 * Descarga el comprobante oficial de recepción como archivo HTML auto-contenido.
 */
export function descargarComprobanteHtml(detalle, rnc = '') {
  const html = generarHtmlComprobanteRecepcion(detalle, rnc)
  const blob = new Blob([html], { type: 'text/html;charset=utf-8' })
  const baseName = `comprobante-recepcion-REC-${String(detalle.id || 0).padStart(5, '0')}`
  const filename = getSequentialFilename(baseName, 'html')
  downloadBlob(blob, filename)
}

/**
 * Genera el documento HTML completo del Comprobante de Despacho oficial.
 */
export function generarHtmlComprobanteDespacho(despacho, ticket = null) {
  const dspNumero = `#DSP-${String(despacho.despachoId || despacho.id || 0).padStart(6, '0')}`
  const ticketCodigo = despacho.codigoTicket || ticket?.codigo || '—'
  const fechaStr = despacho.fecha ? `${despacho.fecha}${despacho.hora ? ' ' + despacho.hora : ''}` : new Date().toLocaleString('es-DO')
  const fechaImpresion = new Date().toLocaleString('es-DO', {
    dateStyle: 'medium',
    timeStyle: 'medium',
  })
  const galonesServidos = typeof despacho.galonesServidos === 'number'
    ? despacho.galonesServidos.toFixed(2)
    : String(despacho.galonesServidos || '0.00')

  const conductor = ticket?.empleadoNombre || '—'
  const cedula = ticket?.empleadoCedula || '—'
  const vehiculo = ticket?.vehiculoPlaca
    ? `${ticket.vehiculoPlaca} (${ticket.vehiculoModelo || 'Vehículo'})`
    : '—'
  const combustible = ticket?.tipoCombustibleNombre || 'Combustible autorizado'
  const estacion = despacho.estacionNombre || 'Estación central'
  const tanque = despacho.tanqueIdentificacion || '—'
  const operador = despacho.operador || 'Operador de turno'
  const inventarioRestante = despacho.inventarioRestante !== undefined && despacho.inventarioRestante !== null
    ? `${Number(despacho.inventarioRestante).toFixed(2)} gal`
    : '—'
  const observaciones = despacho.observaciones || 'Sin observaciones.'

  return `<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <title>Comprobante de Despacho ${dspNumero} - FuelTrack</title>
  <style>
    @page {
      size: letter portrait;
      margin: 15mm 12mm;
    }
    * {
      box-sizing: border-box;
      margin: 0;
      padding: 0;
    }
    body {
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
      color: #16333a;
      background: #ffffff;
      font-size: 12px;
      line-height: 1.4;
      padding: 10px;
    }
    .ticket-container {
      max-width: 680px;
      margin: 0 auto;
      border: 2px solid #16333a;
      border-radius: 4px;
      padding: 24px;
      background: #ffffff;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      border-bottom: 2px solid #16333a;
      padding-bottom: 16px;
      margin-bottom: 20px;
    }
    .brand h1 {
      font-size: 20px;
      font-weight: 800;
      letter-spacing: -0.5px;
      color: #16333a;
      text-transform: uppercase;
    }
    .brand p {
      font-size: 11px;
      color: #4a5a63;
      margin-top: 2px;
    }
    .ticket-badge {
      text-align: right;
    }
    .ticket-title {
      font-size: 13px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #16333a;
    }
    .ticket-num {
      font-size: 18px;
      font-weight: 800;
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
      color: #16333a;
      margin-top: 2px;
    }
    .meta-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 12px;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 4px;
      padding: 14px 16px;
      margin-bottom: 20px;
    }
    .meta-item {
      display: flex;
      flex-direction: column;
    }
    .meta-label {
      font-size: 10px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #4a5a63;
      margin-bottom: 2px;
    }
    .meta-value {
      font-size: 12px;
      font-weight: 600;
      color: #16333a;
    }
    .meta-value.mono {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    }
    .highlight-card {
      background: #f0fdf4;
      border: 1.5px solid #22c55e;
      border-radius: 4px;
      padding: 16px;
      margin-bottom: 20px;
      text-align: center;
    }
    .highlight-card .hl-label {
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #15803d;
      margin-bottom: 4px;
    }
    .highlight-card .hl-vol {
      font-size: 32px;
      font-weight: 900;
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
      color: #166534;
      line-height: 1;
    }
    .highlight-card .hl-sub {
      font-size: 11px;
      color: #15803d;
      margin-top: 4px;
      font-weight: 500;
    }
    .section-title {
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #4a5a63;
      margin-bottom: 8px;
      border-bottom: 1px solid #e2e8f0;
      padding-bottom: 4px;
    }
    .details-table {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 20px;
    }
    .details-table th, .details-table td {
      padding: 8px 10px;
      text-align: left;
      font-size: 11px;
    }
    .details-table th {
      background: #f1f5f9;
      color: #4a5a63;
      font-weight: 600;
      border-bottom: 1px solid #cbd5e1;
    }
    .details-table td {
      border-bottom: 1px solid #e2e8f0;
    }
    .notes-box {
      background: #fafafa;
      border: 1px dashed #cbd5e1;
      border-radius: 4px;
      padding: 10px 14px;
      margin-bottom: 24px;
      font-size: 11px;
      color: #4a5a63;
    }
    .signatures {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 32px;
      margin-top: 36px;
      margin-bottom: 20px;
    }
    .sig-box {
      text-align: center;
    }
    .sig-line {
      border-bottom: 1px solid #16333a;
      margin-bottom: 8px;
      height: 40px;
    }
    .sig-name {
      font-size: 11px;
      font-weight: 700;
      color: #16333a;
    }
    .sig-role {
      font-size: 10px;
      color: #4a5a63;
    }
    .footer {
      border-top: 1px solid #e2e8f0;
      padding-top: 12px;
      margin-top: 20px;
      display: flex;
      justify-content: space-between;
      font-size: 9px;
      color: #94a3b8;
    }
  </style>
</head>
<body>
  <div class="ticket-container">
    <div class="header">
      <div class="brand">
        <h1>FuelTrack Enterprise</h1>
        <p>Sistema Integral de Gestión y Control de Combustible</p>
      </div>
      <div class="ticket-badge">
        <div class="ticket-title">Comprobante de Despacho</div>
        <div class="ticket-num">${dspNumero}</div>
      </div>
    </div>

    <div class="meta-grid">
      <div class="meta-item">
        <span class="meta-label">Ticket de Autorización</span>
        <span class="meta-value mono">${ticketCodigo}</span>
      </div>
      <div class="meta-item">
        <span class="meta-label">Fecha y Hora de Despacho</span>
        <span class="meta-value mono">${fechaStr}</span>
      </div>
      <div class="meta-item">
        <span class="meta-label">Estación de Combustible</span>
        <span class="meta-value">${estacion}</span>
      </div>
      <div class="meta-item">
        <span class="meta-label">Tanque Surtidor</span>
        <span class="meta-value mono">${tanque}</span>
      </div>
    </div>

    <div class="highlight-card">
      <div class="hl-label">Volumen Total Suministrado</div>
      <div class="hl-vol">${galonesServidos} GALONES</div>
      <div class="hl-sub">${combustible}</div>
    </div>

    <div class="section-title">Detalles del Receptor y Vehículo</div>
    <table class="details-table">
      <tbody>
        <tr>
          <th style="width:30%">Conductor / Portador</th>
          <td><strong>${conductor}</strong> (Cédula: ${cedula})</td>
        </tr>
        <tr>
          <th>Vehículo Asignado</th>
          <td>${vehiculo}</td>
        </tr>
        <tr>
          <th>Operador de Despacho</th>
          <td>${operador}</td>
        </tr>
        <tr>
          <th>Saldo en Tanque tras Suministro</th>
          <td class="mono">${inventarioRestante}</td>
        </tr>
      </tbody>
    </table>

    <div class="section-title">Observaciones Registradas</div>
    <div class="notes-box">
      ${observaciones}
    </div>

    <div class="signatures">
      <div class="sig-box">
        <div class="sig-line"></div>
        <div class="sig-name">${conductor}</div>
        <div class="sig-role">Firma del Conductor / Beneficiario</div>
      </div>
      <div class="sig-box">
        <div class="sig-line"></div>
        <div class="sig-name">${operador}</div>
        <div class="sig-role">Firma del Operador de Bomba</div>
      </div>
    </div>

    <div class="footer">
      <span>FuelTrack • Documento generado automáticamente</span>
      <span>Impreso el: ${fechaImpresion}</span>
    </div>
  </div>
</body>
</html>`
}

/**
 * Imprime el comprobante de despacho oficial utilizando el visor aislado.
 */
export function imprimirComprobanteDespacho(despacho, ticket = null) {
  const html = generarHtmlComprobanteDespacho(despacho, ticket)
  imprimirHtml(html)
}

/**
 * Descarga el comprobante de despacho oficial como archivo HTML auto-contenido.
 */
export function descargarComprobanteDespachoHtml(despacho, ticket = null) {
  const html = generarHtmlComprobanteDespacho(despacho, ticket)
  const blob = new Blob([html], { type: 'text/html;charset=utf-8' })
  const baseName = `comprobante-despacho-DSP-${String(despacho.despachoId || despacho.id || 0).padStart(6, '0')}`
  const filename = getSequentialFilename(baseName, 'html')
  downloadBlob(blob, filename)
}
