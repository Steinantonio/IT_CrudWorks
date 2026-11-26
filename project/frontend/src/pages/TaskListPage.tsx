import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { InfiniteData } from '@tanstack/react-query'
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
import { CursorExpiredError, listTasksPage, removeTask, taskKeys, updateTask } from '../lib/tasks'
import type { Task, TaskPage, TaskStatus } from '../lib/tasks'

const CONNECTION_ERROR = 'Could not reach the server. Check your connection and try again.'
const LIST_ERROR = 'Could not load your tasks. Please try again.'
const DELETE_ERROR = 'Could not delete the task. Please try again.'
const TOGGLE_ERROR = 'Could not update the task status. Please try again.'
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

function withTaskStatus(
  data: InfiniteData<TaskPage> | undefined,
  taskId: string,
  status: TaskStatus,
): InfiniteData<TaskPage> | undefined {
  if (!data) {
    return data
  }

  return {
    ...data,
    pages: data.pages.map((page) => ({
      ...page,
      items: page.items.map((task) =>
        task.id === taskId ? { ...task, status } : task,
      ),
    })),
  }
}

function withoutTask(
  data: InfiniteData<TaskPage> | undefined,
  taskId: string,
): InfiniteData<TaskPage> | undefined {
  if (!data) {
    return data
  }

  return {
    ...data,
    pages: data.pages.map((page) => ({
      ...page,
      items: page.items.filter((task) => task.id !== taskId),
    })),
  }
}

export default function TaskListPage() {
  const { session } = useAuth()
  const token = session?.accessToken ?? ''
  const userId = session?.user.id ?? ''
  const queryClient = useQueryClient()
  const [taskToDelete, setTaskToDelete] = useState<Task | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [statusMessage, setStatusMessage] = useState('')
  const [savingDescriptionId, setSavingDescriptionId] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<TaskStatus | null>(null)
  const [generation, setGeneration] = useState(0)
  const headingRef = useRef<HTMLHeadingElement | null>(null)
  const resetCursorRef = useRef<string | null>(null)

  const tasksQuery = useInfiniteQuery({
    queryKey: taskKeys.list(userId, generation),
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
    mutationFn: (task: Task) => removeTask(token, task.id),
    onMutate: async (task) => {
      await queryClient.cancelQueries({ queryKey: taskKeys.listRoot(userId) })
      const previous = queryClient.getQueriesData<InfiniteData<TaskPage>>({
        queryKey: taskKeys.listRoot(userId),
      })
      queryClient.setQueriesData<InfiniteData<TaskPage>>(
        { queryKey: taskKeys.listRoot(userId) },
        (data) => withoutTask(data, task.id),
      )
      return { previous }
    },
    onError: (error: unknown, _task, context) => {
      context?.previous.forEach(([key, data]) => {
        queryClient.setQueryData(key, data)
      })
      setTaskToDelete(null)
      setActionError(describeError(error, DELETE_ERROR))
      setStatusMessage('')
      // The row was unmounted optimistically, so the dialog's trigger is
      // detached and its focus restore is a no-op. Move focus to the heading
      // after the dialog unmounts instead of leaving it on <body>.
      window.setTimeout(() => headingRef.current?.focus(), 0)
    },
    onSuccess: async (_result, task) => {
      setTaskToDelete(null)
      setStatusMessage(`“${task.title}” deleted.`)
      await queryClient.invalidateQueries({ queryKey: taskKeys.listRoot(userId) })
      // The deleted row is gone, so the trigger that opened the dialog no
      // longer exists. Move the focus to the heading instead of losing it.
      headingRef.current?.focus()
    },
  })

  const toggleStatusMutation = useMutation({
    mutationFn: ({ task, status }: { task: Task; status: TaskStatus }) =>
      updateTask(token, task.id, {
        title: task.title,
        description: task.description,
        status,
        dueDate: task.dueDate,
      }),
    onMutate: async ({ task, status }) => {
      await queryClient.cancelQueries({ queryKey: taskKeys.listRoot(userId) })
      const previous = queryClient.getQueriesData<InfiniteData<TaskPage>>({
        queryKey: taskKeys.listRoot(userId),
      })
      queryClient.setQueriesData<InfiniteData<TaskPage>>(
        { queryKey: taskKeys.listRoot(userId) },
        (data) => withTaskStatus(data, task.id, status),
      )
      setActionError(null)
      setStatusMessage('')
      return { previous }
    },
    onError: (error: unknown, _variables, context) => {
      context?.previous.forEach(([key, data]) => {
        queryClient.setQueryData(key, data)
      })
      setActionError(describeError(error, TOGGLE_ERROR))
      setStatusMessage('')
    },
    onSuccess: async (_updated, { task, status }) => {
      setStatusMessage(
        status === 'Done'
          ? `“${task.title}” marked as done.`
          : `“${task.title}” moved back to Todo.`,
      )
      await queryClient.invalidateQueries({ queryKey: taskKeys.listRoot(userId) })

      // With a status filter active, the toggled row can leave the filtered
      // list and unmount, which would drop keyboard focus to <body>.
      if (statusFilter !== null && statusFilter !== status) {
        window.setTimeout(() => headingRef.current?.focus(), 0)
      }
    },
  })

  // One toggle at a time: a second checkbox stays disabled while a toggle is in
  // flight, so a failed mutation can never roll back another task's status.
  const togglePending = toggleStatusMutation.isPending

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

      <p className="sr-only" role="status">
        {statusMessage}
      </p>

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
                const dueSoon = due?.dueSoon === true && task.status !== 'Done'
                const statusClass = task.status.toLowerCase()
                const togglingThisTask =
                  toggleStatusMutation.isPending &&
                  toggleStatusMutation.variables?.task.id === task.id

                return (
                  <li
                    key={task.id}
                    className={`task-row task-row-${statusClass}`}
                    aria-busy={savingDescriptionId === task.id || togglingThisTask || undefined}
                  >
                    <Link
                      className="task-card-overlay"
                      to={`/tasks/${task.id}/edit`}
                      aria-hidden="true"
                      tabIndex={-1}
                    />
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
                        <p
                          className={`task-due${overdue ? ' task-due-overdue' : ''}${dueSoon ? ' task-due-soon' : ''}`}
                        >
                          <IconCalendar className="task-icon" />
                          {dueSoon && <span className="task-due-badge">Due soon</span>}
                          <span>{overdue ? `Overdue · ${due.date}` : `Due ${due.date}`}</span>
                        </p>
                      )}
                    </div>
                    <div className="task-actions">
                      <label className="task-status-toggle">
                        <input
                          type="checkbox"
                          checked={task.status === 'Done'}
                          disabled={togglePending && !togglingThisTask}
                          aria-label={
                            task.status === 'Done'
                              ? `Mark ${task.title} as not done`
                              : `Mark ${task.title} as done`
                          }
                          onChange={(event) => {
                            // Keep the focused checkbox enabled (disabling it would
                            // move focus to the body); ignore a second toggle while
                            // one is still in flight instead.
                            if (togglePending) {
                              return
                            }

                            toggleStatusMutation.mutate({
                              task,
                              status: event.target.checked ? 'Done' : 'Todo',
                            })
                          }}
                        />
                        <span>Done</span>
                      </label>
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
              <p role="status" className="loading-note">
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
          onConfirm={() => deleteMutation.mutate(taskToDelete)}
          onCancel={() => setTaskToDelete(null)}
        />
      )}
    </main>
  )
}
