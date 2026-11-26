export type ColorMode = 'light' | 'dark'

export const colorModeStorageKey = 'crudworks-color-mode'
const modeAttribute = 'data-color-mode'

/** The mode the user picked earlier, or null when nothing valid is stored. */
export function readStoredMode(storage?: Storage): ColorMode | null {
  try {
    // Resolve the storage object inside the try: a blocked localStorage getter
    // throws a SecurityError in some privacy modes, and boot must survive it.
    const target = storage ?? window.localStorage
    const value = target.getItem(colorModeStorageKey)
    return value === 'light' || value === 'dark' ? value : null
  } catch {
    return null
  }
}

/** The operating system preference, used only when the user has not chosen. */
export function systemMode(): ColorMode {
  if (typeof window.matchMedia !== 'function') {
    return 'light'
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export function initialMode(): ColorMode {
  return readStoredMode() ?? systemMode()
}

export function applyMode(mode: ColorMode): void {
  document.documentElement.setAttribute(modeAttribute, mode)
}

export function storeMode(mode: ColorMode): void {
  try {
    window.localStorage.setItem(colorModeStorageKey, mode)
  } catch {
    // Private mode or a blocked storage API: the session still works, the
    // preference just does not survive a reload.
  }
}
