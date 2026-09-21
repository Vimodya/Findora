# Findora — Intelligent Lost & Found Network

Findora allows users and organizations to report lost/found belongings and
intelligently match lost reports with found reports using attributes such as
descriptions, images, location, time, category, brand, color, and physical
characteristics.

## Architecture

This backend starts as a **modular monolith** built with ASP.NET Core Web API
(C#). It is organized into clear layers within a single deployable project so
the codebase stays simple now, but can be split into services later without a
rewrite.

```
Findora/
├── Findora.sln
├── .config/
│   └── dotnet-tools.json  # local tool manifest (pins the dotnet-ef version)
├── .env.example           # template for local environment variables
├── docker-compose.yml     # local PostgreSQL (Module 22 fast-track)
├── Findora.API/
│   ├── Controllers/      # API endpoints (e.g. AuthController, UsersController)
│   ├── Data/              # DbContext, migrations, EF Core configuration
│   │   ├── FindoraDbContext.cs
│   │   └── Migrations/    # EF Core migrations (generated)
│   ├── Models/             # Domain/entity models + shared AuditableEntity
│   ├── DTOs/               # Request/response contracts (e.g. DTOs/Auth/, DTOs/Users/)
│   ├── Services/           # Business logic (e.g. AuthService, UserProfileService)
│   ├── Repositories/       # Data access abstractions (e.g. IUserRepository)
│   ├── Middleware/         # Custom middleware (e.g. ExceptionHandlingMiddleware)
│   ├── Authorization/      # Custom authorization requirements/handlers (e.g. ActiveAccountRequirement)
│   ├── Configuration/      # Strongly-typed options/config binding (e.g. CorsSettings, JwtSettings)
│   ├── Mappings/           # Object-to-object mapping profiles (e.g. AuthMappingExtensions, UserMappingExtensions)
│   ├── Properties/         # launchSettings.json
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Findora.API.csproj
├── Findora.API.Tests/      # xUnit test project (Module 21, started in Module 2)
└── README.md
```

## Technology Stack

- ASP.NET Core Web API (.NET 8 LTS)
- C#
- Entity Framework Core 8 + Npgsql.EntityFrameworkCore.PostgreSQL
- PostgreSQL
- JWT bearer authentication + `Microsoft.AspNetCore.Identity`'s `PasswordHasher<TUser>` (not full ASP.NET Core Identity — see Module 2 section below)
- REST APIs
- Swagger / OpenAPI (with JWT Bearer support)
- Docker (PostgreSQL for local development; API containerization planned)
- AWS deployment (planned)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (recommended
  — runs local PostgreSQL via `docker-compose.yml`) **or** a native PostgreSQL
  16 install if you'd rather not use Docker
- The local `dotnet-ef` tool, restored via the repo's tool manifest (see below)
- A PostgreSQL connection string and a JWT signing key exported for your
  shell session (or set via `dotnet user-secrets`) — see
  [Database configuration](#database-configuration-postgresql) and
  [Authentication & Authorization](#authentication--authorization-module-2)
  below. Both are required for `dotnet run` to serve requests that touch the
  database or issue tokens; the API won't even start if the JWT signing key
  is missing or too short.

## Running locally

```bash
cd Findora.API
dotnet run
```

The API will start (by default on `https://localhost:7251` and
`http://localhost:5222`, per `Properties/launchSettings.json`) and open the
Swagger UI at `/swagger`.

To run against a specific port manually:

```bash
dotnet run --urls "http://localhost:5299"
```

## Build

```bash
dotnet build
```

## Local PostgreSQL via Docker (Module 22 fast-track)

Both developers can run an identical local PostgreSQL 16 instance with
Docker Compose — no native PostgreSQL install required.

### Start PostgreSQL

```bash
docker compose up -d
```

This works out of the box with safe local-only defaults (db `findora_dev`,
user `findora`, password `changeme`, port `5432`) — no `.env` file is
required. To customize (e.g. a different port), copy `.env.example` to `.env`
first and edit the `POSTGRES_*` values; `docker compose` picks up `.env`
automatically.

### Check container status / health

```bash
docker compose ps
```

`STATUS` should show `Up ... (healthy)` — the compose file defines a
`pg_isready`-based health check. To follow logs:

```bash
docker compose logs -f postgres
```

### Apply EF Core migrations against it

With the container healthy and your connection string configured (see
[Database configuration](#database-configuration-postgresql) below) to point
at `localhost:5432`:

```bash
dotnet tool run dotnet-ef database update --project Findora.API --startup-project Findora.API
```

### Stop PostgreSQL

```bash
docker compose down
```

This stops and removes the container **but keeps your data** — it lives in
the named Docker volume `findora_postgres_data`, not in the container itself.
Running `docker compose up -d` again reattaches to the same volume, so
previously-applied migrations and any rows you've added are still there
(verified: the `__EFMigrationsHistory` table survives a full
`down` → `up` cycle).

To wipe the database and start completely fresh (rarely needed):

```bash
docker compose down -v
```

## Database configuration (PostgreSQL)

Findora uses PostgreSQL via EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`).
**No credentials are hard-coded or committed** — the connection string is
resolved through the standard ASP.NET Core configuration chain, in order of
precedence:

1. `ConnectionStrings__DefaultConnection` environment variable (used in
   Docker/CI/staging/production).
2. `dotnet user-secrets` (recommended for local development — never
   committed, stored outside the repo).
3. `ConnectionStrings:DefaultConnection` in `appsettings.json` /
   `appsettings.Development.json` — both intentionally ship with an **empty**
   value in source control.

### Setting up your local connection string

If you're using the Docker Compose PostgreSQL above with its defaults, the
connection string is simply:

```
Host=localhost;Port=5432;Database=findora_dev;Username=findora;Password=changeme
```

If you customized `.env` (different port/db/user/password) or are using a
native PostgreSQL install, copy the environment template and fill in your own
values instead:

```bash
cp .env.example .env
# edit .env with your local PostgreSQL host/port/db/user/password
```

Either way, make the connection string available to the API by exporting it
for your shell session:

```bash
# bash/zsh
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=findora_dev;Username=<you>;Password=<yours>"

# PowerShell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=findora_dev;Username=<you>;Password=<yours>"
```

or store it with the .NET Secret Manager instead (from `Findora.API/`):

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=findora_dev;Username=<you>;Password=<yours>"
```

`.env` and any `appsettings.*.json` overrides containing real values are
gitignored — only `.env.example` (a template with placeholder values) is
committed.

## EF Core migrations

The repo pins a local `dotnet-ef` tool version via `.config/dotnet-tools.json`
so both developers use the same CLI version as the `Microsoft.EntityFrameworkCore.Design`
package. Restore it once per clone:

```bash
dotnet tool restore
```

### Applying existing migrations

With your connection string configured (see above) and PostgreSQL reachable:

```bash
dotnet tool run dotnet-ef database update --project Findora.API --startup-project Findora.API
```

### Adding a new migration

After changing `FindoraDbContext` or adding/editing entities under `Models/`:

```bash
dotnet tool run dotnet-ef migrations add <MigrationName> --project Findora.API --startup-project Findora.API --output-dir Data/Migrations
```

Review the generated migration under `Findora.API/Data/Migrations/` before
committing it, then apply it locally with `database update` above.

### Migration workflow conventions (two-developer team)

- Never hand-edit or delete a migration that has already been applied by the
  other developer or in a shared environment — add a new migration on top
  instead (`dotnet ef migrations remove` is only safe for a migration you
  just created locally and haven't pushed/applied elsewhere).
- Rebase your feature branch onto the latest `dev` **before** adding a new
  migration, so your migration is generated on top of the current model
  snapshot and doesn't silently drop a teammate's schema changes.
- If two migrations are added independently and conflict, resolve by
  regenerating your migration after rebasing rather than manually merging the
  generated files.
- Commit the full migration (the `*.cs`, `*.Designer.cs`, and the updated
  `FindoraDbContextModelSnapshot.cs`) as one unit.

## Authentication & Authorization (Module 2)

### Architecture

Findora issues its own **JWT access tokens** plus an **opaque, rotating
refresh token**, backed by its own `Users`/`Roles`/`UserRoles` tables in
`FindoraDbContext` — not full ASP.NET Core Identity. The only piece of
Identity actually used is `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>`
(PBKDF2-based password hashing), which ships in the ASP.NET Core shared
framework and needs no extra NuGet package. This was a deliberate choice:
`FindoraDbContext` already existed as a plain `DbContext` from Module 1, and
adopting `IdentityDbContext<T>` at this point would mean restructuring
around Identity's schema/table-naming conventions rather than the other way
around.

- **Access token**: short-lived JWT (default 15 min, `Jwt:AccessTokenExpirationMinutes`),
  signed HMAC-SHA256, carrying the user's id/email/name/role claims.
- **Refresh token**: opaque random token (default 7-day expiry,
  `Jwt:RefreshTokenExpirationDays`). Only its SHA-256 hash is ever stored
  (`RefreshTokens.TokenHash`) — the raw value exists only on the wire to the
  client. Rotated on every use (old token revoked, new one issued); reusing
  an already-rotated-away token revokes every other active session for that
  user (basic stolen-token defense).
- **Email verification / password reset tokens**: same opaque-token-hashed-
  at-rest pattern, shorter-lived (24h / 30min respectively), single-use.
- **Roles**: `User`, `OrganizationStaff`, `Admin` — seeded via EF Core
  `HasData` (fixed IDs, applied by the Module 2 migration). Enforced via
  `[Authorize(Roles = ...)]` / the `AdminOnly` and `OrganizationStaffOrAdmin`
  policies registered in `Program.cs`.
- **Rate limiting**: a fixed-window limiter (`RateLimiting:Auth`, default 10
  requests/60s per client IP) applied to the whole `AuthController` via
  `[EnableRateLimiting("auth")]`, using .NET 8's built-in
  `Microsoft.AspNetCore.RateLimiting` (no extra package).

### Configuring the JWT signing key

Exactly like the PostgreSQL connection string (Module 1), **the JWT signing
key is never committed** — `appsettings.json`/`appsettings.Development.json`
ship with `Jwt:SigningKey` empty, and the API **fails fast at startup** with
a clear error if it's missing or shorter than 32 bytes. Set it the same way
as the connection string:

```bash
# bash/zsh
export Jwt__SigningKey="<a long random string, e.g. from `openssl rand -base64 48`>"

# PowerShell
$env:Jwt__SigningKey = "<a long random string>"
```

or via `dotnet user-secrets set "Jwt:SigningKey" "..."` from `Findora.API/`.
See `.env.example` for the full list of `Jwt__*` variables. **Never reuse a
signing key across environments**, and rotating it invalidates every
outstanding access token immediately (refresh tokens are unaffected, since
they aren't JWTs).

### Endpoints

All under `/api/v1/auth`, all anonymous except `logout` (requires a valid
access token):

| Endpoint | Purpose |
|---|---|
| `POST /register` | Create an account (unverified); returns `201` + user summary, no tokens. |
| `POST /login` | Validate credentials; returns access + refresh tokens. |
| `POST /refresh` | Exchange a valid refresh token for a new access + refresh pair (rotation). |
| `POST /logout` | **[Authorize]** Revoke a specific refresh token. |
| `POST /verify-email` | Consume a verification token; marks the account verified. |
| `POST /resend-verification` | Re-issue a verification token (generic response either way). |
| `POST /forgot-password` | Issue a reset token if the email exists (**identical response whether it does or not** — no email enumeration). |
| `POST /reset-password` | Consume a reset token, set a new password, and revoke every existing session. |

Since there's no real email provider yet (that's Module 16), verification
and reset links are delivered by `LoggingEmailSender`, which logs the
"email" (including the token) at `Information` level **only in
Development** — outside Development it deliberately logs only that an
email *would* have been sent, never the body/token, so this dev stub can
never leak a working token if it's ever left wired up somewhere it
shouldn't be.

### Testing it via Swagger

Swagger UI's **Authorize** button now accepts a Bearer token: call
`/login`, copy the `accessToken` value (no `"Bearer "` prefix — Swagger adds
that itself), paste it into the Authorize dialog, and subsequent requests
from Swagger UI include it automatically.

## User & Profile Management (Module 3)

### Endpoints

| Endpoint | Auth | Purpose |
|---|---|---|
| `GET /api/v1/users/me` | Required | The caller's own, full profile (email, phone, avatar, status, reputation, roles). |
| `PUT /api/v1/users/me` | Required | Update `fullName`/`phone`/`avatarUrl` only — see below. |
| `GET /api/v1/users/{id}` | None | A limited public view (`id`, `fullName`, `avatarUrl`, `memberSince`) — no email, phone, status, reputation, or roles. |
| `PATCH /api/v1/users/{id}/status` | Admin only | Set a user's account status to `Active`/`Suspended`/`Deactivated`. |

### What a user can and can't change about their own profile

`PUT /api/v1/users/me` binds to `UpdateProfileRequest`, which only has
`FullName`/`Phone`/`AvatarUrl` properties — there is no `Id`, `Email`,
`Status`, `ReputationScore`, or `Roles` property on that type at all, so
sending them (or anything else) in the request body has no effect. Email,
password, roles, account status, and reputation all require a separate,
more privileged path (respectively: unimplemented — email changes aren't
in scope yet; `/auth/reset-password`; direct DB seeding/future admin
tooling; this endpoint; Module 17).

### Account status

Three states — `Active`, `Suspended`, `Deactivated` — defaulting to
`Active` for every new registration. Enforced in two independent places so
a status change takes effect immediately, not just on the next login:

1. **Issuance time** — `AuthService.LoginAsync`/`RefreshAsync` (Module 2)
   refuse to issue new tokens for a non-`Active` account (`403`).
2. **Use time** — `Authorization/ActiveAccountRequirement.cs` +
   `ActiveAccountAuthorizationHandler.cs`, added to the default
   authorization policy in `Program.cs`. Every `[Authorize]`/
   `[Authorize(Roles = ...)]`-protected endpoint re-checks the account's
   *current* status from the database on every request — so an
   already-issued, still-unexpired access token stops working the instant
   an Admin suspends the account, without waiting for it to expire.

### Reputation (schema only)

`User.ReputationScore` exists and defaults to `0` for every account. There
is **no algorithm and no endpoint to set it** in this pass — Module 17
defines the real scoring logic later. It's returned in `GET /users/me` (so
the schema/response shape is proven out) but never accepted from any
request body.

### Report-history foundation

Deliberately **not implemented** — no endpoint, no placeholder table. Once
Modules 4/5 introduce `LostReport`/`FoundReport` entities, a query surface
(e.g. `GET /api/v1/users/me/reports`) belongs in `UsersController`; see the
doc comment there.

## Testing

`Findora.API.Tests` (xUnit) covers Module 2's authentication logic and
Module 3's profile/status logic — the minimum test project called for by
Module 21's incremental testing requirement, started here rather than
deferred.

```bash
dotnet test
```

- **Integration tests** (`AuthEndpointsTests`, `UsersEndpointsTests`,
  `RateLimitingTests`) boot the real `Program` end-to-end (real middleware
  pipeline, real JWT validation, real rate limiter, real
  `ActiveAccountRequirement`) via `WebApplicationFactory<Program>`, against
  an isolated **EF Core InMemory** database per test class instead of the
  real PostgreSQL container — fast and hermetic, with no Docker dependency
  for `dotnet test` to succeed. The email sender is swapped for
  `FakeEmailSender`, which captures verification/reset tokens for the test
  to use directly instead of scraping logs.
- **`AuthorizationPolicyTests`** exercises the actual `AdminOnly`/
  `OrganizationStaffOrAdmin` policies registered in `Program.cs` via
  `IAuthorizationService`, independent of any specific endpoint — including
  that a suspended Admin still gets rejected.
- **Unit tests** (`TokenServiceTests`, `OpaqueTokenGeneratorTests`) cover
  JWT claim/expiry generation and opaque-token hashing in isolation.

Covered (Module 2): successful/duplicate registration, successful/failed
login, JWT validation (missing/invalid bearer token), refresh rotation +
reuse detection, logout/revocation, role-based 401/403, password reset
(including session revocation), email verification (including single-use
enforcement), and rate limiting.

Covered (Module 3): own-profile retrieval (authenticated/unauthenticated),
profile update with allowed fields persisting and protected fields
(email/status/reputation/id/roles) never changing, one user's update never
touching another user's data, public profile excluding sensitive fields,
Admin-only status changes (Admin succeeds, normal user gets `403` — even
against their own account), invalid status values rejected as `400`, and a
suspended/deactivated account losing access to a protected endpoint and to
login immediately, with an already-issued access token.

Note: these tests intentionally don't exercise real PostgreSQL-specific
behavior (that's implicitly covered by the manual verification against the
Docker container during development — see `TODO.md`'s verification notes).
A Postgres-backed integration test suite (Testcontainers or similar) is a
reasonable next step under Module 21, not required to unblock Module 2/3.

## Cross-cutting API foundations (Module 0)

- **Health check** — `GET /api/health` requires no authentication and always
  returns `200 OK` while the process is up. It stays anonymous even after
  Module 2 adds authentication elsewhere. No external dependencies (e.g.
  PostgreSQL) are checked yet; that can be added later via
  `AspNetCore.HealthChecks.NpgSql` without changing the endpoint itself.
- **Global exception handling** — `Middleware/ExceptionHandlingMiddleware.cs`
  wraps the whole request pipeline. Any unhandled exception is logged
  server-side (method + path only — never headers/body/query string) and
  converted into a `application/problem+json` `ProblemDetails` response with
  an appropriate status code. Clients never see a stack trace or exception
  message.
- **CORS** — a `FrontendCorsPolicy` is registered from `Program.cs`, backed by
  the strongly-typed `Configuration/CorsSettings.cs` bound to the `"Cors"`
  appsettings section. `appsettings.json` ships with an empty allow-list
  (safe default — no cross-origin access); `appsettings.Development.json`
  allows the common local Vite/CRA dev ports (`http://localhost:5173`,
  `http://localhost:3000`) so the future React app can call the API
  immediately. `AllowAnyOrigin()` is never used, and no production domain is
  hard-coded before one exists — add it to the relevant environment's
  `Cors:AllowedOrigins` when it does.
- **Logging** — built-in `ILogger`/`Microsoft.Extensions.Logging` (no
  Serilog — not needed yet). `Default` log level is `Information` in
  Development and `Warning` in Production; `Microsoft.AspNetCore` and
  `Microsoft.EntityFrameworkCore` are kept at `Warning` in both to avoid
  framework/SQL noise. **Never log passwords, JWTs, connection strings, or
  other secrets** — when adding new logging, log identifiers/paths/status
  codes, not full request/response bodies or configuration values.

## Project conventions

A quick reference for where new code belongs and how it should look,
intended for a second developer picking up the repo.

### Naming

- **Namespaces/folders** mirror each other: `Findora.API.<Folder>` (e.g.
  `Findora.API.Middleware`, `Findora.API.Configuration`).
- **Classes/methods/properties**: `PascalCase`. **Parameters/locals**:
  `camelCase`. **Interfaces**: prefixed `I` (e.g. `IAuditableEntity`).
- **Async methods** end in `Async` (e.g. `SaveChangesAsync`).
- **Controllers**: `<Noun>Controller` (e.g. `LostReportsController`).
- **Services**: `<Noun>Service` + an `I<Noun>Service` interface, so
  controllers depend on the abstraction, not the concrete class.
- **Repositories**: `<Noun>Repository` + `I<Noun>Repository`, one per
  aggregate root.
- **DTOs**: suffixed by intent — `CreateXRequest`/`UpdateXRequest` for
  inbound payloads, `XResponse` (or just `XDto`) for outbound shapes. Don't
  reuse one DTO for both directions once fields start to diverge.
- **EF Core migrations**: `PascalCase`, imperative, describing the change
  (e.g. `AddLostReportsTable`), generated by `dotnet ef migrations add`
  rather than hand-named.

### Folder responsibilities

| Folder | Responsibility |
|---|---|
| `Controllers/` | Thin HTTP endpoints only: model binding, calling a service, mapping the result to an HTTP response. No business logic or direct `DbContext` access. |
| `Services/` | Business logic and orchestration. Controllers call services; services call repositories (once introduced) or `DbContext` directly for simple cases. This is where validation rules, matching logic, lifecycle transitions, etc. will live. |
| `Repositories/` | Data-access abstractions when a service's queries get complex enough to warrant hiding them behind an interface (e.g. `IUserRepository`). Not mandatory for every entity from day one — introduce one when it earns its keep. |
| `Models/` | EF Core entities (persisted domain objects) plus shared conventions like `AuditableEntity`/`IAuditableEntity`. Entities should stay free of DTO/HTTP concerns. |
| `DTOs/` | Request/response contracts exposed over HTTP. Controllers accept/return DTOs, never raw entities — see **DTO vs. entity separation** below. |
| `Data/` | `FindoraDbContext`, EF Core entity configuration (`IEntityTypeConfiguration<T>`), and `Data/Migrations/` (generated, reviewed before committing — see **EF Core migrations** above). |
| `Middleware/` | Custom `IMiddleware`/conventional middleware classes (e.g. `ExceptionHandlingMiddleware`), registered in `Program.cs`. |
| `Authorization/` | Custom `IAuthorizationRequirement`/`AuthorizationHandler<T>` pairs (e.g. `ActiveAccountRequirement`) for policy logic that needs more than a role check — registered onto policies in `Program.cs`. |
| `Configuration/` | Strongly-typed options classes bound to an `appsettings` section via `IOptions<T>` (e.g. `CorsSettings`), instead of scattering `builder.Configuration["Some:Key"]` string lookups through the codebase. |
| `Mappings/` | Entity ↔ DTO mapping (AutoMapper profiles or hand-written mapping extension methods), once DTOs are introduced for a given feature. |

### DTO vs. entity separation

- Controllers **never** accept or return `Models/` entities directly —
  always a `DTOs/` type. This keeps the persisted schema free to evolve
  (e.g. adding `IsDeleted`, renaming a column) without silently breaking the
  API contract, and avoids accidentally serializing fields that shouldn't be
  public (audit fields, internal flags, navigation properties that would
  cause serialization cycles).
- Mapping between the two lives in `Mappings/` (or inline in the service if
  it's a single trivial field-for-field copy) — not duplicated ad hoc in
  every controller action.
- Validation of inbound data belongs on the DTO (data annotations or manual
  checks in the service), not on the entity.

## Status

Module 0 (Project Foundation) is complete: global exception handling,
structured logging with sensible per-environment levels, the `/api/health`
endpoint, a CORS scaffold, reviewed configuration/`.gitignore` conventions,
and this conventions documentation are all in place. Module 1 (Database
Foundation) is complete: PostgreSQL/EF Core connectivity, `FindoraDbContext`,
the shared `AuditableEntity`/`IAuditableEntity` convention, and the baseline
`InitialCreate` migration are in place and verified against a real local
PostgreSQL database. The PostgreSQL portion of Module 22 (Docker & Local
Development) is fast-tracked and done — `docker-compose.yml` provides a
shared, reproducible local Postgres 16 instance with a health check and
persistent named volume. The API `Dockerfile`, frontend containerization, and
the rest of Module 22 are not yet implemented.

Module 2 (Authentication & Authorization) is implemented and verified against
the real Docker PostgreSQL database: registration, login, JWT access +
rotating refresh tokens, logout/revocation, email verification, password
reset, role seeding (`User`/`OrganizationStaff`/`Admin`), role-based
authorization policies, and auth rate limiting are all in place, with a new
`Findora.API.Tests` project covering the flows end-to-end (frontend for
Module 2 is being handled separately).

Module 3 (User & Profile Management) backend is implemented and verified
against the real Docker PostgreSQL database: own-profile view/update,
account status (`Active`/`Suspended`/`Deactivated`) enforced both at token
issuance and on every authenticated request, the reputation schema
placeholder, admin-only status management, and the limited public profile
view are all in place, with tests added to `Findora.API.Tests`. Frontend,
lost/found reporting (Module 4+), the reputation *algorithm* (Module 17),
AI matching, and AWS integration have not been implemented yet.
