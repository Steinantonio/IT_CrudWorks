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

      <form className="grid gap-5" onSubmit={handleSubmit(onSubmit)} noValidate>
        {/* The demo block comes first in DOM order so the existing keyboard
            walk from Email onward keeps its order. */}
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
          <div className="min-w-0">
            <p className="m-0 text-sm font-semibold text-slate-800 dark:text-slate-100">
              Demo access
            </p>
            <p className="mt-0.5 mb-0 text-xs text-slate-600 dark:text-slate-300">
              <code className="rounded bg-slate-200 px-1 py-0.5 font-mono text-[0.7rem] text-slate-800 dark:bg-slate-700 dark:text-slate-100">
                {DEMO_EMAIL}
              </code>{' '}
              /{' '}
              <code className="rounded bg-slate-200 px-1 py-0.5 font-mono text-[0.7rem] text-slate-800 dark:bg-slate-700 dark:text-slate-100">
                {DEMO_PASSWORD}
              </code>
            </p>
          </div>
          <button
            type="button"
            className="[font:inherit] rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100 dark:focus-visible:ring-offset-slate-900 dark:hover:bg-slate-800"
            onClick={useDemoAccount}
          >
            Use demo account
          </button>
        </div>

        <div className="grid gap-1.5">
          <label
            htmlFor="login-email"
            className="text-sm font-medium text-slate-700 dark:text-slate-200"
          >
            Email
          </label>
          <input
            id="login-email"
            type="email"
            autoComplete="email"
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900 shadow-sm transition [font:inherit] focus:border-teal-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2 aria-invalid:border-red-600 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-50 dark:focus-visible:ring-offset-slate-900 dark:aria-invalid:border-red-400"
            aria-invalid={errors.email ? true : undefined}
            aria-describedby={errors.email ? 'login-email-error' : undefined}
            {...register('email')}
          />
          {errors.email && (
            <p
              className="m-0 text-sm text-red-700 dark:text-red-300"
              id="login-email-error"
              role="alert"
            >
              {errors.email.message}
            </p>
          )}
        </div>

        <div className="grid gap-1.5">
          <label
            htmlFor="login-password"
            className="text-sm font-medium text-slate-700 dark:text-slate-200"
          >
            Password
          </label>
          <input
            id="login-password"
            type="password"
            autoComplete="current-password"
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900 shadow-sm transition [font:inherit] focus:border-teal-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2 aria-invalid:border-red-600 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-50 dark:focus-visible:ring-offset-slate-900 dark:aria-invalid:border-red-400"
            aria-invalid={errors.password ? true : undefined}
            aria-describedby={errors.password ? 'login-password-error' : undefined}
            {...register('password')}
          />
          {errors.password && (
            <p
              className="m-0 text-sm text-red-700 dark:text-red-300"
              id="login-password-error"
              role="alert"
            >
              {errors.password.message}
            </p>
          )}
        </div>

        <button
          type="submit"
          className="w-full [font:inherit] rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-teal-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-teal-500 dark:text-teal-950 dark:focus-visible:ring-offset-slate-900 dark:hover:bg-teal-400"
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <p className="mt-5 mb-0 text-sm text-slate-600 dark:text-slate-300">
        New here?{' '}
        <Link
          to="/register"
          className="font-semibold text-teal-700 underline decoration-teal-700/40 underline-offset-2 hover:decoration-teal-700 dark:text-teal-300 dark:decoration-teal-300/40"
        >
          Create an account
        </Link>
      </p>
    </AuthLayout>
  )
}
