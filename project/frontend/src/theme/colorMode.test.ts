import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  applyMode,
  colorModeStorageKey,
  initialMode,
  readStoredMode,
  storeMode,
  systemMode,
} from './colorMode'

function htmlElement(): HTMLElement {
  const root = document.querySelector('html')
  if (!root) {
    throw new Error('html element not found')
  }

  return root
}

describe('color mode', () => {
  beforeEach(() => {
    window.localStorage.clear()
    htmlElement().removeAttribute('data-color-mode')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('reads only light and dark from storage', () => {
    expect(readStoredMode()).toBeNull()

    window.localStorage.setItem(colorModeStorageKey, 'dark')
    expect(readStoredMode()).toBe('dark')

    window.localStorage.setItem(colorModeStorageKey, 'sepia')
    expect(readStoredMode()).toBeNull()
  })

  it('prefers the stored mode over the system preference', () => {
    vi.stubGlobal('matchMedia', (query: string) => ({ matches: true, media: query }) as MediaQueryList)
    window.localStorage.setItem(colorModeStorageKey, 'light')

    expect(initialMode()).toBe('light')
  })

  it('falls back to the system preference', () => {
    vi.stubGlobal('matchMedia', (query: string) => ({ matches: true, media: query }) as MediaQueryList)
    expect(systemMode()).toBe('dark')
    expect(initialMode()).toBe('dark')

    vi.stubGlobal('matchMedia', (query: string) => ({ matches: false, media: query }) as MediaQueryList)
    expect(initialMode()).toBe('light')
  })

  it('applies the mode to the html element', () => {
    applyMode('dark')
    expect(htmlElement()).toHaveAttribute('data-color-mode', 'dark')

    applyMode('light')
    expect(htmlElement()).toHaveAttribute('data-color-mode', 'light')
  })

  it('stores the choice', () => {
    storeMode('dark')
    expect(window.localStorage.getItem(colorModeStorageKey)).toBe('dark')
  })

  it('survives a storage whose getItem throws', () => {
    const throwing = {
      getItem: () => {
        throw new Error('blocked')
      },
    } as unknown as Storage

    expect(readStoredMode(throwing)).toBeNull()
  })

  it('survives a blocked localStorage getter at boot', () => {
    const descriptor = Object.getOwnPropertyDescriptor(window, 'localStorage')
    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      get() {
        throw new Error('blocked')
      },
    })

    try {
      expect(readStoredMode()).toBeNull()
      expect(initialMode()).toBe('light')
      expect(() => storeMode('dark')).not.toThrow()
    } finally {
      if (descriptor) {
        Object.defineProperty(window, 'localStorage', descriptor)
      }
    }
  })
})
