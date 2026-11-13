import { describe, expect, it } from 'vitest'
import { formatDueDate } from './dates'

describe('formatDueDate', () => {
  const today = new Date(2026, 10, 12) // 12 Nov 2026

  it('formats a future date and does not mark it overdue', () => {
    expect(formatDueDate('2026-11-14', today)).toEqual({
      date: 'Nov 14, 2026',
      overdue: false,
    })
  })

  it('does not mark today as overdue', () => {
    expect(formatDueDate('2026-11-12', today)).toEqual({
      date: 'Nov 12, 2026',
      overdue: false,
    })
  })

  it('marks a past date as overdue', () => {
    expect(formatDueDate('2026-11-11', today)).toEqual({
      date: 'Nov 11, 2026',
      overdue: true,
    })
  })

  it('returns null for a value that is not a calendar date', () => {
    expect(formatDueDate('not-a-date', today)).toBeNull()
    expect(formatDueDate('2026-02-31', today)).toBeNull()
  })
})
