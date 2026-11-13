import { useEffect, useRef, useState } from 'react'

interface TaskIdChipProps {
  id: string
}

const COPY_RESET_MS = 1600

/**
 * Shows the first 8 characters of the task GUID as a chip. The full ID is
 * available in the tooltip and is copied to the clipboard on click, with a
 * brief "Copied" confirmation. Clipboard access is guarded by feature
 * detection: without it the chip simply stays as a static-looking label.
 */
export default function TaskIdChip({ id }: TaskIdChipProps) {
  const [copied, setCopied] = useState(false)
  const resetTimerRef = useRef<number | null>(null)

  useEffect(
    () => () => {
      if (resetTimerRef.current !== null) {
        window.clearTimeout(resetTimerRef.current)
      }
    },
    [],
  )

  const shortId = `#${id.slice(0, 8).toUpperCase()}`

  async function copyId(): Promise<void> {
    if (typeof navigator === 'undefined' || !navigator.clipboard) {
      return
    }

    try {
      await navigator.clipboard.writeText(id)
    } catch {
      // Clipboard permission denied: keep showing the short ID.
      return
    }

    setCopied(true)
    if (resetTimerRef.current !== null) {
      window.clearTimeout(resetTimerRef.current)
    }
    resetTimerRef.current = window.setTimeout(() => setCopied(false), COPY_RESET_MS)
  }

  return (
    <button
      type="button"
      className={`task-id-chip${copied ? ' task-id-chip-copied' : ''}`}
      title={id}
      aria-label={`Copy task ID ${shortId}`}
      onClick={() => {
        void copyId()
      }}
    >
      {copied ? 'Copied' : shortId}
    </button>
  )
}
