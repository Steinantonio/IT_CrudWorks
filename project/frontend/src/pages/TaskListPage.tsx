import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorState from '../components/ErrorState'
import StatusBadge from '../components/StatusBadge'
import TaskDescriptionEditor from '../components/TaskDescriptionEditor'
import TaskIdChip from '../components/TaskIdChip'
import { IconCalendar, IconTask } from '../components/icons'
import { useAuth } from '../auth/authContext'
import { formatDueDate } from '../lib/dates'
import { CursorExpiredError, listTasksPage, removeTask, taskKeys } from '../lib/tasks'
import type { Task, TaskStatus } from '../lib/tasks'

const CONNECTION_ERROR = 'Could not reach the server. Check your connection and try again.'
const LIST_ERROR = 'Could not load your tasks. Please try again.'
const DELETE_ERROR = 'Could not delete the task. Please try again.'
const FILTER_EMPTY_NOTE = 'No tasks with this status.'

const STATUS_CARDS = [
  { status: 'Todo', label: 'Todo', className: 'task-stat-todo' },
  { status: 'InProgress', label: 'In progress', className: 'task-stat-inprogress' },
  { status: 'Done', label: 'Done', className: 'task-stat-done' },
] as const satisfies ReadonlyArray<{ status: TaskStatus; label: string; className: string }>

function describeError(error: unknown, fallback: string): string {
  return error instanceof TypeError ? CONNECTION_ERROR : fallback
}

function countByStatus(tasks: Task[]): Record<TaskStatus, number> {
  const counts: Record<TaskStatus, number> = { Todo: 0, InProgress: 0, Done: 0 }
  for (const task of tasks) {
    counts[task.status] += 1
  }

  return counts
}

export default function TaskListPage() {
  const { session } = useAuth()
  const token = session?.accessToken ?? ''
  const queryClient = useQueryClient()
  const [taskToDelete, setTaskToDelete] = useState<Task | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [savingDescriptionId, setSavingDescriptionId] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<TaskStatus | null>(null)
  const [generation, setGeneration] = useState(0)
  const headingRef = useRef<HTMLHeadingElement | null>(null)
  const resetCursorRef = useRef<string | null>(null)

  const tasksQuery = useInfiniteQuery({
    queryKey: taskKeys.list(session?.user.id ?? '', generation),
    queryFn: ({ pageParam }) => listTasksPage(token, { cursor: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
    enabled: token !== '',
  })

  useEffect(() => {
    if (!(tasksQuery.error instanceof CursorExpiredError)) {
      return
    }

    if (resetCursorRef.current === tasksQuery.error.cursor) {
      return
    }

    resetCursorRef.current = tasksQuery.error.cursor
    setGeneration((current) => current + 1)
  }, [tasksQuery.error])

  const tasks = tasksQuery.data?.pages.flatMap((page) => page.items) ?? []

  const deleteMutation = useMutation({
    mutationFn: (id: string) => removeTask(token, id),
    onSuccess: async () => {
      setTaskToDelete(null)
      await queryClient.invalidateQueries({ queryKey: taskKeys.all })
      // The deleted row is gone, so the trigger that opened the dialog no
      // longer exists. Move the focus to the heading instead of losing it.
      headingRef.current?.focus()
    },
    onError: (error: unknown) => {
      setTaskToDelete(null)
      setActionError(describeError(error, DELETE_ERROR))
    },
  })

  // Counts are always computed from the loaded pages, never the filtered view.
  const counts = countByStatus(tasks)
  const visibleTasks =
    statusFilter === null ? tasks : tasks.filter((task) => task.status === statusFilter)

  const toggleStatusFilter = useCallback((status: TaskStatus) => {
    setStatusFilter((current) => (current === status ? null : status))
  }, [])

  const handleSavingChange = useCallback((taskId: string, saving: boolean) => {
    setSavingDescriptionId((current) => {
      if (saving) {
        return taskId
      }

      return current === taskId ? null : current
    })
  }, [])

  return (
    <main className="page" tabIndex={-1}>
      <div className="page-toolbar">
        <div className="page-heading">
          <p className="eyebrow">Your work</p>
          <h1 ref={headingRef} tabIndex={-1}>
            Tasks
          </h1>
          <p className="page-subtitle">
            Everything on your plate, grouped by where it stands.
          </p>
        </div>
        <Link className="button" to="/tasks/new">
          <span aria-hidden="true">+</span> New task
        </Link>
      </div>

      {actionError && (
        <p className="alert" role="alert">
          {actionError}
        </p>
      )}

      {tasksQuery.isPending && (
        <p role="status" className="loading-note">
          Loading tasks…
        </p>
      )}

      {tasksQuery.isError && tasks.length === 0 && (
        <ErrorState
          message={describeError(tasksQuery.error, LIST_ERROR)}
          onRetry={() => {
            setActionError(null)
            tasksQuery.refetch()
          }}
        />
      )}

      {tasksQuery.isError && tasks.length > 0 && (
        <ErrorState
          message={describeError(tasksQuery.error, LIST_ERROR)}
          onRetry={() => {
            setActionError(null)
            if (tasksQuery.isFetchNextPageError) {
              tasksQuery.fetchNextPage()
            } else {
              tasksQuery.refetch()
            }
          }}
        />
      )}

      {tasksQuery.isSuccess && tasks.length === 0 && (
        <div className="empty-state">
          <span className="empty-mark" aria-hidden="true">
            ✓
          </span>
          <h2>Your list is clear</h2>
          <p>No tasks yet — create your first task.</p>
          <Link className="button" to="/tasks/new">
            Create a task
          </Link>
        </div>
      )}

      {tasks.length > 0 && (
        <>
          <div className="task-stats" role="group" aria-label="Task summary">
            {STATUS_CARDS.map((card) => (
              <button
                key={card.status}
                type="button"
                className={`task-stat ${card.className}`}
                aria-pressed={statusFilter === card.status}
                aria-label={`Filter by ${card.label} (${counts[card.status]})`}
                onClick={() => toggleStatusFilter(card.status)}
              >
                <span className="task-stat-label">{card.label}</span>
                <span className="task-stat-count">{counts[card.status]}</span>
              </button>
            ))}
          </div>

          {tasksQuery.hasNextPage && (
            <p className="task-filter-note" role="status">
              Counts and filters include loaded tasks.
            </p>
          )}

          {visibleTasks.length === 0 ? (
            <p className="task-filter-note" role="status">
              {FILTER_EMPTY_NOTE}
            </p>
          ) : (
            <ul className="task-list">
              {visibleTasks.map((task) => {
                const due = task.dueDate ? formatDueDate(task.dueDate) : null
                const overdue = due?.overdue === true && task.status !== 'Done'
                const statusClass = task.status.toLowerCase()

                return (
                  <li
                    key={task.id}
                    className={`task-row task-row-${statusClass}`}
                    aria-busy={savingDescriptionId === task.id || undefined}
                  >
                    <div className="task-main">
                      <div className="task-title-line">
                        <IconTask className="task-icon task-icon-title" />
                        <Link className="task-title" to={`/tasks/${task.id}/edit`}>
                          {task.title}
                        </Link>
                        <StatusBadge status={task.status} />
                        <TaskIdChip id={task.id} />
                      </div>
                      <TaskDescriptionEditor
                        task={task}
                        onSavingChange={(saving) => handleSavingChange(task.id, saving)}
                      />
                      {due && (
                        <p className={`task-due${overdue ? ' task-due-overdue' : ''}`}>
                          <IconCalendar className="task-icon" />
                          <span>{overdue ? `Overdue · ${due.date}` : `Due ${due.date}`}</span>
                        </p>
                      )}
                    </div>
                    <div className="task-actions">
                      <Link
                        className="button button-quiet"
                        to={`/tasks/${task.id}/edit`}
                        aria-label={`Edit ${task.title}`}
                      >
                        Edit
                      </Link>
                      <button
                        type="button"
                        className="button button-quiet button-quiet-danger"
                        aria-label={`Delete ${task.title}`}
                        onClick={() => {
                          setActionError(null)
                          setTaskToDelete(task)
                        }}
                      >
                        Delete
                      </button>
                    </div>
                  </li>
                )
              })}
            </ul>
          )}

          {tasksQuery.hasNextPage && (
            <div className="task-pagination">
              <button
                type="button"
                className="button"
                onClick={() => tasksQuery.fetchNextPage()}
                disabled={tasksQuery.isFetchingNextPage}
              >
                {tasksQuery.isFetchingNextPage ? 'Loading more…' : 'Load more'}
              </button>
              <p role="status" aria-live="polite" className="loading-note">
                {tasksQuery.isFetchingNextPage ? 'Loading more tasks…' : ''}
              </p>
            </div>
          )}

          {!tasksQuery.hasNextPage && (
            <p className="task-filter-note" role="status">
              You have reached the end of the list.
            </p>
          )}
        </>
      )}

      {taskToDelete && (
        <ConfirmDialog
          title="Delete task"
          description={`Delete “${taskToDelete.title}”? This cannot be undone.`}
          confirmLabel="Delete task"
          cancelLabel="Cancel"
          busy={deleteMutation.isPending}
          onConfirm={() => deleteMutation.mutate(taskToDelete.id)}
          onCancel={() => setTaskToDelete(null)}
        />
      )}
    </main>
  )
}
