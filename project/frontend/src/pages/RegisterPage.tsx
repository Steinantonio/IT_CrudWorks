import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { useAuth } from '../auth/authContext'
import AuthLayout from '../components/AuthLayout'
import { ApiError } from '../lib/api'

const DISPLAY_NAME_MESSAGE = 'Display name must be between 2 and 100 characters.'
const EMAIL_MESSAGE = 'Email must be a valid address.'
const PASSWORD_MESSAGE =
  'Password must be 8–128 characters and contain a letter and a digit.'

const registerSchema = z.object({
  displayName: z
    .string()
    .trim()
    .min(2, DISPLAY_NAME_MESSAGE)
    .max(100, DISPLAY_NAME_MESSAGE),
  email: z
    .string()
    .trim()
    .pipe(z.email(EMAIL_MESSAGE)),
  password: z
    .string()
    .min(8, PASSWORD_MESSAGE)
    .max(128, PASSWORD_MESSAGE)
    .regex(/\p{L}/u, PASSWORD_MESSAGE)
    .regex(/\p{Nd}/u, PASSWORD_MESSAGE),
})

type RegisterValues = z.infer<typeof registerSchema>
type RegisterFieldName = keyof RegisterValues

const serverFieldNames: readonly RegisterFieldName[] = ['displayName', 'email', 'password']

function isRegisterFieldName(value: string): value is RegisterFieldName {
  return serverFieldNames.some((field) => field === value)
}

export default function RegisterPage() {
  const { register: registerUser } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { displayName: '', email: '', password: '' },
  })

  async function onSubmit(values: RegisterValues) {
    setFormError(null)

    try {
      await registerUser(values.displayName, values.email, values.password)
      navigate('/login', {
        replace: true,
        state: {
          notice: 'Your account is ready. Sign in to continue.',
          email: values.email,
        },
      })
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setFormError('That email is already registered.')
        return
      }

      if (error instanceof ApiError && error.status === 400 && error.errors) {
        let mapped = false

        for (const [field, messages] of Object.entries(error.errors)) {
          if (isRegisterFieldName(field)) {
            setError(field, { type: 'server', message: messages[0] ?? 'Invalid value.' })
            mapped = true
          }
        }

        if (mapped) {
          return
        }

        setFormError('Some details are invalid. Check the form and try again.')
        return
      }

      if (error instanceof TypeError) {
        setFormError('Could not reach the server. Check your connection and try again.')
        return
      }

      setFormError('Could not create your account. Please try again.')
    }
  }

  return (
    <AuthLayout
      eyebrow="Get started"
      heading="Create an account"
      headingId="register-heading"
    >
      {formError && (
        <p className="alert" role="alert">
          {formError}
        </p>
      )}

      <form className="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="field">
          <label htmlFor="register-display-name">Display name</label>
          <input
            id="register-display-name"
            type="text"
            autoComplete="name"
            aria-invalid={errors.displayName ? true : undefined}
            aria-describedby={errors.displayName ? 'register-display-name-error' : undefined}
            {...register('displayName')}
          />
          {errors.displayName && (
            <p className="field-error" id="register-display-name-error" role="alert">
              {errors.displayName.message}
            </p>
          )}
        </div>

        <div className="field">
          <label htmlFor="register-email">Email</label>
          <input
            id="register-email"
            type="email"
            autoComplete="email"
            aria-invalid={errors.email ? true : undefined}
            aria-describedby={errors.email ? 'register-email-error' : undefined}
            {...register('email')}
          />
          {errors.email && (
            <p className="field-error" id="register-email-error" role="alert">
              {errors.email.message}
            </p>
          )}
        </div>

        <div className="field">
          <label htmlFor="register-password">Password</label>
          <input
            id="register-password"
            type="password"
            autoComplete="new-password"
            aria-invalid={errors.password ? true : undefined}
            aria-describedby={
              errors.password
                ? 'register-password-hint register-password-error'
                : 'register-password-hint'
            }
            {...register('password')}
          />
          {errors.password && (
            <p className="field-error" id="register-password-error" role="alert">
              {errors.password.message}
            </p>
          )}
          <p className="field-hint" id="register-password-hint">
            At least 8 characters with a letter and a digit.
          </p>
        </div>

        <button type="submit" className="button" disabled={isSubmitting}>
          {isSubmitting ? 'Creating account…' : 'Create account'}
        </button>
      </form>

      <p className="auth-switch">
        Already have an account? <Link to="/login">Sign in</Link>
      </p>
    </AuthLayout>
  )
}
