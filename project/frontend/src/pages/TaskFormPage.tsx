import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ErrorState from '../components/ErrorState'
import { STATUS_LABELS } from '../components/statusLabels'
import { useAuth } from '../auth/authContext'
import { ApiError } from '../lib/api'
import { TASK_STATUS_VALUES, createTask, getTask, taskKeys, updateTask } from '../lib/tasks'
import type { TaskInput } from '../lib/tasks'
import { z } from 'zod'

const TITLE_MESSAGE = 'Title must be between 3 and 200 characters.'
const DESCRIPTION_MESSAGE = 'Description must be at most 2000 characters.'
const STATUS_MESSAGE = 'Status must be Todo, InProgress, or Done.'
const DUE_DATE_MESSAGE = 'Due date must be a valid date.'

const CONNECTION_ERROR = 'Could not reach the server. Check your connection and try again.'
const SAVE_ERROR = 'Could not save the task. Please try again.'
const GONE_ERROR = 'This task no longer exists.'
const FORM_ERROR = 'Some details are invalid. Check the form and try again.'

const taskSchema = z.object({
  title: z.string().trim().min(3, TITLE_MESSAGE).max(200, TITLE_MESSAGE),
  description: z.string().trim().max(2000, DESCRIPTION_MESSAGE),
  status: z.enum(TASK_STATUS_VALUES, { message: STATUS_MESSAGE }),
  dueDate: z.string().trim().refine(isValidDueDate, DUE_DATE_MESSAGE),
})

function isValidDueDate(value: string): boolean {
  return value === '' || (/^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(Date.parse(value)))
}

type TaskFormValues = z.infer<typeof taskSchema>
type TaskFieldName = keyof TaskFormValues

const serverFieldNames: readonly TaskFieldName[] = ['title', 'description', 'status', 'dueDate']

function isTaskFieldName(value: string): value is TaskFieldName {
  return serverFieldNames.some((field) => field === value)
}

function toInput(values: TaskFormValues): TaskInput {
  return {
    title: values.title,
    description: values.description === '' ? null : values.description,
    status: values.status,
    dueDate: values.dueDate === '' ? null : values.dueDate,
  }
}

export default function TaskFormPage() {
  const { id } = useParams()
  const isEdit = id !== undefined
  const { session } = useAuth()
  const token = session?.accessToken ?? ''
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)

  const taskQuery = useQuery({
    queryKey: taskKeys.detail(id ?? ''),
    queryFn: () => getTask(token, id ?? ''),
    enabled: isEdit && token !== '',
    retry: false,
  })

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<TaskFormValues>({
    resolver: zodResolver(taskSchema),
    defaultValues: { title: '', description: '', status: 'Todo', dueDate: '' },
  })

  useEffect(() => {
    const task = taskQuery.data
    if (task) {
      reset({
        title: task.title,
        description: task.description ?? '',
        status: task.status,
        dueDate: task.dueDate ?? '',
      })
    }
  }, [taskQuery.data, reset])

  const saveMutation = useMutation({
    mutationFn: (input: TaskInput) =>
      isEdit && id ? updateTask(token, id, input) : createTask(token, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: taskKeys.all })
      if (id) {
        await queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) })
      }
      navigate('/', { replace: true })
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError && error.status === 404) {
        setFormError(GONE_ERROR)
        return
      }

      if (error instanceof ApiError && error.status === 400 && error.errors) {
        let mapped = false

        for (const [field, messages] of Object.entries(error.errors)) {
          if (isTaskFieldName(field)) {
            setError(field, { type: 'server', message: messages[0] ?? 'Invalid value.' })
            mapped = true
          }
        }

        if (mapped) {
          return
        }

        setFormError(FORM_ERROR)
        return
      }

      if (error instanceof TypeError) {
        setFormError(CONNECTION_ERROR)
        return
      }

      setFormError(SAVE_ERROR)
    },
  })

  if (isEdit && taskQuery.isPending) {
    return (
      <main className="page" tabIndex={-1}>
        <p role="status" className="loading-note">
          Loading task…
        </p>
      </main>
    )
  }

  if (isEdit && taskQuery.isError) {
    if (taskQuery.error instanceof ApiError && taskQuery.error.status === 404) {
      return (
        <main className="page" tabIndex={-1}>
          <div className="empty-state">
            <h1>Task not found</h1>
            <p>The task you requested does not exist or belongs to another user.</p>
            <Link className="button button-quiet" to="/">
              Back to tasks
            </Link>
          </div>
        </main>
      )
    }

    return (
      <main className="page" tabIndex={-1}>
        <ErrorState
          message={
            taskQuery.error instanceof TypeError ? CONNECTION_ERROR : 'Could not load the task. Please try again.'
          }
          onRetry={() => taskQuery.refetch()}
        />
      </main>
    )
  }

  return (
    <main className="page" tabIndex={-1}>
      <div className="task-form-shell">
        <Link className="back-link" to="/">
          <span aria-hidden="true">←</span> Back to tasks
        </Link>

        <section className="panel task-form-panel" aria-labelledby="task-form-heading">
          <p className="eyebrow">Tasks</p>
          <h1 id="task-form-heading">{isEdit ? 'Edit task' : 'New task'}</h1>

          {formError && (
            <p className="alert" role="alert">
              {formError}
            </p>
          )}

          <form
            className="form"
            onSubmit={handleSubmit((values) => {
              setFormError(null)
              saveMutation.mutate(toInput(values))
            })}
            noValidate
          >
            <div className="field">
              <label htmlFor="task-title">Title</label>
              <input
                id="task-title"
                type="text"
                aria-invalid={errors.title ? true : undefined}
                aria-describedby={errors.title ? 'task-title-error' : undefined}
                {...register('title')}
              />
              {errors.title && (
                <p className="field-error" id="task-title-error" role="alert">
                  {errors.title.message}
                </p>
              )}
            </div>

            <div className="field">
              <label htmlFor="task-description">Description</label>
              <textarea
                id="task-description"
                rows={4}
                aria-invalid={errors.description ? true : undefined}
                aria-describedby={errors.description ? 'task-description-error' : undefined}
                {...register('description')}
              />
              {errors.description && (
                <p className="field-error" id="task-description-error" role="alert">
                  {errors.description.message}
                </p>
              )}
            </div>

            <div className="field-row">
              <div className="field">
                <label htmlFor="task-status">Status</label>
                <select
                  id="task-status"
                  aria-invalid={errors.status ? true : undefined}
                  aria-describedby={errors.status ? 'task-status-error' : undefined}
                  {...register('status')}
                >
                  {TASK_STATUS_VALUES.map((status) => (
                    <option key={status} value={status}>
                      {STATUS_LABELS[status]}
                    </option>
                  ))}
                </select>
                {errors.status && (
                  <p className="field-error" id="task-status-error" role="alert">
                    {errors.status.message}
                  </p>
                )}
              </div>

              <div className="field">
                <label htmlFor="task-due-date">Due date</label>
                <input
                  id="task-due-date"
                  type="date"
                  aria-invalid={errors.dueDate ? true : undefined}
                  aria-describedby={errors.dueDate ? 'task-due-date-error' : undefined}
                  {...register('dueDate')}
                />
                {errors.dueDate && (
                  <p className="field-error" id="task-due-date-error" role="alert">
                    {errors.dueDate.message}
                  </p>
                )}
              </div>
            </div>

            <div className="form-actions">
              <button
                type="submit"
                className="button"
                disabled={isSubmitting || saveMutation.isPending}
              >
                {saveMutation.isPending ? 'Saving…' : isEdit ? 'Save changes' : 'Create task'}
              </button>
              <Link className="button button-quiet" to="/">
                Cancel
              </Link>
            </div>
          </form>
        </section>
      </div>
    </main>
  )
}
