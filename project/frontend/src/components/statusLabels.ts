import type { TaskStatus } from '../lib/tasks'

export const STATUS_LABELS: Record<TaskStatus, string> = {
  Todo: 'Todo',
  InProgress: 'In progress',
  Done: 'Done',
}
