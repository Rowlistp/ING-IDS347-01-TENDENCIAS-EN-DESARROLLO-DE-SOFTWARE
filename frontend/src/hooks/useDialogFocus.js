import { useEffect, useRef } from 'react'

export default function useDialogFocus(ref, active, onClose) {
  const close = useRef(onClose)
  useEffect(() => { close.current = onClose }, [onClose])
  useEffect(() => {
    if (!active || !ref.current) return
    const dialog = ref.current
    const previous = document.activeElement
    const oldOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const focusable = () => [...dialog.querySelectorAll('a[href], button, input, select, textarea, [tabindex]')]
      .filter(el => !el.disabled && el.tabIndex >= 0 && el.getClientRects().length)
    const focusFirst = () => (focusable()[0] ?? dialog).focus()
    focusFirst()
    function onKey(event) {
      if (event.key === 'Escape') {
        event.preventDefault()
        event.stopPropagation()
        close.current?.()
      } else if (event.key === 'Tab') {
        const items = focusable()
        const index = items.indexOf(document.activeElement)
        if (!items.length || (event.shiftKey ? index <= 0 : index === items.length - 1 || index < 0)) {
          event.preventDefault()
          ;(event.shiftKey ? items.at(-1) ?? dialog : items[0] ?? dialog).focus()
        }
      }
    }
    function keepFocus(event) {
      if (!dialog.contains(event.target)) focusFirst()
    }
    document.addEventListener('keydown', onKey, true)
    document.addEventListener('focusin', keepFocus)
    return () => {
      document.removeEventListener('keydown', onKey, true)
      document.removeEventListener('focusin', keepFocus)
      document.body.style.overflow = oldOverflow
      if (previous?.isConnected) previous.focus()
    }
  }, [active, ref])
}
