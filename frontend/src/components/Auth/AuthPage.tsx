import { useState, type FormEvent } from 'react'
import axios from 'axios'


type AuthMode = 'sign-in' | 'create-account'
type LoginResponse = { accessToken: string }

function AuthPage() {
  const isEmployee = window.location.pathname.endsWith('/employee')
  const [mode, setMode] = useState<AuthMode>('sign-in')
  const [notice, setNotice] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setNotice('')
    setIsSubmitting(true)

    const formData = new FormData(event.currentTarget)
    const credentials = {
      email: String(formData.get('email')),
      password: String(formData.get('password')),
    }

    try {
      if (mode === 'create-account') {
        await axios.post('/api/register', {
            ...credentials,
            inviteCode: String(formData.get('inviteCode') ?? '').trim(),
        })
      }

      const { data: login } = await axios.post<LoginResponse>(
        '/api/login?useCookies=false',
        credentials
      )
      window.localStorage.setItem('accessToken', login.accessToken)
      if (isEmployee) {
        window.location.assign('/employee/shift-notes')
        return
      }
      setNotice(
        mode === 'create-account'
          ? 'Account created and signed in. Your property team must assign your access before you can use protected features.'
          : 'Signed in. Your property team must assign your access before you can use protected features.'
      )
    } catch (error) {
      if (axios.isAxiosError<{ detail?: string; title?: string; errors?: Record<string, string[]> }>(error)) {
        const response = error.response?.data
        const validationErrors = response?.errors ? Object.values(response.errors).flat().join(' ') : ''
        setNotice(response?.detail ?? validationErrors ?? response?.title ?? error.message)
      } else {
        setNotice(error instanceof Error ? error.message : 'Unable to connect to the server.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  function changeMode(nextMode: AuthMode) {
    setMode(nextMode)
    setNotice('')
  }

  return (
    <div className="flex min-h-dvh flex-col bg-[#F5E9E2] font-['Inter',system-ui,sans-serif] text-lg leading-relaxed text-[#0B0014] antialiased">
      <a
        href="#auth-form"
        className="sr-only rounded-lg bg-[#0B0014] px-4 py-3 text-[#F5E9E2] focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-10 focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
      >
        Skip to sign in
      </a>

      <header className="mx-auto flex w-full max-w-6xl items-center justify-between px-5 py-5 sm:py-7">
        <a
          href="/"
          className="font-['Fraunces',Georgia,serif] text-3xl font-bold leading-none text-[#773344] no-underline"
        >
          Shiftr
        </a>
        <a
          href="/"
          className="text-base font-semibold text-[#773344] underline decoration-[#D44D5C] decoration-2 underline-offset-4 hover:text-[#5E2836]"
        >
          Back to home
        </a>
      </header>

      <main className="mx-auto grid w-full max-w-6xl flex-1 items-center gap-10 px-5 pb-10 pt-4 md:gap-14 md:pb-14 lg:grid-cols-[1fr_0.88fr] lg:gap-20">
        <section aria-labelledby="welcome-title" className="mx-auto w-full max-w-xl lg:mx-0">
          <p className="mb-4 text-sm font-bold uppercase tracking-[0.14em] text-[#773344]">
            {isEmployee ? 'For the people who look after it' : 'For the people who live here'}
          </p>
          <h1
            id="welcome-title"
            className="max-w-lg font-['Fraunces',Georgia,serif] text-5xl font-bold leading-[1.02] text-[#773344] sm:text-6xl"
          >
            A better way to feel at home.
          </h1>
          <p className="mt-5 max-w-lg text-lg leading-relaxed sm:text-xl">
            {isEmployee
              ? 'Pick up right where your team left off. Keep the important details in one shared place.'
              : 'Requests, updates, and the people who help make your building feel like home, all in one place.'}
          </p>

          <div aria-hidden="true" className="mt-8 hidden max-w-md sm:block lg:mt-12">
            <svg viewBox="0 0 360 230" xmlns="http://www.w3.org/2000/svg" className="h-auto w-full">
              <circle cx="315" cy="35" r="22" className="fill-[#D44D5C]" />
              <circle cx="34" cy="194" r="11" className="fill-[#D44D5C]" />
              <path
                d="M40 24h174a24 24 0 0 1 24 24v56a24 24 0 0 1-24 24H95l-29 24v-24H40a24 24 0 0 1-24-24V48a24 24 0 0 1 24-24z"
                strokeWidth="4"
                strokeLinejoin="round"
                className="fill-[#E3B5A4] stroke-[#773344]"
              />
              <rect x="44" y="55" width="112" height="9" rx="4.5" className="fill-[#773344]" />
              <rect x="44" y="79" width="76" height="9" rx="4.5" className="fill-[#773344]" />
              <path
                d="M139 116h174a24 24 0 0 1 24 24v46a24 24 0 0 1-24 24h-15v20l-29-20H139a24 24 0 0 1-24-24v-46a24 24 0 0 1 24-24z"
                className="fill-[#773344]"
              />
              <rect x="145" y="143" width="122" height="9" rx="4.5" className="fill-[#F5E9E2]" />
              <rect x="145" y="165" width="88" height="9" rx="4.5" className="fill-[#F5E9E2]" />
            </svg>
          </div>
        </section>

        <section
          aria-labelledby="form-title"
          className="mx-auto w-full max-w-lg rounded-3xl border border-[#E3B5A4] bg-[#FFF9F5] p-6 shadow-[0_18px_55px_rgba(119,51,68,0.08)] sm:p-9"
        >
          <div className="mb-7">
            <p className="mb-1 text-sm font-bold uppercase tracking-[0.12em] text-[#773344]">
              {isEmployee ? 'Employee access' : 'Resident access'}
            </p>
            <h2
              id="form-title"
              className="font-['Fraunces',Georgia,serif] text-3xl font-semibold leading-tight text-[#0B0014]"
            >
              {mode === 'sign-in' ? 'Welcome back' : 'Create your account'}
            </h2>
          </div>

          <div className="mb-7 grid grid-cols-2 rounded-xl bg-[#F5E9E2] p-1" role="tablist" aria-label="Account action">
            <button
              type="button"
              role="tab"
              aria-selected={mode === 'sign-in'}
              onClick={() => changeMode('sign-in')}
              className={`min-h-11 rounded-lg px-3 py-2 text-base font-semibold transition-colors focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#773344] ${mode === 'sign-in' ? 'bg-[#773344] text-white' : 'text-[#773344] hover:bg-white/70'}`}
            >
              Sign in
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={mode === 'create-account'}
              onClick={() => changeMode('create-account')}
              className={`min-h-11 rounded-lg px-3 py-2 text-base font-semibold transition-colors focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#773344] ${mode === 'create-account' ? 'bg-[#773344] text-white' : 'text-[#773344] hover:bg-white/70'}`}
            >
              Create account
            </button>
          </div>

          <form id="auth-form" onSubmit={handleSubmit} className="grid gap-5">
            {mode === 'create-account' && (
              <label className="grid gap-2 text-base font-semibold">
                Property invite code
                <input
                  autoComplete="off"
                  className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none transition focus:border-[#773344] focus:ring-2 focus:ring-[#773344]/20"
                  name="inviteCode"
                  placeholder="Enter the code from your property"
                  type="text"
                />
              </label>
            )}

            <label className="grid gap-2 text-base font-semibold">
              Email address
              <input
                autoComplete="email"
                className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none transition focus:border-[#773344] focus:ring-2 focus:ring-[#773344]/20"
                name="email"
                placeholder="you@example.com"
                required
                type="email"
              />
            </label>

            <label className="grid gap-2 text-base font-semibold">
              Password
              <input
                autoComplete={mode === 'sign-in' ? 'current-password' : 'new-password'}
                className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none transition focus:border-[#773344] focus:ring-2 focus:ring-[#773344]/20"
                name="password"
                placeholder="At least 8 characters"
                minLength={8}
                required
                type="password"
              />
            </label>

            {notice && (
              <p role="status" className="rounded-lg border border-[#D44D5C]/40 bg-[#D44D5C]/10 px-4 py-3 text-sm leading-snug">
                {notice}
              </p>
            )}

            <button
              disabled={isSubmitting}
              className="mt-1 inline-flex min-h-13 items-center justify-center rounded-xl border-2 border-[#773344] bg-[#773344] px-6 py-3 text-base font-bold text-white transition-colors hover:border-[#5E2836] hover:bg-[#5E2836] focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#0B0014]"
              type="submit"
            >
              {isSubmitting
                ? 'Connecting...'
                : mode === 'sign-in'
                  ? 'Sign in to Shiftr'
                  : 'Create account'}
            </button>
          </form>

          <div className="mt-6 border-t border-[#E3B5A4] pt-5 text-center text-sm leading-relaxed">
            <span>{isEmployee ? 'Resident?' : 'Part of the property team?'} </span>
            <a
              className="font-semibold text-[#773344] underline decoration-[#D44D5C] decoration-2 underline-offset-4 hover:text-[#5E2836]"
              href={isEmployee ? '/sign-in/resident' : '/sign-in/employee'}
            >
              {isEmployee ? 'Go to resident access' : 'Go to employee access'}
            </a>
          </div>
        </section>
      </main>
    </div>
  )
}

export default AuthPage