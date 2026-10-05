import { useState, type FormEvent } from 'react'
import axios from 'axios'


type AuthMode = 'sign-in' | 'create-account'
type EmployeeSignupMode = 'join-team' | 'create-organization'
type LoginResponse = { accessToken: string }

function AuthPage() {
  const isEmployee = window.location.pathname.endsWith('/employee')
  const [mode, setMode] = useState<AuthMode>('sign-in')
  const [employeeSignupMode, setEmployeeSignupMode] = useState<EmployeeSignupMode>('join-team')
  const [notice, setNotice] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const isCreatingOrganization =
    isEmployee && mode === 'create-account' && employeeSignupMode === 'create-organization'

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setNotice('')
    setIsSubmitting(true)

    const formData = new FormData(event.currentTarget)
    const credentials = {
      email: String(formData.get('email')),
      password: String(formData.get('password')),
    }
    const accessRequest = {
      employeeInviteCode: String(formData.get('inviteCode') ?? '').trim(),
      firstName: String(formData.get('firstName') ?? '').trim(),
      lastName: String(formData.get('lastName') ?? '').trim(),
      phoneNumber: String(formData.get('phoneNumber') ?? '').trim(),
      role: String(formData.get('employeeRole') ?? 'FrontDesk'),
    }

    try {
      if (isEmployee && mode === 'create-account') {
        await axios.post('/api/register', {
          ...credentials,
          inviteCode: String(formData.get('inviteCode') ?? '').trim(),
          createOrganization: isCreatingOrganization,
        })
      }

      let login: LoginResponse
      try {
        const { data } = await axios.post<LoginResponse>('/api/login?useCookies=false', credentials)
        login = data
      } catch (error) {
        if (
          isEmployee ||
          mode !== 'create-account' ||
          !axios.isAxiosError(error) ||
          error.response?.status !== 401
        ) {
          throw error
        }

        await axios.post('/api/register', {
          ...credentials,
          inviteCode: String(formData.get('inviteCode') ?? '').trim(),
        })
        const { data } = await axios.post<LoginResponse>('/api/login?useCookies=false', credentials)
        login = data
      }
      window.localStorage.setItem('accessToken', login.accessToken)
      if (isEmployee) {
        const headers = { Authorization: `Bearer ${login.accessToken}` }
        if (mode === 'create-account') {
          if (isCreatingOrganization) {
            await axios.post('/api/owner-signup', {
              organizationName: String(formData.get('organizationName') ?? '').trim(),
              firstName: String(formData.get('firstName') ?? '').trim(),
              lastName: String(formData.get('lastName') ?? '').trim(),
              phoneNumber: String(formData.get('phoneNumber') ?? '').trim(),
            }, { headers })

            const { data: ownerLogin } = await axios.post<LoginResponse>(
              '/api/login?useCookies=false',
              credentials
            )
            window.localStorage.setItem('accessToken', ownerLogin.accessToken)
            window.location.assign('/dashboard')
            return
          }

          await axios.post('/api/employee-access-requests', accessRequest, { headers })
          window.location.assign('/employee/access-request?submitted=1')
          return
        }

        const { data: isAdmin } = await axios.get<boolean>('/api/employee/IsAdmin', {
          headers,
        })
        if (isAdmin) {
          window.location.assign('/dashboard')
          return
        }

        const { data: hasEmployeeProfile } = await axios.get<boolean>('/api/employee/HasProfile', { headers })
        window.location.assign(hasEmployeeProfile ? '/employee/shift-notes' : '/employee/access-request')
        return
      }
      if (mode === 'create-account') {
        let hasResidentProfile = true
        try {
          await axios.get('/api/resident/profile', {
            headers: { Authorization: ['Bearer', login.accessToken].join(' ') },
          })
        } catch (error) {
          if (!axios.isAxiosError(error) || ![403, 404].includes(error.response?.status ?? 0)) {
            throw error
          }
          hasResidentProfile = false
        }

        if (!hasResidentProfile) {
          await axios.post('/api/resident-signup', {
            inviteCode: String(formData.get('inviteCode') ?? '').trim(),
            firstName: String(formData.get('firstName') ?? '').trim(),
            lastName: String(formData.get('lastName') ?? '').trim(),
            unitNumber: String(formData.get('unitNumber') ?? '').trim() || null,
          }, { headers: { Authorization: ['Bearer', login.accessToken].join(' ') } })

          const { data: residentLogin } = await axios.post<LoginResponse>(
            '/api/login?useCookies=false',
            credentials
          )
          login = residentLogin
          window.localStorage.setItem('accessToken', login.accessToken)
        }

        await axios.get('/api/resident/profile', {
          headers: { Authorization: ['Bearer', login.accessToken].join(' ') },
        })
      } else {
        try {
          await axios.get('/api/resident/profile', {
            headers: { Authorization: ['Bearer', login.accessToken].join(' ') },
          })
        } catch (error) {
          if (axios.isAxiosError(error) && [403, 404].includes(error.response?.status ?? 0)) {
            throw new Error(
              'This account does not have a resident profile yet. Choose Create account to finish setting it up.'
            )
          }
          throw error
        }
      }
      window.location.assign(mode === 'create-account' ? '/resident/dashboard?welcome=1' : '/resident/dashboard')
    } catch (error) {
      if (axios.isAxiosError<{ detail?: string; title?: string; errors?: Record<string, string[]> }>(error)) {
        const response = error.response?.data
        const validationErrors = response?.errors ? Object.values(response.errors).flat().join(' ') : ''
        setNotice(validationErrors || response?.detail || response?.title || error.message)
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
            {mode === 'create-account' && (!isEmployee || employeeSignupMode === 'join-team') && (
              <label className="grid gap-2 text-base font-semibold">
                {isEmployee ? 'Employee invite code' : 'Property invite code'}
                <input
                  autoComplete="off"
                  className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none transition focus:border-[#773344] focus:ring-2 focus:ring-[#773344]/20"
                  name="inviteCode"
                  placeholder="Enter the code from your property"
                  type="text"
                  required
                />
              </label>
            )}

            {mode === 'create-account' && !isEmployee && (
              <>
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <label className="grid gap-2 text-base font-semibold">
                    First name
                    <input autoComplete="given-name" className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none" name="firstName" required />
                  </label>
                  <label className="grid gap-2 text-base font-semibold">
                    Last name
                    <input autoComplete="family-name" className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none" name="lastName" required />
                  </label>
                </div>
                <label className="grid gap-2 text-base font-semibold">
                  Unit number <span className="font-normal">(optional)</span>
                  <input autoComplete="address-line2" className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none" name="unitNumber" maxLength={30} />
                </label>
              </>
            )}

            {mode === 'create-account' && isEmployee && (
              <>
                <fieldset className="grid gap-3">
                  <legend className="mb-1 text-base font-semibold">How would you like to get started?</legend>
                  <label className="flex min-h-12 cursor-pointer items-start gap-3 rounded-xl border border-[#B98482] bg-white p-4 text-base">
                    <input
                      checked={employeeSignupMode === 'join-team'}
                      className="mt-1 accent-[#773344]"
                      name="employeeSignupMode"
                      onChange={() => setEmployeeSignupMode('join-team')}
                      type="radio"
                      value="join-team"
                    />
                    <span>
                      <span className="block font-semibold">Join an existing team</span>
                      <span className="block text-sm font-normal">Use an employee invite code from your property.</span>
                    </span>
                  </label>
                  <label className="flex min-h-12 cursor-pointer items-start gap-3 rounded-xl border border-[#B98482] bg-white p-4 text-base">
                    <input
                      checked={employeeSignupMode === 'create-organization'}
                      className="mt-1 accent-[#773344]"
                      name="employeeSignupMode"
                      onChange={() => setEmployeeSignupMode('create-organization')}
                      type="radio"
                      value="create-organization"
                    />
                    <span>
                      <span className="block font-semibold">Create a new organization</span>
                      <span className="block text-sm font-normal">Set up your organization and become its owner.</span>
                    </span>
                  </label>
                </fieldset>

                {isCreatingOrganization && (
                  <section aria-labelledby="create-organization-title" className="grid gap-3 rounded-xl border border-[#E3B5A4] bg-[#F5E9E2]/60 p-4">
                    <h3 id="create-organization-title" className="text-base font-bold text-[#773344]">
                      Create new organization
                    </h3>
                    <label className="grid gap-2 text-base font-semibold">
                      Organization name
                      <input
                        autoComplete="organization"
                        className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none focus:border-[#773344] focus:ring-2 focus:ring-[#773344]/20"
                        name="organizationName"
                        placeholder="Your organization"
                        required
                      />
                    </label>
                    <p className="text-sm leading-snug">
                      You’ll be the owner and can add properties and invite your team after setup.
                    </p>
                  </section>
                )}

                <div className="grid grid-cols-2 gap-3">
                  <label className="grid gap-2 text-base font-semibold">
                    First name
                    <input autoComplete="given-name" className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none" name="firstName" required />
                  </label>
                  <label className="grid gap-2 text-base font-semibold">
                    Last name
                    <input autoComplete="family-name" className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none" name="lastName" required />
                  </label>
                </div>
                <label className="grid gap-2 text-base font-semibold">
                  Phone number
                  <input autoComplete="tel" className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none" name="phoneNumber" required type="tel" />
                </label>
                {employeeSignupMode === 'join-team' && (
                  <label className="grid gap-2 text-base font-semibold">
                    Requested access
                    <select className="min-h-12 w-full rounded-lg border border-[#B98482] bg-white px-4 text-base font-normal outline-none" name="employeeRole" defaultValue="FrontDesk">
                      <option value="FrontDesk">Front desk</option>
                      <option value="Manager">Manager</option>
                    </select>
                  </label>
                )}
              </>
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
                  : isCreatingOrganization
                    ? 'Create organization'
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