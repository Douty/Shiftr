import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'

type Property = {
  id: number
  name: string
  residentInviteId: string
  employeeInviteId: string
  managers: Employee[]
  frontDeskAgents: Employee[]
}

type Employee = {
  id: number
  firstName: string
  lastName: string
  email: string
  phoneNumber: string
}

type Organization = {
  id: number
  name: string
  properties: Property[]
}

type EmployeeAccessRequest = {
  id: number
  propertyId: number
  propertyName: string
  firstName: string
  lastName: string
  email: string
  phoneNumber: string
  role: 'Manager' | 'FrontDesk'
  createdAt: string
}

type Editor =
  | { type: 'organization'; id: number; name: string }
  | { type: 'property'; organizationId: number; propertyId: number | null; name: string }
  | null

function getAuthHeaders() {
  const token = window.localStorage.getItem('accessToken')
  if (!token) {
    window.location.assign('/sign-in/employee')
    throw new Error('Your session has expired. Sign in again to continue.')
  }
  return { Authorization: `Bearer ${token}` }
}

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError<{ detail?: string; title?: string }>(error)) {
    if (error.response?.status === 401) {
      window.localStorage.removeItem('accessToken')
      window.location.assign('/sign-in/employee')
      return 'Your session has expired. Sign in again.'
    }
    if (error.response?.status === 403) return 'Your account does not have access to manage this organization.'
    return error.response?.data?.detail ?? error.response?.data?.title ?? error.message
  }
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}

function DashboardPage() {
  const [organizations, setOrganizations] = useState<Organization[]>([])
  const [selectedOrganizationId, setSelectedOrganizationId] = useState<number | null>(null)
  const [isOwner, setIsOwner] = useState(false)
  const [accessRequests, setAccessRequests] = useState<EmployeeAccessRequest[]>([])
  const [requestsLoading, setRequestsLoading] = useState(false)
  const [requestNotice, setRequestNotice] = useState('')
  const [activeRequestId, setActiveRequestId] = useState<number | null>(null)
  const [rotatingPropertyId, setRotatingPropertyId] = useState<number | null>(null)
  const [copiedPropertyId, setCopiedPropertyId] = useState<number | null>(null)
  const [copiedResidentPropertyId, setCopiedResidentPropertyId] = useState<number | null>(null)
  const [editor, setEditor] = useState<Editor>(null)
  const [notice, setNotice] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    let isActive = true

    async function loadDashboard() {
      try {
        const headers = getAuthHeaders()
        const [organizationsResponse, ownerResponse] = await Promise.all([
          axios.get<Organization[]>('/api/organization/mine', { headers }),
          axios.get<boolean>('/api/employee/IsOwner', { headers }),
        ])
        if (!isActive) return
        setOrganizations(organizationsResponse.data)
        setSelectedOrganizationId(organizationsResponse.data[0]?.id ?? null)
        setIsOwner(ownerResponse.data)
      } catch (error) {
        if (isActive) setNotice(getErrorMessage(error))
      } finally {
        if (isActive) setIsLoading(false)
      }
    }

    void loadDashboard()
    return () => {
      isActive = false
    }
  }, [])

  useEffect(() => {
    if (selectedOrganizationId === null) return
    let isActive = true

    async function loadRequests() {
      setRequestsLoading(true)
      setRequestNotice('')
      try {
        const { data } = await axios.get<EmployeeAccessRequest[]>(
          `/api/employee-access-requests/organizations/${selectedOrganizationId}`,
          { headers: getAuthHeaders() },
        )
        if (isActive) setAccessRequests(data)
      } catch (error) {
        if (isActive) setRequestNotice(getErrorMessage(error))
      } finally {
        if (isActive) setRequestsLoading(false)
      }
    }

    void loadRequests()
    return () => {
      isActive = false
    }
  }, [selectedOrganizationId])

  const selectedOrganization = organizations.find(organization => organization.id === selectedOrganizationId)
  const propertyCount = organizations.reduce((count, organization) => count + organization.properties.length, 0)

  function editOrganization(organization: Organization) {
    setNotice('')
    setEditor({ type: 'organization', id: organization.id, name: organization.name })
  }

  function editProperty(organizationId: number, property?: Property) {
    setNotice('')
    setEditor({
      type: 'property',
      organizationId,
      propertyId: property?.id ?? null,
      name: property?.name ?? '',
    })
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!editor) return

    const name = String(new FormData(event.currentTarget).get('name') ?? '').trim()
    if (!name) return

    setIsSaving(true)
    setNotice('')
    try {
      const headers = getAuthHeaders()
      if (editor.type === 'organization') {
        const { data } = await axios.put<Organization>(
          `/api/organization/${editor.id}`,
          { id: editor.id, name },
          { headers },
        )
        setOrganizations(current => current.map(organization =>
          organization.id === editor.id ? { ...organization, name: data.name } : organization,
        ))
        setNotice('Organization details saved.')
      } else if (editor.propertyId === null) {
        const { data } = await axios.post<Property>(
          `/api/property/organizations/${editor.organizationId}`,
          { name },
          { headers },
        )
        setOrganizations(current => current.map(organization =>
          organization.id === editor.organizationId
            ? { ...organization, properties: [...organization.properties, data] }
            : organization,
        ))
        setNotice('Property created.')
      } else {
        const { data } = await axios.put<Property>(
          `/api/property/${editor.propertyId}`,
          { id: editor.propertyId, name },
          { headers },
        )
        setOrganizations(current => current.map(organization =>
          organization.id === editor.organizationId
            ? { ...organization, properties: organization.properties.map(property =>
              property.id === data.id ? data : property,
            ) }
            : organization,
        ))
        setNotice('Property details saved.')
      }
      setEditor(null)
    } catch (error) {
      setNotice(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleAccessRequest(request: EmployeeAccessRequest, action: 'approve' | 'reject') {
    setActiveRequestId(request.id)
    setRequestNotice('')
    try {
      const headers = getAuthHeaders()
      await axios.post(`/api/employee-access-requests/${request.id}/${action}`, undefined, { headers })
      setAccessRequests(current => current.filter(item => item.id !== request.id))
      if (action === 'approve') {
        const { data } = await axios.get<Organization[]>('/api/organization/mine', { headers })
        setOrganizations(data)
      }
      setRequestNotice(action === 'approve' ? 'Employee access approved.' : 'Request declined.')
    } catch (error) {
      setRequestNotice(getErrorMessage(error))
    } finally {
      setActiveRequestId(null)
    }
  }

  async function rotateEmployeeInvite(property: Property) {
    setRotatingPropertyId(property.id)
    setRequestNotice('')
    try {
      const { data } = await axios.post<{ inviteId: string }>(
        `/api/property/${property.id}/invite-ids/Employee/rotate`,
        undefined,
        { headers: getAuthHeaders() },
      )
      setOrganizations(current => current.map(organization => ({
        ...organization,
        properties: organization.properties.map(item =>
          item.id === property.id ? { ...item, employeeInviteId: data.inviteId } : item,
        ),
      })))
      setRequestNotice(`Employee invite code rotated for ${property.name}.`)
    } catch (error) {
      setRequestNotice(getErrorMessage(error))
    } finally {
      setRotatingPropertyId(null)
    }
  }

  async function copyEmployeeInvite(property: Property) {
    try {
      await navigator.clipboard.writeText(property.employeeInviteId)
      setCopiedPropertyId(property.id)
      window.setTimeout(() => setCopiedPropertyId(null), 1800)
    } catch {
      setRequestNotice('Clipboard access is unavailable in this browser.')
    }
  }

  async function rotateResidentInvite(property: Property) {
    setRotatingPropertyId(property.id)
    setRequestNotice('')
    try {
      const { data } = await axios.post<{ inviteId: string }>(
        `/api/property/${property.id}/invite-ids/Resident/rotate`,
        undefined,
        { headers: getAuthHeaders() },
      )
      setOrganizations(current => current.map(organization => ({
        ...organization,
        properties: organization.properties.map(item =>
          item.id === property.id ? { ...item, residentInviteId: data.inviteId } : item,
        ),
      })))
      setRequestNotice(`Resident invite code rotated for ${property.name}.`)
    } catch (error) {
      setRequestNotice(getErrorMessage(error))
    } finally {
      setRotatingPropertyId(null)
    }
  }

  async function copyResidentInvite(property: Property) {
    try {
      await navigator.clipboard.writeText(property.residentInviteId)
      setCopiedResidentPropertyId(property.id)
      window.setTimeout(() => setCopiedResidentPropertyId(null), 1800)
    } catch {
      setRequestNotice('Clipboard access is unavailable in this browser.')
    }
  }

  return (
    <div className="dashboard-shell">
      <header className="dashboard-header">
        <a href="/" className="dashboard-brand" aria-label="Shiftr home">Shiftr<span>.</span></a>
        <div className="dashboard-account">
          <span className="account-mark" aria-hidden="true">{isOwner ? 'O' : 'A'}</span>
          <span className="account-role">{isOwner ? 'Owner account' : 'Admin account'}</span>
          {!isLoading && !isOwner && <a className="dashboard-view-switch" href="/employee/dashboard">Employee dashboard</a>}
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
        </div>
      </header>

      <main className="dashboard-main">
        <div className="dashboard-eyebrow"><span /> Organization workspace</div>
        <div className="dashboard-heading-row">
          <div>
            <h1>Portfolio overview</h1>
            <p className="dashboard-subtitle">Your organization and the properties connected to it.</p>
          </div>
          {selectedOrganization && (
            <button className="primary-action" type="button" onClick={() => editProperty(selectedOrganization.id)}>
              <span aria-hidden="true">+</span> Add property
            </button>
          )}
        </div>

        {notice && <p className="dashboard-notice" role="status">{notice}</p>}

        {isLoading ? (
          <div className="dashboard-loading" role="status">Loading your portfolio<span>...</span></div>
        ) : organizations.length === 0 ? (
          <section className="dashboard-empty" aria-labelledby="empty-title">
            <span className="empty-index">01</span>
            <div>
              <h2 id="empty-title">No organization assigned</h2>
              <p>Your account needs to be linked to an organization before its properties can appear here.</p>
            </div>
          </section>
        ) : (
          <>
            <section className="portfolio-summary" aria-label="Portfolio summary">
              <div className="summary-primary">
                <span className="summary-label">Organizations</span>
                <strong>{organizations.length.toString().padStart(2, '0')}</strong>
              </div>
              <div className="summary-secondary">
                <span className="summary-label">Properties in portfolio</span>
                <strong>{propertyCount.toString().padStart(2, '0')}</strong>
              </div>
              <p className="summary-note">{isOwner ? 'Owner access' : 'Administrator access'}<br />Management workspace</p>
            </section>

            <section className="organization-section" aria-labelledby="organization-title">
              <div className="section-kicker">Your organization</div>
              {organizations.length > 1 && (
                <label className="organization-picker-label">
                  <span className="sr-only">Choose organization</span>
                  <select
                    value={selectedOrganizationId ?? ''}
                    onChange={event => setSelectedOrganizationId(Number(event.target.value))}
                  >
                    {organizations.map(organization => (
                      <option key={organization.id} value={organization.id}>{organization.name}</option>
                    ))}
                  </select>
                </label>
              )}
              {selectedOrganization && (
                <div className="organization-title-row">
                  <div>
                    <h2 id="organization-title">{selectedOrganization.name}</h2>
                    <span className="organization-id">Organization #{selectedOrganization.id}</span>
                  </div>
                  {isOwner && (
                    <button className="text-action" type="button" onClick={() => editOrganization(selectedOrganization)}>
                      Edit organization <span aria-hidden="true">↗</span>
                    </button>
                  )}
                </div>
              )}
            </section>

            {selectedOrganization && (
              <section className="properties-section" aria-labelledby="properties-title">
                <div className="properties-heading">
                  <div>
                    <div className="section-kicker">Locations</div>
                    <h2 id="properties-title">Properties <span>{selectedOrganization.properties.length}</span></h2>
                  </div>
                  <button className="secondary-action" type="button" onClick={() => editProperty(selectedOrganization.id)}>
                    <span aria-hidden="true">+</span> New property
                  </button>
                </div>

                {selectedOrganization.properties.length === 0 ? (
                  <div className="properties-empty">
                    <p>This organization has no properties yet.</p>
                    <button className="text-action" type="button" onClick={() => editProperty(selectedOrganization.id)}>
                      Create the first property <span aria-hidden="true">→</span>
                    </button>
                  </div>
                ) : (
                  <div className="property-list">
                    {selectedOrganization.properties.map((property, index) => (
                      <article className="property-row" key={property.id}>
                        <span className="property-number">{String(index + 1).padStart(2, '0')}</span>
                        <span className="property-symbol" aria-hidden="true"><i /><i /><i /></span>
                        <div className="property-name-block">
                          <h3>{property.name}</h3>
                          <p>Property #{property.id}</p>
                        </div>
                        <span className="property-status"><i /> Active</span>
                        <button className="text-action property-edit" type="button" onClick={() => editProperty(selectedOrganization.id, property)}>
                          Edit <span aria-hidden="true">↗</span>
                        </button>
                      </article>
                    ))}
                  </div>
                )}
              </section>
            )}

            {selectedOrganization && (
              <section className="employee-access-section" aria-labelledby="employee-access-title">
                <div className="employee-access-heading">
                  <div>
                    <div className="section-kicker">People and permissions</div>
                    <h2 id="employee-access-title">Employee access</h2>
                  </div>
                  <span className="request-count">{accessRequests.length.toString().padStart(2, '0')} pending</span>
                </div>
                {requestNotice && <p className="dashboard-notice access-notice" role="status">{requestNotice}</p>}

                <section className="access-subsection" aria-labelledby="requests-title">
                  <div className="access-subheading">
                    <div>
                      <h3 id="requests-title">Access requests</h3>
                      <p>Review requests from people joining your properties.</p>
                    </div>
                    <span className="access-count">{accessRequests.length}</span>
                  </div>
                  {requestsLoading ? (
                    <p className="access-empty">Loading requests...</p>
                  ) : accessRequests.length === 0 ? (
                    <p className="access-empty">No pending employee access requests.</p>
                  ) : (
                    <div className="access-request-list">
                      {accessRequests.map(request => (
                        <article className="access-request-row" key={request.id}>
                          <div className="request-person-mark" aria-hidden="true">
                            {request.firstName.charAt(0)}{request.lastName.charAt(0)}
                          </div>
                          <div className="request-person">
                            <h4>{request.firstName} {request.lastName}</h4>
                            <p>{request.email} · {request.phoneNumber}</p>
                          </div>
                          <div className="request-target">
                            <strong>{request.propertyName}</strong>
                            <span>{request.role === 'FrontDesk' ? 'Front desk' : 'Manager'} · {new Date(request.createdAt).toLocaleDateString()}</span>
                          </div>
                          <div className="request-actions">
                            <button type="button" className="approve-action" disabled={activeRequestId !== null} onClick={() => void handleAccessRequest(request, 'approve')}>
                              {activeRequestId === request.id ? 'Working...' : 'Approve'}
                            </button>
                            <button type="button" className="decline-action" disabled={activeRequestId !== null} onClick={() => void handleAccessRequest(request, 'reject')}>
                              Decline
                            </button>
                          </div>
                        </article>
                      ))}
                    </div>
                  )}
                </section>

                <section className="access-subsection" aria-labelledby="staff-title">
                  <div className="access-subheading">
                    <div>
                      <h3 id="staff-title">Employee access list</h3>
                      <p>People currently assigned to each property.</p>
                    </div>
                  </div>
                  <div className="staff-property-list">
                    {selectedOrganization.properties.map(property => {
                      const staff = [
                        ...property.managers.map(employee => ({ ...employee, accessRole: 'Manager' })),
                        ...property.frontDeskAgents.map(employee => ({ ...employee, accessRole: 'Front desk' })),
                      ]
                      return (
                        <div className="staff-property-row" key={property.id}>
                          <div className="staff-property-name">
                            <strong>{property.name}</strong>
                            <span>{staff.length} {staff.length === 1 ? 'employee' : 'employees'}</span>
                          </div>
                          {staff.length ? (
                            <ul>
                              {staff.map(employee => (
                                <li key={employee.id}>
                                  <span className="staff-dot" aria-hidden="true" />
                                  <span>{employee.firstName} {employee.lastName}</span>
                                  <span className="staff-role">{employee.accessRole}</span>
                                </li>
                              ))}
                            </ul>
                          ) : <span className="staff-empty">No assigned employees</span>}
                        </div>
                      )
                    })}
                    {selectedOrganization.properties.length === 0 && <p className="access-empty">Add a property to manage employee access.</p>}
                  </div>
                </section>

                <section className="access-subsection invite-subsection" aria-labelledby="invite-title">
                  <div className="access-subheading">
                    <div>
                      <h3 id="invite-title">Employee invite codes</h3>
                      <p>Share a code with a new employee so they can request access.</p>
                    </div>
                  </div>
                  <div className="invite-list">
                    {selectedOrganization.properties.map(property => (
                      <div className="invite-row" key={property.id}>
                        <div className="invite-property">
                          <strong>{property.name}</strong>
                          <code>{property.employeeInviteId || 'Unavailable'}</code>
                        </div>
                        <div className="invite-actions">
                          <button type="button" className="invite-tool" aria-label={`Copy ${property.name} employee invite code`} title="Copy code" onClick={() => void copyEmployeeInvite(property)} disabled={!property.employeeInviteId}>
                            {copiedPropertyId === property.id ? 'Copied' : 'Copy'}
                          </button>
                          <button type="button" className="invite-tool" aria-label={`Rotate ${property.name} employee invite code`} title="Rotate code" onClick={() => void rotateEmployeeInvite(property)} disabled={rotatingPropertyId !== null}>
                            {rotatingPropertyId === property.id ? 'Rotating...' : 'Rotate'}
                          </button>
                        </div>
                      </div>
                    ))}
                    {selectedOrganization.properties.length === 0 && <p className="access-empty">Invite codes appear here after you add a property.</p>}
                  </div>
                </section>

                <section className="access-subsection invite-subsection" aria-labelledby="resident-invite-title">
                  <div className="access-subheading">
                    <div>
                      <h3 id="resident-invite-title">Resident invite codes</h3>
                      <p>Share a code with residents so they can create an account for their property.</p>
                    </div>
                  </div>
                  <div className="invite-list">
                    {selectedOrganization.properties.map(property => (
                      <div className="invite-row" key={property.id}>
                        <div className="invite-property">
                          <strong>{property.name}</strong>
                          <code>{property.residentInviteId || 'Unavailable'}</code>
                        </div>
                        <div className="invite-actions">
                          <button type="button" className="invite-tool" aria-label={`Copy ${property.name} resident invite code`} title="Copy code" onClick={() => void copyResidentInvite(property)} disabled={!property.residentInviteId}>
                            {copiedResidentPropertyId === property.id ? 'Copied' : 'Copy'}
                          </button>
                          <button type="button" className="invite-tool" aria-label={`Rotate ${property.name} resident invite code`} title="Rotate code" onClick={() => void rotateResidentInvite(property)} disabled={rotatingPropertyId !== null}>
                            {rotatingPropertyId === property.id ? 'Rotating...' : 'Rotate'}
                          </button>
                        </div>
                      </div>
                    ))}
                    {selectedOrganization.properties.length === 0 && <p className="access-empty">Invite codes appear here after you add a property.</p>}
                  </div>
                </section>
              </section>
            )}
          </>
        )}
        <footer className="dashboard-footer"><span>SHIFTR / PORTFOLIO</span><span>Organization management</span></footer>
      </main>

      {editor && (
        <div className="dialog-backdrop" role="presentation" onMouseDown={event => {
          if (event.target === event.currentTarget && !isSaving) setEditor(null)
        }}>
          <section className="editor-dialog" role="dialog" aria-modal="true" aria-labelledby="editor-title">
            <div className="dialog-topline">
              <span>{editor.type === 'organization' ? 'Organization settings' : 'Property settings'}</span>
              <button className="dialog-close" type="button" aria-label="Close dialog" onClick={() => setEditor(null)}>×</button>
            </div>
            <h2 id="editor-title">
              {editor.type === 'organization'
                ? 'Edit organization'
                : editor.propertyId === null ? 'Add a property' : 'Edit property'}
            </h2>
            <form onSubmit={handleSubmit}>
              <label htmlFor="record-name">{editor.type === 'organization' ? 'Organization name' : 'Property name'}</label>
              <input id="record-name" autoFocus maxLength={120} name="name" required defaultValue={editor.name} />
              <div className="dialog-actions">
                <button className="cancel-action" type="button" disabled={isSaving} onClick={() => setEditor(null)}>Cancel</button>
                <button className="primary-action" type="submit" disabled={isSaving}>
                  {isSaving ? 'Saving...' : 'Save changes'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </div>
  )
}

export default DashboardPage