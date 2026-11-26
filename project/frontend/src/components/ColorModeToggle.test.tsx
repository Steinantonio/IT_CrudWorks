import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { colorModeStorageKey } from '../theme/colorMode'
import ColorModeToggle from './ColorModeToggle'

function htmlElement(): HTMLElement {
  const root = document.querySelector('html')
  if (!root) {
    throw new Error('html element not found')
  }

  return root
}

describe('ColorModeToggle', () => {
  beforeEach(() => {
    window.localStorage.clear()
    htmlElement().removeAttribute('data-color-mode')
    vi.stubGlobal('matchMedia', (query: string) => ({ matches: false, media: query }) as MediaQueryList)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('starts in light mode and switches to dark, persisting the choice', async () => {
    const user = userEvent.setup()
    render(<ColorModeToggle />)

    const button = screen.getByRole('button', { name: 'Switch to dark mode' })
    expect(button).toHaveAttribute('aria-pressed', 'false')
    expect(htmlElement()).toHaveAttribute('data-color-mode', 'light')

    await user.click(button)

    expect(htmlElement()).toHaveAttribute('data-color-mode', 'dark')
    expect(window.localStorage.getItem(colorModeStorageKey)).toBe('dark')
    expect(screen.getByRole('button', { name: 'Switch to light mode' })).toHaveAttribute(
      'aria-pressed',
      'true',
    )
  })

  it('starts in dark mode when the choice was stored', () => {
    window.localStorage.setItem(colorModeStorageKey, 'dark')
    render(<ColorModeToggle />)

    expect(htmlElement()).toHaveAttribute('data-color-mode', 'dark')
    expect(screen.getByRole('button', { name: 'Switch to light mode' })).toBeInTheDocument()
  })
})
