import { useEffect, useRef } from 'react'

interface ConfirmDialogProps {
  title: string
  description: string
  confirmLabel: string
  cancelLabel: string
  busy?: boolean
  onConfirm: () => void
  onCancel: () => void
}

/**
 * Modal confirmation dialog. Focus moves to the first control on open,
 * Escape closes it, Tab stays inside, and focus returns to the trigger
 * element that had focus before the dialog opened.
 */
export default function ConfirmDialog({
  title,
  description,
  confirmLabel,
  cancelLabel,
  busy = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const dialogRef = useRef<HTMLDivElement | null>(null)
  const onConfirmRef = useRef(onConfirm)
  const onCancelRef = useRef(onCancel)
  const busyRef = useRef(busy)

  // Keep the latest callbacks readable from event handlers and effects.
  useEffect(() => {
    onConfirmRef.current = onConfirm
    onCancelRef.current = onCancel
    busyRef.current = busy
  })

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) {
      return undefined
    }

    const previouslyFocused = document.activeElement
    const firstControl = dialog.querySelector<HTMLElement>('button')
    firstControl?.focus()

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        event.preventDefault()
        // While the delete is in flight the buttons are disabled; Escape must
        // not dismiss the dialog behind the user's back.
        if (!busyRef.current) {
          onCancelRef.current()
        }
        return
      }

      if (event.key !== 'Tab' || !dialog) {
        return
      }

      const controls = Array.from(dialog.querySelectorAll<HTMLElement>('button:not([disabled])'))
      const first = controls[0]
      const last = controls[controls.length - 1]
      if (!first || !last) {
        return
      }

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      if (previouslyFocused instanceof HTMLElement) {
        previouslyFocused.focus()
      }
    }
  }, [])

  return (
    <div className="dialog-backdrop">
      <div
        ref={dialogRef}
        className="dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
        aria-describedby="confirm-dialog-description"
      >
        <h2 id="confirm-dialog-title">{title}</h2>
        <p id="confirm-dialog-description">{description}</p>
        <div className="dialog-actions">
          <button
            type="button"
            className="button button-quiet"
            onClick={() => onCancelRef.current()}
            disabled={busy}
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            className="button button-danger"
            onClick={() => onConfirmRef.current()}
            disabled={busy}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
