import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import ConfirmDialog from './ConfirmDialog'

const TITLE = 'Delete task'
const DESCRIPTION = 'Delete “Write the proposal”? This cannot be undone.'

interface DialogHarnessProps {
  onConfirm: () => void
}

function DialogHarness({ onConfirm }: DialogHarnessProps) {
  const [open, setOpen] = useState(false)

  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Delete Write the proposal
      </button>
      {open && (
        <ConfirmDialog
          title={TITLE}
          description={DESCRIPTION}
          confirmLabel="Delete task"
          cancelLabel="Cancel"
          onConfirm={() => {
            onConfirm()
            setOpen(false)
          }}
          onCancel={() => setOpen(false)}
        />
      )}
    </>
  )
}

function trigger(): HTMLElement {
  return screen.getByRole('button', { name: 'Delete Write the proposal' })
}

describe('ConfirmDialog', () => {
  it('exposes the dialog semantics with a title and a description', async () => {
    const user = userEvent.setup()
    render(<DialogHarness onConfirm={vi.fn()} />)

    await user.click(trigger())

    const dialog = screen.getByRole('dialog')
    expect(dialog).toHaveAttribute('aria-modal', 'true')
    expect(dialog).toHaveAccessibleName(TITLE)
    expect(dialog).toHaveAccessibleDescription(DESCRIPTION)
  })

  it('moves the focus to the safe action when it opens', async () => {
    const user = userEvent.setup()
    render(<DialogHarness onConfirm={vi.fn()} />)

    await user.click(trigger())

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cancel' })).toHaveFocus()
  })

  it('keeps the focus inside the dialog while tabbing', async () => {
    const user = userEvent.setup()
    render(<DialogHarness onConfirm={vi.fn()} />)

    await user.click(trigger())
    const cancel = screen.getByRole('button', { name: 'Cancel' })
    const confirm = screen.getByRole('button', { name: 'Delete task' })

    await user.tab()
    expect(confirm).toHaveFocus()

    await user.tab()
    expect(cancel).toHaveFocus()

    await user.tab({ shift: true })
    expect(confirm).toHaveFocus()
  })

  it('closes with Escape and returns the focus to the trigger', async () => {
    const user = userEvent.setup()
    render(<DialogHarness onConfirm={vi.fn()} />)

    await user.click(trigger())
    await user.keyboard('{Escape}')

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(trigger()).toHaveFocus()
  })

  it('ignores Escape while the delete is in flight', async () => {
    const onCancel = vi.fn()
    const user = userEvent.setup()
    render(
      <ConfirmDialog
        title={TITLE}
        description={DESCRIPTION}
        confirmLabel="Delete task"
        cancelLabel="Cancel"
        busy
        onConfirm={vi.fn()}
        onCancel={onCancel}
      />,
    )

    await user.keyboard('{Escape}')

    expect(onCancel).not.toHaveBeenCalled()
  })

  it('confirms with the keyboard and returns the focus to the trigger', async () => {
    const onConfirm = vi.fn()
    const user = userEvent.setup()
    render(<DialogHarness onConfirm={onConfirm} />)

    await user.click(trigger())
    await user.tab()
    await user.keyboard('{Enter}')

    expect(onConfirm).toHaveBeenCalledTimes(1)
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(trigger()).toHaveFocus()
  })
})
