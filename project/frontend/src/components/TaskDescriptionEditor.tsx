import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useId, useState } from 'react'
import { useAuth } from '../auth/authContext'
import { taskKeys, updateTask } from '../lib/tasks'
import type { Task } from '../lib/tasks'
import { IconDescription } from './icons'

const CONNECTION_ERROR = 'Could not reach the server. Check your connection and try again.'
const SAVE_ERROR = 'Could not save the description. Please try again.'
const TOO_LONG_MESSAGE = 'Description must be at most 2000 characters.'
const MAX_LENGTH = 2000

interface TaskDescriptionEditorProps {
  task: Task
  /** Lets the row mark itself busy while the description is being saved. */
  onSavingChange: (saving: boolean) => void
}

/**
 * Inline description editing on a task card. Saving performs a full-replace
 * `PUT` (ADR-0004): title, status and due date are sent unchanged, and an
 * empty or whitespace-only description is cleared by sending `null`.
 */
export default function TaskDescriptionEditor({
  task,
  onSavingChange,
}: TaskDescriptionEditorProps) {
  const { session } = useAuth()
  const token = session?.accessToken ?? ''
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState(false)
  const [value, setValue] = useState('')
  const [saveError, setSaveError] = useState<string | null>(null)
  const textareaId = useId()

  // The backend trims and maps empty/whitespace to null, so validation and
  // the sent value both work on the trimmed text.
  const trimmed = value.trim()
  const tooLong = trimmed.length > MAX_LENGTH

  const descriptionMutation = useMutation({
    mutationFn: (description: string | null) =>
      updateTask(token, task.id, {
        title: task.title,
        description,
        status: task.status,
        dueDate: task.dueDate,
      }),
    onMutate: () => onSavingChange(true),
    onSettled: () => onSavingChange(false),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: taskKeys.all })
      setEditing(false)
      setSaveError(null)
    },
    onError: (error: unknown) => {
      setSaveError(error instanceof TypeError ? CONNECTION_ERROR : SAVE_ERROR)
    },
  })

  function startEdit(description: string | null): void {
    setValue(description ?? '')
    setSaveError(null)
    setEditing(true)
  }

  function cancelEdit(): void {
    setEditing(false)
    setValue('')
    setSaveError(null)
  }

  function save(): void {
    if (descriptionMutation.isPending || tooLong) {
      return
    }

    descriptionMutation.mutate(trimmed === '' ? null : trimmed)
  }

  if (editing) {
    return (
      <div className="task-description-editor">
        <label className="task-description-label" htmlFor={textareaId}>
          Description
        </label>
        <textarea
          id={textareaId}
          className="task-description-textarea"
          value={value}
          onChange={(event) => setValue(event.target.value)}
          rows={3}
          aria-invalid={tooLong || undefined}
          aria-describedby={tooLong ? `${textareaId}-error` : undefined}
        />
        {tooLong && (
          <p id={`${textareaId}-error`} className="task-description-error" role="alert">
            {TOO_LONG_MESSAGE}
          </p>
        )}
        {saveError && (
          <p className="task-description-error" role="alert">
            {saveError}
          </p>
        )}
        <div className="task-description-actions">
          <button
            type="button"
            className="button"
            onClick={save}
            disabled={descriptionMutation.isPending || tooLong}
          >
            {descriptionMutation.isPending ? 'Saving…' : 'Save description'}
          </button>
          <button
            type="button"
            className="button button-quiet"
            onClick={cancelEdit}
            disabled={descriptionMutation.isPending}
          >
            Cancel
          </button>
        </div>
      </div>
    )
  }

  return (
    <>
      {task.description && <p className="task-description">{task.description}</p>}
      <button
        type="button"
        className="task-description-toggle"
        onClick={() => startEdit(task.description)}
      >
        <IconDescription className="task-icon" />
        <span>{task.description ? 'Edit description' : 'Add description'}</span>
      </button>
    </>
  )
}
