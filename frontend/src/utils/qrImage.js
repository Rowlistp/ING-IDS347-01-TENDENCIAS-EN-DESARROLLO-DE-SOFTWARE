// Alternative decoder for dense signed QR images; server validation is still required.
export async function decodeQrImage(file) {
  const [{ default: jsQR }, bitmap] = await Promise.all([import('jsqr'), createImageBitmap(file)])
  try {
    const canvas = document.createElement('canvas')
    const scale = Math.min(1, 2048 / Math.max(bitmap.width, bitmap.height))
    canvas.width = Math.max(1, Math.round(bitmap.width * scale))
    canvas.height = Math.max(1, Math.round(bitmap.height * scale))
    const context = canvas.getContext('2d', { willReadFrequently: true })
    context.fillStyle = '#fff'
    context.fillRect(0, 0, canvas.width, canvas.height)
    context.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
    const pixels = context.getImageData(0, 0, canvas.width, canvas.height)
    const code = jsQR(pixels.data, pixels.width, pixels.height, { inversionAttempts: 'attemptBoth' })
    if (!code?.data) throw new Error('No se encontró un código QR legible.')
    return code.data
  } finally {
    bitmap.close()
  }
}
