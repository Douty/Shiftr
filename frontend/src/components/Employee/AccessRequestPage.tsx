import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'

type AccessRequest = {
  propertyName: string
  role: 'Manager' | 'FrontDesk'
  status: 'Pending' | 'Approved'
  createdAt: string
}

function getAuthHeaders() {
  const token = window.localStorage.getItem('accessToken')
  if (!token) {
    window.location.assign('/sign-in/employee')
    throw new Error('Sign in to request employee access.')
  }
  return { Authorization: `Bearer ${token}` }
}

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError<{ detail?: string; title?: string }>(error)) {
    if (error.response?.status === 404) return 'That employee invite code is not valid.'
    if (error.response?.status === 409) return 'Your account already has employee access or a pending request.'
    return error.response?.data?.detail ?? error.response?.data?.title ?? error.message
  }
  return error instanceof Error ? error.message : 'Unable to submit your request.'
}

function AccessRequestPage() {
  const [pendingRequest, setPendingRequest] = useState<AccessRequest | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [notice, setNotice] = useState('')

  useEffect(() => {
    let isActive = true

    async function loadPendingRequest() {
      try {
        const { data } = await axios.get<AccessRequest>('/api/employee-access-requests/mine', {
          headers: getAuthHeaders(),
        })
        if (isActive) setPendingRequest(data)
      } catch (error) {
        if (!isActive || axios.isAxiosError(error) && error.response?.status === 404) return
        if (isActive) setNotice(getErrorMessage(error))
      } finally {
        if (isActive) setIsLoading(false)
      }
    }

    void loadPendingRequest()

    return () => {
      isActive = false
    }
  }, [])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setNotice('')
    const form = new FormData(event.currentTarget)
    const payload = {
      employeeInviteCode: String(form.get('employeeInviteCode') ?? '').trim(),
      firstName: String(form.get('firstName') ?? '').trim(),
      lastName: String(form.get('lastName') ?? '').trim(),
      phoneNumber: String(form.get('phoneNumber') ?? '').trim(),
      role: String(form.get('role')),
    }

    try {
      const { data } = await axios.post<AccessRequest>('/api/employee-access-requests', payload, {
        headers: getAuthHeaders(),
      })
      setPendingRequest(data)
      setNotice('Your request has been sent for review.')
    } catch (error) {
      setNotice(getErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="access-request-page">
      <header className="access-request-header">
        <a href="/" className="dashboard-brand" aria-label="Shiftr home">Shiftr<span>.</span></a>
        <button
          type="button"
          className="sign-out-button"
          onClick={() => {
            window.localStorage.removeItem('accessToken')
            window.location.assign('/sign-in/employee')
          }}
        >
          Sign out
        </button>
      </header>
      <main className="access-request-main">
        <div className="dashboard-eyebrow"><span /> Employee access</div>
        <h1>Request property access</h1>
        <p className="access-request-intro">Your request will be sent to the property team for review.</p>

        {notice && <p className="dashboard-notice" role="status">{notice}</p>}
        {isLoading ? (
          <p className="access-request-loading" role="status">Checking request status...</p>
        ) : pendingRequest ? (
          <section className="request-status-panel" aria-labelledby="request-status-title">
              <span className="request-status-mark" aria-hidden="true">{pendingRequest.status === 'Approved' ? '✓' : '…'}</span>
            <div>
              <p className="section-kicker">{pendingRequest.status === 'Approved' ? 'Access approved' : 'Awaiting review'}</p>
              <h2 id="request-status-title">
                {pendingRequest.status === 'Approved'
                  ? `You have access to ${pendingRequest.propertyName}`
                  : `Request sent to ${pendingRequest.propertyName}`}
              </h2>
              <p>
                {pendingRequest.role === 'FrontDesk' ? 'Front desk' : 'Manager'} access
                {pendingRequest.status === 'Approved' ? ' · Sign in again to refresh your access' : ` · ${new Date(pendingRequest.createdAt).toLocaleDateString()}`}
              </p>
            </div>
          </section>
        ) : (
          <form className="access-request-form" onSubmit={handleSubmit}>
            <label>
              Employee invite code
              <input autoComplete="off" maxLength={120} name="employeeInviteCode" required />
            </label>
            <div className="access-request-name-fields">
              <label>
                First name
                <input autoComplete="given-name" maxLength={100} name="firstName" required />
              </label>
              <label>
                Last name
                <input autoComplete="family-name" maxLength={100} name="lastName" required />
              </label>
            </div>
            <label>
              Phone number
              <input autoComplete="tel" name="phoneNumber" required type="tel" />
            </label>
            <label>
              Requested access
              <select name="role" defaultValue="FrontDesk">
                <option value="FrontDesk">Front desk</option>
                <option value="Manager">Manager</option>
              </select>
            </label>
            <button className="primary-action" type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Sending request...' : 'Send access request'}
            </button>
          </form>
        )}
        <a className="access-request-back" href="/sign-in/employee">Back to employee sign in</a>
      </main>
    </div>
  )
}

export default AccessRequestPage