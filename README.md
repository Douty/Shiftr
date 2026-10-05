# Local development owner account

The backend can create a local Owner account from .NET user-secrets. This is
enabled only in the Development environment; credentials are never stored in
the repository.

From the repository root, set an email address and a strong local password:

```powershell
dotnet user-secrets set "BootstrapOwner:Email" "you@example.com" --project backend\backend.csproj
dotnet user-secrets set "BootstrapOwner:Password" "choose-a-strong-local-password" --project backend\backend.csproj
```

Start the app from the frontend directory with `npm run start:both`. On backend
startup, the configured account is created (if needed) and assigned the Owner
role. If the email already belongs to an account, that account is promoted
without changing its existing password. Sign in at `/sign-in/employee`.

Keep these values in user-secrets or another local secret store; do not commit
them to source control. To remove the bootstrap configuration, run
`dotnet user-secrets remove "BootstrapOwner:Email" --project backend\backend.csproj`
and the equivalent command for `BootstrapOwner:Password`.

## Local development sample data

The backend can populate every existing organization with development-only
sample data. It fills each organization to at least 150 residents and 30
employees, creates sample properties when an organization has none, and adds
amenities, reservations, shift notes, and pending/rejected employee access
requests for its properties. The seeder is idempotent and runs only when a
password is configured; it is rejected outside the Development environment.

Set the shared password for the generated sign-in accounts with .NET
user-secrets:

```powershell
dotnet user-secrets set "DevelopmentData:Password" "choose-a-strong-local-password" --project backend\backend.csproj
```

Sample accounts use the `@example.test` domain. Remove the setting to stop
seeding on startup:

```powershell
dotnet user-secrets remove "DevelopmentData:Password" --project backend\backend.csproj
```

## Employee workspace

Signed-in front-desk employees and managers can use `/employee/dashboard` to
open separate pages for the daily activity log (`/employee/daily-activity`),
amenity booking management (`/employee/amenities`), and resident lookup
(`/employee/residents`). Resident lookup returns names and unit numbers only
for employees assigned to that property. Employees can create, update, and
cancel amenity bookings for assigned residents. Managers can switch between
the employee dashboard and `/dashboard`; backend authorization keeps
owner-only portfolio actions restricted to owners.