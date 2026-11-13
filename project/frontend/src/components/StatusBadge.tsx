import type { TaskStatus } from '../lib/tasks'
import { STATUS_LABELS } from './statusLabels'

interface StatusBadgeProps {
  status: TaskStatus
}

export default function StatusBadge({ status }: StatusBadgeProps) {
  return (
    <span className={`status-badge status-${status.toLowerCase()}`}>{STATUS_LABELS[status]}</span>
  )
}
