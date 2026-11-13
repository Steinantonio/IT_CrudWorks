import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { useAuth } from '../auth/authContext'
import AuthLayout from '../components/AuthLayout'
import { ApiError } from '../lib/api'

const DEMO_EMAIL = 'demo@crudworks.local'
const DEMO_PASSWORD = 'Demo123!'

const loginSchema = z.object({
  email: z
    .string()
    .trim()
    .pipe(z.email('Enter a valid email address.')),
  password: z.string().min(1, 'Enter your password.'),
})

type LoginValues = z.infer<typeof loginSchema>

interface LoginLocationState {
  from?: unknown
  notice?: unknown
  email?: unknown
}

function asText(value: unknown): string | undefined {
  return typeof value === 'string' ? value : undefined
}

function asInternalPath(value: unknown): string | undefined {
  const path = asText(value)
  return path && path.startsWith('/') && !path.startsWith('//') ? path : undefined
}

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const state = (location.state ?? {}) as LoginLocationState
  const returnTo = asInternalPath(state.from)
  const notice = asText(state.notice)

  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: asText(state.email) ?? '', password: '' },
  })

  async function onSubmit(values: LoginValues) {
    setFormError(null)

    try {
      await login(values.email, values.password)
      navigate(returnTo ?? '/', { replace: true })
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setFormError('Invalid email or password.')
        return
      }

      if (error instanceof TypeError) {
        setFormError('Could not reach the server. Check your connection and try again.')
        return
      }

      setFormError('Sign-in failed. Please try again.')
    }
  }

  function useDemoAccount() {
    setFormError(null)
    setValue('email', DEMO_EMAIL, { shouldValidate: true })
    setValue('password', DEMO_PASSWORD, { shouldValidate: true })
  }

  return (
    <AuthLayout eyebrow="Welcome back" heading="Sign in" headingId="login-heading">
      {notice && (
        <p className="alert alert-success" role="status">
          {notice}
        </p>
      )}
      {formError && (
        <p className="alert" role="alert">
          {formError}
        </p>
      )}

      <form className="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        {/* The demo block comes first in DOM order so the existing keyboard
            walk from Email onward keeps its order. */}
        <div className="demo-access">
          <div className="demo-access-copy">
            <p className="demo-access-title">Demo access</p>
            <p className="demo-access-credentials">
              <code>{DEMO_EMAIL}</code> / <code>{DEMO_PASSWORD}</code>
            </p>
          </div>
          <button
            type="button"
            className="button button-quiet demo-access-button"
            onClick={useDemoAccount}
          >
            Use demo account
          </button>
        </div>

        <div className="field">
          <label htmlFor="login-email">Email</label>
          <input
            id="login-email"
            type="email"
            autoComplete="email"
            aria-invalid={errors.email ? true : undefined}
            aria-describedby={errors.email ? 'login-email-error' : undefined}
            {...register('email')}
          />
          {errors.email && (
            <p className="field-error" id="login-email-error" role="alert">
              {errors.email.message}
            </p>
          )}
        </div>

        <div className="field">
          <label htmlFor="login-password">Password</label>
          <input
            id="login-password"
            type="password"
            autoComplete="current-password"
            aria-invalid={errors.password ? true : undefined}
            aria-describedby={errors.password ? 'login-password-error' : undefined}
            {...register('password')}
          />
          {errors.password && (
            <p className="field-error" id="login-password-error" role="alert">
              {errors.password.message}
            </p>
          )}
        </div>

        <button type="submit" className="button" disabled={isSubmitting}>
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <p className="auth-switch">
        New here? <Link to="/register">Create an account</Link>
      </p>
    </AuthLayout>
  )
}
