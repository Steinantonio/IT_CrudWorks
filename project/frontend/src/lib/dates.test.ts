import { describe, expect, it } from 'vitest'
import { formatDueDate } from './dates'

describe('formatDueDate', () => {
  const today = new Date(2026, 10, 12) // 12 Nov 2026

  it('marks today through today + 2 days as due soon', () => {
    expect(formatDueDate('2026-11-12', today)).toEqual({
      date: 'Nov 12, 2026',
      overdue: false,
      dueSoon: true,
    })
    expect(formatDueDate('2026-11-14', today)).toEqual({
      date: 'Nov 14, 2026',
      overdue: false,
      dueSoon: true,
    })
  })

  it('does not mark a date three days out as due soon', () => {
    expect(formatDueDate('2026-11-15', today)).toEqual({
      date: 'Nov 15, 2026',
      overdue: false,
      dueSoon: false,
    })
  })

  it('marks a past date as overdue and not due soon', () => {
    expect(formatDueDate('2026-11-11', today)).toEqual({
      date: 'Nov 11, 2026',
      overdue: true,
      dueSoon: false,
    })
  })

  it('returns null for a value that is not a calendar date', () => {
    expect(formatDueDate('not-a-date', today)).toBeNull()
    expect(formatDueDate('2026-02-31', today)).toBeNull()
  })
})
