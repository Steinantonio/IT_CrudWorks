export interface DueDateFormat {
  /** Human-readable date, for example `Nov 14, 2026`. */
  date: string
  /** True when the date is before the start of the current day. */
  overdue: boolean
}

const DATE_ONLY = /^(\d{4})-(\d{2})-(\d{2})$/

const dueDateFormatter = new Intl.DateTimeFormat('en-US', {
  month: 'short',
  day: 'numeric',
  year: 'numeric',
})

/**
 * Parses the API's date-only value (`YYYY-MM-DD`) as a local calendar date.
 * `new Date('2026-11-14')` would parse as UTC midnight and could render as
 * Nov 13 in a negative-offset timezone.
 */
function parseDateOnly(value: string): Date | null {
  const match = DATE_ONLY.exec(value)
  if (!match) {
    return null
  }

  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  const date = new Date(year, month - 1, day)

  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month - 1 ||
    date.getDate() !== day
  ) {
    return null
  }

  return date
}

export function formatDueDate(value: string, today: Date = new Date()): DueDateFormat | null {
  const date = parseDateOnly(value)
  if (!date) {
    return null
  }

  const todayStart = new Date(today.getFullYear(), today.getMonth(), today.getDate())

  return {
    date: dueDateFormatter.format(date),
    overdue: date.getTime() < todayStart.getTime(),
  }
}
