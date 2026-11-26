import { useEffect, useState } from 'react'
import { applyMode, initialMode, storeMode } from '../theme/colorMode'
import type { ColorMode } from '../theme/colorMode'

/** Light/dark switch for the whole app; the choice survives reloads. */
export default function ColorModeToggle() {
  const [mode, setMode] = useState<ColorMode>(() => initialMode())

  useEffect(() => {
    applyMode(mode)
  }, [mode])

  function toggle() {
    const next: ColorMode = mode === 'dark' ? 'light' : 'dark'
    storeMode(next)
    setMode(next)
  }

  const next = mode === 'dark' ? 'light' : 'dark'

  return (
    <button
      type="button"
      className="button button-quiet color-mode-toggle"
      onClick={toggle}
      aria-label={`Switch to ${next} mode`}
      aria-pressed={mode === 'dark'}
    >
      {mode === 'dark' ? 'Light mode' : 'Dark mode'}
    </button>
  )
}
