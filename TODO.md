# Findora — Development Roadmap (TODO.md)

This roadmap breaks Findora into **numbered, dependency-ordered modules** for
a two-developer team. It follows the current repository structure
(`Findora.sln` / `Findora.API` modular monolith — see [README.md](README.md))
and is meant to be worked through **in order**, module by module, without
skipping ahead into modules whose dependencies are not yet merged.

## How to use this file

- Check off tasks with `[x]` as they are completed and merged into `dev`.
- Do not start a module until its **Dependencies** are satisfied (merged into `dev`).
- Each module has a **Stage** tag so the team knows what's core vs. later work:
  - **MVP** — required for a working end-to-end demo (report → match → claim → recover).
  - **Post-MVP** — important, builds directly on the MVP loop.
  - **Advanced** — AI, advanced fraud detection, analytics; can be simplified/stubbed if time is short.
  - **Infrastructure** — cross-cutting (security hardening, testing, Docker, CI/CD, AWS, final integration).
- AI (Module 9), AWS integrations (Modules 6/23), and advanced fraud detection
  (Module 17) are explicitly **non-blocking** — the platform must work end-to-end
  without them first (rule-based matching, local file storage, manual review).

### Sequencing note

Modules are numbered by conceptual dependency, but two low-risk infrastructure
modules are cheap to pull forward and benefit the whole team early:

- **Module 22 (Docker & Local Development)** — a basic `docker-compose.yml`
  for PostgreSQL should be set up right after **Module 1**, so both developers
  run the same local database without installing PostgreSQL natively.
- **Module 24 (CI/CD)** — a minimal GitHub Actions workflow (build + test on
  every PR) should be set up right after **Module 0**, so broken builds are
  caught immediately instead of at the end.

Everything else should follow the numbered order below.

---

## Module 0 — Project Foundation

**Stage:** Infrastructure · **Status:** ✅ Largely complete (solution scaffolded)

**Purpose:** Establish the ASP.NET Core Web API foundation, project conventions,
and baseline operational concerns (errors, logging, health) that every other
module builds on.

**Features/tasks**
- [x] Solution (`Findora.sln`) and Web API project (`Findora.API`) created
- [x] Modular monolith folder structure (`Controllers`, `Data`, `Models`, `DTOs`, `Services`, `Repositories`, `Middleware`, `Configuration`, `Mappings`)
- [x] Swagger/OpenAPI configured
- [ ] Global exception-handling middleware (`Middleware/`) returning a consistent error response shape (e.g. `ProblemDetails`)
- [ ] Structured logging (`Serilog` or built-in `ILogger`) with request/response and error logging
- [ ] Basic API health check endpoint
- [ ] `appsettings.json` / `appsettings.Development.json` conventions for secrets vs. config (no secrets committed)
- [ ] `.gitignore` reviewed for `.NET`, `bin/`, `obj/`, `.env`, IDE files
- [ ] `CONTRIBUTING.md` or a section in `README.md` documenting coding conventions (naming, folder usage, DTO vs. entity separation)

**Backend tasks**
- [ ] Add global exception-handling middleware
- [ ] Add logging provider and log levels per environment
- [ ] Add `GET /api/health` endpoint (no auth required)
- [ ] Add CORS policy scaffold (locked down, to be configured per environment later)

**Database tasks**
- N/A (introduced in Module 1)

**Frontend tasks**
- N/A (frontend project scaffolding happens alongside Module 2/3 once there is something to call)

**API endpoints**
- `GET /api/health` — returns API status (200 OK)

**Dependencies**
- None (starting point)

**Definition of Done**
- Solution builds with 0 errors/warnings (`dotnet build`)
- `dotnet run` starts the API and Swagger UI loads at `/swagger`
- `GET /api/health` returns `200 OK`
- Unhandled exceptions return a consistent JSON error shape, not a stack trace
- Logging is visible in console output for requests and errors
- Conventions documented so a new contributor knows where new code belongs

**Suggested Git branch name**
`feature/project-foundation`

---

## Module 1 — Database Foundation

**Stage:** MVP

**Purpose:** Establish PostgreSQL connectivity and the EF Core data access
layer that every feature module will extend with its own entities.

**Features/tasks**
- [ ] PostgreSQL connection string configuration (per environment, via `appsettings`/env vars)
- [ ] `Npgsql.EntityFrameworkCore.PostgreSQL` + `Microsoft.EntityFrameworkCore.Design` packages added
- [ ] `FindoraDbContext` created in `Data/`
- [ ] Base entity / audit fields convention (`Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted` where soft-delete applies) as a shared base class or interface in `Models/`
- [ ] Initial (empty/skeleton) migration created and applied
- [ ] `dotnet ef` tooling documented for both developers (how to add/apply migrations locally)
- [ ] Database connection verified against local Docker PostgreSQL (see Module 22 fast-track)

**Backend tasks**
- [ ] Register `DbContext` in `Program.cs` via dependency injection
- [ ] Add `IAuditableEntity` (or base `AuditableEntity`) convention used by future entities
- [ ] Add design-time `DbContext` factory if needed for `dotnet ef` CLI

**Database tasks**
- [ ] Create PostgreSQL database (local + document connection string format)
- [ ] Create and apply initial EF Core migration (baseline, even if no domain tables yet)
- [ ] Decide and document migration workflow (who runs `dotnet ef migrations add`, how conflicts are avoided)

**Frontend tasks**
- N/A

**API endpoints**
- None (internal data layer only)

**Dependencies**
- Module 0

**Definition of Done**
- API connects to a local PostgreSQL instance on startup with no errors
- `dotnet ef migrations add InitialCreate` and `dotnet ef database update` both succeed
- Base auditable entity convention documented for use by all future `Models/`
- Both developers can run migrations locally against their own database instance

**Suggested Git branch name**
`feature/database-foundation`

---

## Module 2 — Authentication & Authorization

**Stage:** MVP

**Purpose:** Allow users to securely register, log in, and be identified/authorized
on every subsequent request. Everything past this point assumes a known,
authenticated user.

**Features/tasks**
- [ ] User registration (email + password)
- [ ] Password hashing (ASP.NET Core Identity or `BCrypt`/`PBKDF2` if Identity is not used wholesale)
- [ ] Login issuing a JWT access token (+ refresh token strategy)
- [ ] Email verification flow (token generation + verification endpoint; actual email sending can be a stub/log in MVP, wired to a real provider in Module 16)
- [ ] Password recovery (forgot password → reset token → reset password)
- [ ] Role-based authorization (`User`, `OrganizationStaff`, `Admin` at minimum)
- [ ] JWT validation middleware/configuration in `Program.cs`
- [ ] Basic rate limiting on auth endpoints (login/register) to deter brute force

**Backend tasks**
- [ ] Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore` and/or JWT packages (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- [ ] `User` entity + Identity configuration (or custom user table if not using Identity fully)
- [ ] `AuthService` (in `Services/`) for register/login/verify/reset logic
- [ ] JWT token generation/validation configuration (`Configuration/`)
- [ ] Role seeding (`User`, `OrganizationStaff`, `Admin`)
- [ ] `[Authorize]` / role-based policies applied as a reusable convention

**Database tasks**
- [ ] `Users` table (via Identity or custom), `Roles`, `UserRoles`
- [ ] Migration for auth-related tables
- [ ] Email verification token / password reset token storage (or Identity's built-in token providers)

**Frontend tasks**
- [ ] React + TypeScript project scaffolded (`findora-web/` or similar, separate from backend)
- [ ] Register / Login pages
- [ ] Auth token storage strategy (httpOnly cookie preferred, or secure storage if using access/refresh tokens client-side)
- [ ] Protected route wrapper (redirect unauthenticated users to login)
- [ ] Forgot/reset password pages

**API endpoints**
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/verify-email`
- `POST /api/v1/auth/resend-verification`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`

**Dependencies**
- Module 1

**Definition of Done**
- A user can register, verify their email (or stubbed verification in dev), log in, and receive a valid JWT
- Protected endpoints reject requests without a valid token (`401`)
- Role-based endpoints reject users without the required role (`403`)
- Password reset flow works end-to-end in a local test
- Frontend can register/login and store/send the auth token on subsequent requests
- Basic rate limiting confirmed on login endpoint

**Suggested Git branch name**
`feature/authentication`

---

## Module 3 — User & Profile Management

**Stage:** MVP

**Purpose:** Give authenticated users a profile and account surface, and lay
the groundwork for the reputation/report-history features used later by
matching, claims, and fraud prevention.

**Features/tasks**
- [ ] View own profile
- [ ] Update profile (name, contact preferences, avatar optional)
- [ ] Account status (`Active`, `Suspended`, `Deactivated`)
- [ ] Report-history foundation (a query surface listing a user's own lost/found reports — populated once Module 4/5 exist)
- [ ] Basic reputation field on `User` (numeric or tiered), not yet computed by any algorithm — just the schema and a manual/admin-settable value

**Backend tasks**
- [ ] `UserProfileService` for profile read/update
- [ ] Admin-only endpoint to change user status
- [ ] Extend `User` entity with profile fields (name, phone (optional), avatar URL, status, reputation score)

**Database tasks**
- [ ] Migration adding profile fields + status + reputation score to `Users`
- [ ] Index on status for admin filtering

**Frontend tasks**
- [ ] Profile view page
- [ ] Profile edit form
- [ ] Account status banner (e.g., "account suspended") when applicable

**API endpoints**
- `GET /api/v1/users/me`
- `PUT /api/v1/users/me`
- `GET /api/v1/users/{id}` (limited public view)
- `PATCH /api/v1/users/{id}/status` (Admin)

**Dependencies**
- Module 2

**Definition of Done**
- A logged-in user can view and update their profile
- Admin can change a user's status and it is enforced at login/auth middleware (suspended users cannot authenticate or act)
- Reputation field exists on the schema and is returned in the profile response (value/logic itself is finalized in Module 17)

**Suggested Git branch name**
`feature/user-profile-management`

---

## Module 4 — Lost Item Reporting

**Stage:** MVP

**Purpose:** Core MVP feature — allow users to report a lost item with the
attributes later used for search and matching.

**Features/tasks**
- [ ] Create lost report
- [ ] Update lost report (owner only, before it's matched/closed)
- [ ] Delete/archive lost report (soft delete)
- [ ] Item category (enum or lookup table: Electronics, Documents, Bags, Jewelry, Pets, Keys, Other, ...)
- [ ] Item name, description, brand, color
- [ ] Location lost (text + coordinates)
- [ ] Date/time lost
- [ ] Identifying characteristics (free text, used later for hidden-characteristic claim verification)
- [ ] Optional serial number field
- [ ] Report status (`Active`, `UnderReview`, `Matched`, `Recovered`, `Expired`, `Archived` — full lifecycle finalized in Module 15)

**Backend tasks**
- [ ] `LostReport` entity in `Models/`
- [ ] `LostReportDto` (create/update/read variants) in `DTOs/`
- [ ] `LostReportService` + `LostReportRepository`
- [ ] Ownership authorization (only the reporting user, or Admin, can edit/delete)
- [ ] Basic input validation (required fields, date not in the future, etc.)

**Database tasks**
- [ ] `LostReports` table with FK to `Users`
- [ ] `ItemCategories` lookup table (shared with Module 5)
- [ ] Migration + indexes on `CategoryId`, `Status`, `LostAt`, and location columns

**Frontend tasks**
- [ ] "Report Lost Item" form
- [ ] My Lost Reports list + detail view
- [ ] Edit/archive lost report UI

**API endpoints**
- `POST /api/v1/lost-reports`
- `GET /api/v1/lost-reports` (own reports, paginated)
- `GET /api/v1/lost-reports/{id}`
- `PUT /api/v1/lost-reports/{id}`
- `DELETE /api/v1/lost-reports/{id}` (archive)
- `PATCH /api/v1/lost-reports/{id}/status`

**Dependencies**
- Module 3

**Definition of Done**
- Authenticated user can create, view, update, and archive a lost report
- Validation rejects incomplete/invalid submissions with clear error messages
- Only the owner (or Admin) can modify a report
- Frontend form covers all required attributes and displays validation errors

**Suggested Git branch name**
`feature/lost-item-reporting`

---

## Module 5 — Found Item Reporting

**Stage:** MVP

**Purpose:** Mirror of Module 4 for found items, plus the organization
hand-off concept that Modules 13/14 build on.

**Features/tasks**
- [ ] Create found report
- [ ] Found location (text + coordinates)
- [ ] Found date/time
- [ ] Images (attachment references — actual upload/storage lives in Module 6)
- [ ] Description, category, brand, color (reuses `ItemCategories` from Module 4)
- [ ] Current holding location (with the finder / handed to an organization)
- [ ] Organization handover status (`WithFinder`, `HandedToOrganization`, `PendingPickup`) — full org workflow in Module 14

**Backend tasks**
- [ ] `FoundReport` entity in `Models/`
- [ ] `FoundReportDto` variants in `DTOs/`
- [ ] `FoundReportService` + `FoundReportRepository`
- [ ] Ownership/authorization rules (reporter, or organization staff on behalf of the org)

**Database tasks**
- [ ] `FoundReports` table (FK to `Users`, nullable FK to `Organizations` for later)
- [ ] Migration + indexes on `CategoryId`, `Status`, `FoundAt`, and location columns

**Frontend tasks**
- [ ] "Report Found Item" form
- [ ] My Found Reports list + detail view
- [ ] Handover status indicator

**API endpoints**
- `POST /api/v1/found-reports`
- `GET /api/v1/found-reports`
- `GET /api/v1/found-reports/{id}`
- `PUT /api/v1/found-reports/{id}`
- `PATCH /api/v1/found-reports/{id}/status`

**Dependencies**
- Module 3
- (Can be developed **in parallel** with Module 4 by the other developer — shares the `ItemCategories` lookup table introduced in Module 4, so agree on that schema first.)

**Definition of Done**
- Authenticated user can create, view, and update a found report
- Image field accepts a placeholder/reference (real upload wired in Module 6)
- Found reports are queryable in the same shape as lost reports (consistent DTO conventions) to simplify Module 7/8

**Suggested Git branch name**
`feature/found-item-reporting`

---

## Module 6 — Image & File Management

**Stage:** MVP (local storage) → Post-MVP (S3)

**Purpose:** Let users attach images to reports. Start with local/dev storage
behind a storage abstraction so S3 can be swapped in later without touching
callers.

**Features/tasks**
- [ ] Image upload endpoint (multipart/form-data)
- [ ] Storage abstraction (`IFileStorageService`) with a local-disk implementation first
- [ ] Amazon S3 integration **prepared but not required for MVP** (interface + config placeholders; concrete S3 implementation can land alongside Module 23)
- [ ] Image metadata (filename, content type, size, linked report, uploaded-by, uploaded-at)
- [ ] Access control (only report owner/org staff/admin can delete; images for public reports are viewable by any authenticated user)
- [ ] Image validation (file type allow-list, max size, basic malware/size sanity checks)

**Backend tasks**
- [ ] `IFileStorageService` interface in `Services/` with `LocalFileStorageService` implementation
- [ ] `ReportImage` entity linking to `LostReport`/`FoundReport`
- [ ] Upload endpoint with validation middleware/filters
- [ ] Config-driven switch between local storage and S3 (`Configuration/`)

**Database tasks**
- [ ] `ReportImages` table (FK to report, metadata columns)
- [ ] Migration + index on report FK

**Frontend tasks**
- [ ] Image upload component (drag-and-drop or file picker) on report forms
- [ ] Image gallery/preview on report detail view

**API endpoints**
- `POST /api/v1/images/upload`
- `GET /api/v1/images/{id}`
- `DELETE /api/v1/images/{id}`

**Dependencies**
- Module 4, Module 5 (needs report entities to attach images to)

**Definition of Done**
- User can upload one or more images to a lost/found report
- Invalid files (wrong type/too large) are rejected with a clear error
- Images are retrievable and deletable by authorized users only
- Storage is abstracted behind an interface so swapping to S3 later requires no controller/service-caller changes

**Suggested Git branch name**
`feature/image-file-management`

---

## Module 7 — Search & Location

**Stage:** MVP

**Purpose:** Let users find relevant lost/found reports — the entry point
into matching and claims.

**Features/tasks**
- [ ] Search lost reports (text + filters)
- [ ] Search found reports (text + filters)
- [ ] Category filtering
- [ ] Location filtering (text match first; radius search below)
- [ ] Date/time range filtering
- [ ] Radius-based search (PostGIS or basic bounding-box + Haversine distance calc if PostGIS is out of scope for MVP)
- [ ] Map integration on frontend (marker pins for search results)
- [ ] Geospatial database support decision (PostGIS extension vs. plain lat/lng columns with app-level distance calculation)

**Backend tasks**
- [ ] `SearchService` with filter/pagination support
- [ ] Query parameter binding for filters (`category`, `lat`, `lng`, `radiusKm`, `dateFrom`, `dateTo`, `keyword`)
- [ ] Decide + implement geospatial querying approach (raw SQL/PostGIS function vs. EF Core in-memory distance filter for MVP scale)

**Database tasks**
- [ ] Enable PostGIS extension **if** chosen (otherwise skip)
- [ ] Indexes to support filtered search (category, date, and either geometry index or lat/lng composite index)

**Frontend tasks**
- [ ] Search page with filter controls
- [ ] Map component (e.g., Leaflet/Mapbox) showing pins for results
- [ ] Result list synced with map view

**API endpoints**
- `GET /api/v1/search/lost-reports?category=&lat=&lng=&radiusKm=&dateFrom=&dateTo=&keyword=`
- `GET /api/v1/search/found-reports?category=&lat=&lng=&radiusKm=&dateFrom=&dateTo=&keyword=`

**Dependencies**
- Module 4, Module 5

**Definition of Done**
- Users can filter and search both lost and found reports by category, location, and date
- Radius search returns results within the specified distance, verified with test data
- Map view renders pins matching the filtered result set
- Search response times are reasonable for demo-scale data (no need for OpenSearch yet — that's Module 23)

**Suggested Git branch name**
`feature/search-location`

---

## Module 8 — Basic Matching Engine

**Stage:** MVP

**Purpose:** Ship a working, explainable, non-AI matching system before
introducing AI. This is the module that proves the core value proposition.

**Features/tasks**
- [ ] Rule-based match candidate generation between lost and found reports
- [ ] Match on category (exact)
- [ ] Match on brand (exact/fuzzy)
- [ ] Match on color (exact/fuzzy)
- [ ] Location proximity scoring (distance between lost/found coordinates)
- [ ] Time proximity scoring (closeness of lost date to found date)
- [ ] Basic text similarity on description (e.g., simple token overlap or Levenshtein — no ML yet)
- [ ] Combined weighted match score (0–100)
- [ ] Trigger point: run matching when a new report is created (against existing opposite-type reports)

**Backend tasks**
- [ ] `MatchingService` implementing the weighted scoring rules
- [ ] Background trigger (on report creation) or a scheduled/manual job to (re)compute candidates
- [ ] Configurable weights (`Configuration/`) so scoring can be tuned without code changes

**Database tasks**
- [ ] (Schema for storing candidates lives in Module 10 — `PotentialMatches` table — introduced there so scoring and storage land together)

**Frontend tasks**
- N/A directly (surfaced via Module 10's match management UI)

**API endpoints**
- `POST /api/v1/matching/run/{reportId}` (manual/admin trigger for MVP demo purposes)

**Dependencies**
- Module 4, Module 5

**Definition of Done**
- Given a lost report and a set of found reports (or vice versa), the engine returns a ranked list of candidates with a numeric score
- Scoring logic is unit-testable in isolation (no HTTP/DB required to test the scoring function itself)
- Weights are configurable, not hard-coded magic numbers scattered through the code
- Manual test: a clearly matching lost/found pair scores noticeably higher than an unrelated pair

**Suggested Git branch name**
`feature/basic-matching-engine`

---

## Module 9 — AI Matching Engine

**Stage:** Advanced (does not block MVP)

**Purpose:** Enhance (not replace) the basic matching engine with semantic
and visual similarity, as a separate Python service the .NET backend calls.
**AI never makes the final ownership decision** — it only contributes to the
match score surfaced in Module 10/11.

**Features/tasks**
- [ ] Standalone Python AI service (FastAPI or similar) — separate deployable unit, consistent with "modular monolith, not microservices" for the .NET core, but AI is inherently a different runtime
- [ ] Text embeddings for report descriptions (sentence-transformer model or hosted LLM embedding API)
- [ ] Semantic text similarity (cosine similarity on embeddings) replacing/augmenting Module 8's basic text comparison
- [ ] Image embeddings (CLIP or similar) for uploaded photos
- [ ] Image similarity scoring between lost/found report images
- [ ] AI-assisted item classification (suggest category/brand from image, user confirms)
- [ ] Combined match score merging rule-based (Module 8) + semantic + image signals
- [ ] Human-readable match explanation (e.g., "similar description, same category, found 2km away, 3 days later")
- [ ] Confidence score distinct from match score (how sure the model is, vs. how good the match is)

**Backend tasks (.NET)**
- [ ] `IAiMatchingClient` interface calling the Python service over HTTP (internal network call)
- [ ] Fallback to Module 8 scoring if the AI service is unavailable (must never hard-block matching)
- [ ] Merge AI signals into the existing `PotentialMatches` scoring pipeline

**AI service tasks (Python)**
- [ ] `/embeddings/text` endpoint
- [ ] `/embeddings/image` endpoint
- [ ] `/similarity` endpoint (given two embeddings, return a similarity score)
- [ ] `/classify` endpoint (image → suggested category/brand)
- [ ] Containerized (Docker) for later AWS deployment (Module 23)

**Database tasks**
- [ ] Columns/table to persist embeddings or a reference to where they're stored (e.g., OpenSearch vector index in Module 23, or a Postgres `vector` column via `pgvector` if used sooner)
- [ ] `MatchExplanation` text field on `PotentialMatches` (Module 10)

**Frontend tasks**
- [ ] Display AI match explanation and confidence score alongside the match score on the match detail view

**API endpoints**
- Internal service-to-service only (not public): `POST /internal/ai/embeddings/text`, `POST /internal/ai/embeddings/image`, `POST /internal/ai/similarity`, `POST /internal/ai/classify`

**Dependencies**
- Module 8 (extends it, doesn't replace it)
- Module 6 (needs stored images to embed)

**Definition of Done**
- AI service runs locally (Docker) and responds to embedding/similarity/classify requests
- .NET backend calls the AI service and merges its score into the combined match score
- If the AI service is down, matching still works using Module 8's rule-based score only (graceful degradation, verified by a test)
- Match explanations are human-readable and shown in the UI
- No code path treats an AI match/confidence score as a final ownership decision

**Suggested Git branch name**
`feature/ai-matching-engine`

---

## Module 10 — Match Management

**Stage:** MVP

**Purpose:** Persist and manage the match candidates produced by Module 8
(and later enriched by Module 9), and let users act on them.

**Features/tasks**
- [ ] `PotentialMatch` records linking a lost report ↔ found report
- [ ] Match status (`Pending`, `Accepted`, `Rejected`, `Expired`)
- [ ] Match score (from Module 8, enriched later by Module 9)
- [ ] Match explanation field
- [ ] User notification when a new potential match is found (basic version — full notification system in Module 16)
- [ ] Accept/reject a potential match (accepting moves toward Module 11 claims)

**Backend tasks**
- [ ] `PotentialMatch` entity + `MatchService`
- [ ] Endpoint to list matches for the current user's reports
- [ ] Accept/reject actions with authorization (only involved parties)
- [ ] Hook: on match creation, enqueue a notification (Module 16 will consume this)

**Database tasks**
- [ ] `PotentialMatches` table (FK to `LostReports`, `FoundReports`, score, explanation, status, timestamps)
- [ ] Migration + index on status and report FKs

**Frontend tasks**
- [ ] "Potential Matches" list per report
- [ ] Match detail view (score, explanation, images side by side)
- [ ] Accept/reject buttons

**API endpoints**
- `GET /api/v1/matches`
- `GET /api/v1/matches/{id}`
- `POST /api/v1/matches/{id}/accept`
- `POST /api/v1/matches/{id}/reject`

**Dependencies**
- Module 8 (and, once available, Module 9)

**Definition of Done**
- Potential matches are created and persisted automatically when Module 8's engine finds a candidate above a configurable threshold
- Users can view and accept/reject matches for their own reports
- Accepting a match is the entry point into Module 11 (claims)

**Suggested Git branch name**
`feature/match-management`

---

## Module 11 — Ownership Claims

**Stage:** MVP

**Purpose:** Let a user formally claim a found item once a match is
accepted, with verification questions to reduce false claims.

**Features/tasks**
- [ ] Submit claim against an accepted match
- [ ] Claim validation (required fields, one active claim per match)
- [ ] Hidden item characteristics captured at report time (Module 4) used as verification questions
- [ ] Proof-of-ownership Q&A (claimant answers, finder/org compares to hidden characteristics)
- [ ] Claim status (`Submitted`, `UnderReview`, `Approved`, `Rejected`)
- [ ] Admin/organization review action
- [ ] Fraud prevention foundation: flag claims with mismatched answers or unusually fast submission (full system in Module 17)

**Backend tasks**
- [ ] `Claim` entity + `ClaimService`
- [ ] Claim creation tied to a `PotentialMatch`
- [ ] Answer comparison logic against hidden characteristics (simple match/no-match for MVP; scoring can improve later)
- [ ] Review endpoint restricted to Admin/OrganizationStaff

**Database tasks**
- [ ] `Claims` table (FK to `PotentialMatch`, claimant, status, timestamps)
- [ ] `ClaimAnswers` table (question/answer pairs, correctness flag)
- [ ] Migration + indexes on status and match FK

**Frontend tasks**
- [ ] "Submit Claim" flow with verification questions
- [ ] Claim status view for claimant
- [ ] Review queue UI for Admin/Org staff

**API endpoints**
- `POST /api/v1/claims`
- `GET /api/v1/claims/{id}`
- `GET /api/v1/claims` (mine, or all for Admin/Org)
- `POST /api/v1/claims/{id}/answers`
- `PATCH /api/v1/claims/{id}/status`

**Dependencies**
- Module 10

**Definition of Done**
- A user can submit a claim with answers to verification questions tied to a specific match
- Admin/org staff can review a claim and approve/reject it
- Claim decisions update the linked report status (feeds into Module 15 lifecycle)
- A user cannot submit multiple simultaneous active claims on the same match

**Suggested Git branch name**
`feature/ownership-claims`

---

## Module 12 — Secure Communication

**Stage:** Post-MVP

**Purpose:** Let matched users coordinate handover without exposing private
contact details directly.

**Features/tasks**
- [ ] User-to-user messaging scoped to a match/claim context
- [ ] No direct exposure of email/phone in message payloads or profiles
- [ ] Conversation management (one conversation per match/claim)
- [ ] Message authorization (only the two involved parties, or org staff mediating)
- [ ] Report/block functionality for abuse prevention

**Backend tasks**
- [ ] `Conversation` and `Message` entities
- [ ] `MessagingService` with authorization checks per conversation
- [ ] Report/block endpoints feeding into Module 17's trust signals

**Database tasks**
- [ ] `Conversations` table (FK to match/claim, participants)
- [ ] `Messages` table (FK to conversation, sender, body, timestamp, read status)
- [ ] `UserBlocks` / `Reports` table

**Frontend tasks**
- [ ] Conversation list
- [ ] Message thread UI (basic polling is fine for MVP; real-time/WebSocket is a later enhancement)
- [ ] Report/block UI

**API endpoints**
- `GET /api/v1/conversations`
- `POST /api/v1/conversations`
- `GET /api/v1/conversations/{id}/messages`
- `POST /api/v1/conversations/{id}/messages`
- `POST /api/v1/conversations/{id}/report`
- `POST /api/v1/conversations/{id}/block`

**Dependencies**
- Module 10, Module 11 (a conversation is always tied to a match or claim)

**Definition of Done**
- Two matched users can exchange messages without either seeing the other's raw email/phone
- Only conversation participants (and authorized org/admin staff where relevant) can read/send messages
- Report/block actions are recorded and retrievable by Module 17/18

**Suggested Git branch name**
`feature/secure-communication`

---

## Module 13 — Organization Management

**Stage:** Post-MVP

**Purpose:** Introduce organizations (e.g., campus security, transit lost &
found, malls) as first-class actors distinct from individual users.

**Features/tasks**
- [ ] Organization registration
- [ ] Organization verification (Admin-approved before the org can act as an org)
- [ ] Organization profile (name, type, contact info)
- [ ] Organization locations (one org can have multiple physical sites)
- [ ] Staff/users linked to an organization
- [ ] Organization-scoped roles (`OrgAdmin`, `OrgStaff`)

**Backend tasks**
- [ ] `Organization`, `OrganizationLocation`, `OrganizationStaff` entities
- [ ] `OrganizationService`
- [ ] Verification workflow (Admin approval endpoint)
- [ ] Org-scoped authorization policies (a staff member can only act within their org)

**Database tasks**
- [ ] `Organizations`, `OrganizationLocations`, `OrganizationStaff` tables
- [ ] Migration + FK from `OrganizationStaff` to `Users`

**Frontend tasks**
- [ ] Organization registration form
- [ ] Organization profile/settings page
- [ ] Staff management UI (invite/remove staff)
- [ ] Admin verification queue for pending organizations

**API endpoints**
- `POST /api/v1/organizations`
- `GET /api/v1/organizations/{id}`
- `PUT /api/v1/organizations/{id}`
- `POST /api/v1/organizations/{id}/staff`
- `POST /api/v1/organizations/{id}/verify` (Admin)
- `GET /api/v1/organizations/{id}/locations`

**Dependencies**
- Module 2 (roles), Module 3

**Definition of Done**
- An organization can register and appears in an Admin verification queue
- Once verified, org staff can log in and act with organization-scoped permissions
- Unverified organizations cannot use organization-only features (enforced server-side, not just hidden in UI)

**Suggested Git branch name**
`feature/organization-management`

---

## Module 14 — Organization Lost & Found Portal

**Stage:** Post-MVP

**Purpose:** Give verified organizations a dedicated workflow for handling
found items at scale (e.g., a campus security desk logging dozens of items).

**Features/tasks**
- [ ] Register found items on behalf of the organization
- [ ] Review claims submitted against organization-held items
- [ ] Verify claimant users (identity check before handover)
- [ ] Manage physical items (holding location, shelf/bin reference)
- [ ] Update item status
- [ ] Record handovers (who picked up, when, verified by whom)
- [ ] Generate basic reports (items logged, items returned, pending items)

**Backend tasks**
- [ ] Extend `FoundReportService` with org-scoped creation/listing
- [ ] `HandoverRecord` entity + service
- [ ] Org-scoped claim review endpoints (reuses Module 11's `Claim` entity, filtered by org)

**Database tasks**
- [ ] `HandoverRecords` table (FK to found report, claimant, verified-by staff member, timestamp)
- [ ] Migration + indexes on org FK across found reports/claims

**Frontend tasks**
- [ ] Org dashboard: item intake form
- [ ] Org claim review queue
- [ ] Handover recording UI
- [ ] Basic org reporting view (counts/lists, not full analytics — that's Module 19)

**API endpoints**
- `POST /api/v1/organizations/{id}/found-items`
- `GET /api/v1/organizations/{id}/claims`
- `PATCH /api/v1/organizations/{id}/claims/{claimId}`
- `POST /api/v1/organizations/{id}/handovers`
- `GET /api/v1/organizations/{id}/reports`

**Dependencies**
- Module 13, Module 5, Module 11

**Definition of Done**
- Org staff can log a found item, review a claim against it, and record a handover
- Handover records are immutable once created (audit trail)
- Basic counts (items logged/returned/pending) are visible on the org dashboard

**Suggested Git branch name**
`feature/organization-portal`

---

## Module 15 — Item Lifecycle & Recovery

**Stage:** MVP (core state machine) — formalizes what Modules 4/5/10/11/14 already produce

**Purpose:** Make the report lifecycle an explicit, validated state machine
instead of an ad-hoc status field, so every module updates status consistently.

**Features/tasks**
- [ ] Formal lifecycle: `Reported → UnderReview → PotentialMatch → ClaimSubmitted → OwnershipVerified → RecoveryScheduled → Recovered`
- [ ] Alternate path: `Reported → NoMatch → Expired/Archived`
- [ ] State transition validation (only legal transitions allowed, enforced server-side)
- [ ] Transition history/audit trail per report
- [ ] Automatic expiry job for stale reports with no match after N days (configurable)

**Backend tasks**
- [ ] `ReportLifecycleService` centralizing all status transitions (other services call this instead of setting status directly)
- [ ] State machine definition (allowed transitions table/switch, not scattered `if` checks)
- [ ] Background job (hosted service) for auto-expiry

**Database tasks**
- [ ] `ReportStatusHistory` table (FK to report, from-status, to-status, changed-by, timestamp)
- [ ] Migration

**Frontend tasks**
- [ ] Status timeline/stepper component on report detail view
- [ ] Visual indication of current stage

**API endpoints**
- `PATCH /api/v1/reports/{id}/lifecycle` (internal-ish; typically driven by other modules' actions rather than called directly by the frontend)
- `GET /api/v1/reports/{id}/history`

**Dependencies**
- Module 4, Module 5, Module 10, Module 11, Module 14

**Definition of Done**
- Illegal status transitions are rejected (e.g., cannot go from `Reported` directly to `Recovered`)
- Every status change is recorded in the history table
- Modules 4, 5, 10, 11, 14 all go through `ReportLifecycleService` rather than writing status directly
- Auto-expiry job correctly archives stale unmatched reports in a local test run

**Suggested Git branch name**
`feature/item-lifecycle`

---

## Module 16 — Notifications

**Stage:** MVP (in-app + email) → Post-MVP (push)

**Purpose:** Keep users informed at each meaningful event without requiring
them to poll the app.

**Features/tasks**
- [ ] Notification events: potential match found, match response, claim submitted, claim approved/rejected, organization accepted an item, item recovered, additional information required
- [ ] In-app notification list (read/unread)
- [ ] Email notifications (transactional email provider — can start as a simple SMTP/log-based stub in local dev)
- [ ] Push notifications — **later stage**, after in-app/email are solid

**Backend tasks**
- [ ] `INotificationService` abstraction with in-app + email channel implementations
- [ ] `Notification` entity + event hooks from Modules 10, 11, 14, 15
- [ ] Email templates for each event type
- [ ] Background/queued dispatch (in-process queue for MVP; can move to Amazon SQS in Module 23 without changing callers)

**Database tasks**
- [ ] `Notifications` table (FK to user, type, payload, read status, timestamps)
- [ ] Migration + index on user + read status

**Frontend tasks**
- [ ] Notification bell/list component
- [ ] Mark-as-read interaction
- [ ] Email preference toggle (optional for MVP)

**API endpoints**
- `GET /api/v1/notifications`
- `PATCH /api/v1/notifications/{id}/read`

**Dependencies**
- Module 10, Module 11, Module 14, Module 15 (these are the event sources)

**Definition of Done**
- Each listed event reliably produces both an in-app notification and an email (or logged email in dev)
- Users can view and mark notifications as read
- Notification dispatch is abstracted so swapping in SQS-backed delivery later doesn't change calling code

**Suggested Git branch name**
`feature/notifications`

---

## Module 17 — Fraud Prevention & Trust

**Stage:** Advanced (basic pieces started in Modules 3/11; full system is later-stage)

**Purpose:** Reduce false claims and abuse while keeping **final ownership
decisions under human control** at all times.

**Features/tasks**
- [ ] Hidden item characteristics (already captured in Module 4; formalize how strictly they're weighted in claim review)
- [ ] Proof-of-ownership questions (already in Module 11; expand question bank/quality)
- [ ] User reputation scoring (build real logic on top of Module 3's schema — e.g., successful returns, false claim count)
- [ ] Report history surfaced to reviewers (how many reports/claims this user has made)
- [ ] Suspicious claim detection (heuristics: rapid-fire claims, mismatched answers, brand-new account claiming high-value item)
- [ ] Rate limiting on report/claim submission endpoints
- [ ] Organization verification enforcement (tie into Module 13's verified flag)
- [ ] High-value item flag requiring mandatory Admin review before recovery

**Backend tasks**
- [ ] `TrustService` computing reputation and suspicious-activity flags
- [ ] Rate-limiting middleware/policy on sensitive endpoints
- [ ] `IsHighValue` flag on reports (manual or threshold-based) forcing an Admin review gate in Module 15's lifecycle

**Database tasks**
- [ ] `TrustSignals`/`SuspiciousActivityFlags` table
- [ ] Migration + indexes for admin queries

**Frontend tasks**
- [ ] Reputation indicator on user profile (for reviewers, not necessarily public)
- [ ] Suspicious-flag badge in Admin/Org review queues

**API endpoints**
- `GET /api/v1/admin/suspicious-claims`
- (Reputation surfaced as part of existing user/claim payloads rather than new endpoints)

**Dependencies**
- Module 11 (claims), Module 3 (user reputation schema)

**Definition of Done**
- Reviewers see reputation and suspicious-activity flags when reviewing a claim
- High-value items cannot reach `Recovered` status without explicit Admin approval
- Rate limits confirmed to block rapid repeated submissions in a local test
- No automated process can auto-approve a claim — a human always makes the final call

**Suggested Git branch name**
`feature/fraud-prevention`

---

## Module 18 — Admin Dashboard

**Stage:** Post-MVP (basic user/report management) → Advanced (full dashboard)

**Purpose:** Give platform admins the tools to moderate, verify, and oversee
the system end-to-end.

**Features/tasks**
- [ ] User management (view, suspend, change role)
- [ ] Report moderation (view/hide inappropriate reports)
- [ ] Suspicious activity review (consumes Module 17's flags)
- [ ] Claim disputes (escalation view when a claim decision is contested)
- [ ] Organization verification queue (consumes Module 13)
- [ ] High-value item review queue (consumes Module 17)
- [ ] Platform statistics summary (consumes Module 19)
- [ ] Audit logs (who did what, when — spans multiple modules)

**Backend tasks**
- [ ] Admin-only controller set with role policy enforcement
- [ ] `AuditLog` entity + write hooks from key actions across modules (status changes, verifications, approvals)

**Database tasks**
- [ ] `AuditLogs` table (actor, action, entity type/id, timestamp, metadata)
- [ ] Migration + index on entity/action for querying

**Frontend tasks**
- [ ] Admin dashboard shell/navigation
- [ ] User management table
- [ ] Report/claim moderation views
- [ ] Organization verification queue UI
- [ ] Audit log viewer

**API endpoints**
- `GET /api/v1/admin/users`
- `GET /api/v1/admin/reports`
- `GET /api/v1/admin/claims`
- `GET /api/v1/admin/organizations`
- `GET /api/v1/admin/audit-logs`
- `GET /api/v1/admin/stats`

**Dependencies**
- Module 2 (roles) for basic user management; full dashboard depends on Modules 13, 17, 19

**Definition of Done**
- Admin can view and act on users, reports, claims, and organizations from one place
- Every sensitive admin action is recorded in the audit log
- Basic version (user management + moderation) works without waiting for Module 19's analytics to be complete

**Suggested Git branch name**
`feature/admin-dashboard`

---

## Module 19 — Analytics

**Stage:** Advanced

**Purpose:** Give organizations and admins insight into recovery performance
and trends.

**Features/tasks**
- [ ] Recovery rate (recovered / total reports, overall and per category)
- [ ] Average recovery time (from report to `Recovered`)
- [ ] Most common lost-item categories
- [ ] Common lost-item locations (heatmap-style aggregation)
- [ ] Unresolved items count/aging
- [ ] Seasonal patterns (reports over time, by month/season)

**Backend tasks**
- [ ] `AnalyticsService` with aggregation queries (consider read-optimized queries/materialized views if performance becomes an issue)
- [ ] Org-scoped vs. platform-wide analytics access control

**Database tasks**
- [ ] Indexes to support aggregation queries efficiently
- [ ] Optional materialized view(s) for expensive aggregates

**Frontend tasks**
- [ ] Analytics dashboard with charts (recovery rate, category breakdown, time-to-recovery trend, location heatmap)

**API endpoints**
- `GET /api/v1/analytics/recovery-rate`
- `GET /api/v1/analytics/avg-recovery-time`
- `GET /api/v1/analytics/categories`
- `GET /api/v1/analytics/locations`

**Dependencies**
- Module 15 (lifecycle data is the source of truth for these metrics), Module 14 (org-scoped data)

**Definition of Done**
- Dashboard renders accurate metrics against seeded/demo data
- Org users see only their own organization's data; admins see platform-wide data
- Queries perform acceptably at demo scale (no need for a full data warehouse)

**Suggested Git branch name**
`feature/analytics`

---

## Module 20 — Security Hardening

**Stage:** Infrastructure (continuous, with a dedicated hardening pass before production)

**Purpose:** Formal security pass across the whole application before it's
considered production-ready.

**Features/tasks**
- [ ] Enforce HTTPS everywhere (including local dev certs and production config)
- [ ] Review authorization checks on every endpoint (no missing `[Authorize]`/ownership checks)
- [ ] Input validation review (all DTOs, all controllers)
- [ ] Rate limiting review (beyond auth endpoints — claims, messaging, uploads)
- [ ] Secure file upload review (content-type sniffing, size limits, storage isolation)
- [ ] Private S3 objects (no public bucket access; signed URLs for access)
- [ ] Sensitive data protection (PII encryption at rest where appropriate, no secrets in logs)
- [ ] Audit logging completeness review (builds on Module 18)
- [ ] Secure AWS IAM policies (least privilege per service)
- [ ] General API security review (CORS policy tightened, security headers, dependency vulnerability scan)
- [ ] Ownership/access checks re-verified across all resource types (reports, claims, messages, images)

**Backend tasks**
- [ ] Security-focused code review pass across all controllers/services
- [ ] Add/verify security headers middleware
- [ ] Dependency vulnerability scan (`dotnet list package --vulnerable`, `npm audit`)

**Database tasks**
- [ ] Review sensitive columns for encryption-at-rest needs
- [ ] Confirm least-privilege DB user for the application (not superuser)

**Frontend tasks**
- [ ] Review token storage approach for XSS/CSRF exposure
- [ ] Sanitize any user-generated content rendered as HTML

**API endpoints**
- None new — hardens existing endpoints

**Dependencies**
- All MVP + Post-MVP modules (this is a cross-cutting pass, best done once the core surface area exists, and repeated before each major release)

**Definition of Done**
- Security checklist above is completed and documented
- No endpoint is missing an authorization check
- `dotnet list package --vulnerable` and `npm audit` show no unaddressed high/critical issues
- S3 buckets (once in use) are confirmed private with signed-URL access only

**Suggested Git branch name**
`feature/security-hardening`

---

## Module 21 — Testing

**Stage:** Infrastructure (continuous, started from Module 2 onward)

**Purpose:** Ensure correctness and prevent regressions as the two
developers work in parallel.

**Features/tasks**
- [ ] Unit tests for services (matching logic, lifecycle transitions, claim validation especially)
- [ ] Integration tests for repositories/DbContext (using a test PostgreSQL instance or Testcontainers)
- [ ] API tests for controllers (`WebApplicationFactory`-based)
- [ ] Authentication tests (register/login/token validation)
- [ ] Authorization tests (role/ownership checks per endpoint)
- [ ] Matching tests (Module 8 scoring correctness, Module 9 graceful degradation)
- [ ] Claim/ownership tests (verification question logic, one-active-claim rule)
- [ ] File upload tests (validation rules, access control)
- [ ] Security tests (rejected unauthorized access attempts)
- [ ] End-to-end tests (key user journeys: report → match → claim → recover)

**Backend tasks**
- [ ] `Findora.API.Tests` project (xUnit) added to the solution
- [ ] Test project referenced in `Findora.sln`
- [ ] CI wired to run tests on every PR (ties into Module 24)

**Database tasks**
- [ ] Test database strategy (Testcontainers PostgreSQL or a dedicated test schema/instance)

**Frontend tasks**
- [ ] Component tests (React Testing Library) for key forms/flows
- [ ] End-to-end tests (Playwright/Cypress) for the golden path

**API endpoints**
- N/A

**Dependencies**
- Applies incrementally to every module from Module 2 onward; this module tracks the **formal test suite setup and golden-path E2E coverage**, not the very first unit test (write tests alongside each module as you go)

**Definition of Done**
- Test project builds and runs via `dotnet test`
- Core services (auth, matching, lifecycle, claims) have unit test coverage
- At least one E2E test covers the full lost → found → match → claim → recover path
- CI fails the build if tests fail

**Suggested Git branch name**
`feature/testing`

---

## Module 22 — Docker & Local Development

**Stage:** Infrastructure (fast-track right after Module 1)

**Purpose:** Make local setup identical and fast for both developers.

**Features/tasks**
- [ ] `Dockerfile` for `Findora.API`
- [ ] `docker-compose.yml` with PostgreSQL (and later: AI service, when Module 9 lands)
- [ ] Environment variable conventions (`.env.example` committed, real `.env` gitignored)
- [ ] Local development setup documented in `README.md`
- [ ] Production configuration separation (`appsettings.Production.json` / env-based overrides, no secrets committed)

**Backend tasks**
- [ ] Multi-stage `Dockerfile` (build stage + slim runtime stage)
- [ ] Health check wired into `docker-compose.yml` (depends on Module 0's `/api/health`)

**Database tasks**
- [ ] PostgreSQL service in `docker-compose.yml` with a named volume for persistence
- [ ] Document how migrations are applied against the containerized database

**Frontend tasks**
- [ ] (Optional) frontend dev container/service added to `docker-compose.yml` once the React app exists

**API endpoints**
- N/A

**Dependencies**
- Module 1 (needs something to containerize/connect to)

**Definition of Done**
- `docker compose up` brings up API + PostgreSQL with a working connection, no manual PostgreSQL install required
- `.env.example` documents every required environment variable
- Both developers confirm they can run the full stack locally via Docker

**Suggested Git branch name**
`feature/docker-local-dev`

---

## Module 23 — AWS Deployment

**Stage:** Advanced (introduced progressively, only after the app is stable locally)

**Purpose:** Move from local Docker to a real AWS environment, piece by
piece — never all at once.

**Features/tasks**
- [ ] Amazon ECR (container registry) for API image
- [ ] ECS/Fargate service running the containerized API
- [ ] RDS PostgreSQL (replacing local Postgres for staging/production)
- [ ] S3 (replacing local file storage from Module 6 — concrete implementation of `IFileStorageService`)
- [ ] SQS (replacing the in-process notification queue from Module 16)
- [ ] OpenSearch (vector/text search — replacing/augmenting Module 7's search and Module 9's embedding similarity at scale)
- [ ] CloudFront (CDN for images/static assets, once S3 is in place)
- [ ] Lambda — only where genuinely useful (e.g., scheduled expiry job from Module 15, or image thumbnail generation); not a default choice
- [ ] CloudWatch (logs/metrics/alarms)
- [ ] IAM (least-privilege roles per service — coordinated with Module 20)

**Backend tasks**
- [ ] `S3FileStorageService` implementing `IFileStorageService`
- [ ] `SqsNotificationDispatcher` implementing `INotificationService`'s queue interface
- [ ] OpenSearch client integration for search/vector queries
- [ ] Environment-based configuration for AWS resource names/regions

**Database tasks**
- [ ] RDS instance provisioned, migrations applied via CI/CD pipeline (Module 24)
- [ ] Connection string via secrets manager, not committed config

**Frontend tasks**
- [ ] Point frontend build at deployed API URL (via environment config, not hard-coded)
- [ ] CloudFront-served image URLs

**API endpoints**
- No new application endpoints — this module is infrastructure/integration

**Dependencies**
- Module 22 (Docker), Module 6 (S3 target), Module 16 (SQS target), Module 7/9 (OpenSearch target); should not start until the app is feature-stable locally

**Definition of Done**
- API runs on ECS/Fargate against RDS PostgreSQL in a staging AWS environment
- Images upload to and serve from S3 (private, signed URLs)
- Notifications dispatch through SQS
- Search queries OpenSearch successfully for at least the MVP search use case
- CloudWatch shows logs/metrics for the deployed service
- IAM roles follow least privilege (verified against Module 20's checklist)

**Suggested Git branch name**
`feature/aws-deployment`

---

## Module 24 — CI/CD

**Stage:** Infrastructure (basic pipeline fast-tracked right after Module 0; full pipeline lands here)

**Purpose:** Automate build, test, and deployment so neither developer is
manually running these steps before every merge.

**Features/tasks**
- [ ] GitHub Actions workflow: build + test on every PR (fast-tracked early)
- [ ] Docker image build step (once Module 22 exists)
- [ ] Push image to ECR (once Module 23 exists)
- [ ] Deployment pipeline to ECS/Fargate (staging first, production gated)
- [ ] Environment-specific configuration/secrets via GitHub Actions secrets
- [ ] Production deployment workflow (manual approval gate recommended for a student project)

**Backend tasks**
- [ ] `.github/workflows/ci.yml` (build/test, runs on PR to `dev`/`main`)
- [ ] `.github/workflows/deploy.yml` (build image, push to ECR, deploy — runs on merge to `main`)

**Database tasks**
- [ ] Migration-apply step included in the deployment pipeline (with a safe rollback plan documented)

**Frontend tasks**
- [ ] Frontend build/test/lint step added to CI
- [ ] Frontend deployment step (e.g., to S3/CloudFront or a simple static host) once Module 23 infra exists

**API endpoints**
- N/A

**Dependencies**
- Basic build/test pipeline: Module 0 (fast-tracked). Full deploy pipeline: Module 22, Module 23.

**Definition of Done**
- Every PR automatically runs build + backend tests + frontend lint/tests, and merging is blocked on failure
- Merges to `main` automatically build and push a Docker image to ECR
- A documented, working path exists from `main` merge → deployed staging environment
- Secrets are never committed; all pipeline secrets live in GitHub Actions secrets

**Suggested Git branch name**
`feature/ci-cd`

---

## Module 25 — Final Integration & Production Readiness

**Stage:** Infrastructure (final pass)

**Purpose:** Confirm the whole system works together end-to-end and is
genuinely ready to demo/hand in/deploy.

**Features/tasks**
- [ ] Full user workflow verified: Lost → Found → Match → Claim → Verification → Recovery
- [ ] Organization workflow verified end-to-end
- [ ] Notification workflow verified end-to-end
- [ ] AI workflow verified end-to-end (with graceful fallback confirmed)
- [ ] Final security review (re-run Module 20's checklist)
- [ ] Basic performance/load sanity check (demo-scale, not enterprise load testing)
- [ ] Error handling review across all modules (no unhandled exceptions surfacing raw errors)
- [ ] Documentation pass (`README.md`, API docs via Swagger, setup instructions all current)
- [ ] Deployment verified in the target AWS environment

**Backend tasks**
- [ ] Cross-module smoke test suite run against a staging deployment
- [ ] Final review of all `TODO`/`FIXME` comments in the codebase

**Database tasks**
- [ ] Final migration review (no pending/uncommitted migrations)
- [ ] Backup/restore process documented for the production database

**Frontend tasks**
- [ ] Full click-through of every major flow in a production-like environment
- [ ] Cross-browser/basic responsive check

**API endpoints**
- N/A — validation pass, not new features

**Dependencies**
- All prior modules

**Definition of Done**
- A single reviewer can walk through the entire lost → found → match → claim → recovery flow (individual + organization paths) without hitting a broken step
- Documentation is accurate and sufficient for someone outside the team to set up and run the project
- Staging deployment matches what's demoed/submitted

**Suggested Git branch name**
`feature/final-integration`

---

## Team Development Strategy

With two developers, the goal is to **maximize parallel work while never
having one person blocked waiting on the other's unmerged code.** Use
`Developer A` / `Developer B` as role placeholders — assign real names once
you agree on the split below.

### Suggested default split

**Developer A — Backend/API, Database, Auth, Core Business Logic**
- Owns `Findora.API` service/repository logic, EF Core migrations, and the rule-based/AI matching, lifecycle, and claims logic.

**Developer B — Frontend, UI, API Integration, Maps, UX**
- Owns the React + TypeScript app, consumes the API contracts Developer A defines, builds map/search UX and all user-facing flows.

This is a **default**, not a fixed rule — swap based on actual comfort with
backend vs. frontend once you both start Module 0/1.

### Module-by-module assignment

| Module | Name | Owner | Parallelizable? | Depends on (must be merged first) | Merge gate before next module |
|---|---|---|---|---|---|
| 0 | Project Foundation | Shared (pair once, split cleanup after) | No — do together first | — | Foundation merged to `dev` |
| 1 | Database Foundation | Developer A | No | 0 | DbContext + migration tooling merged |
| 2 | Authentication & Authorization | Developer A (backend) + Developer B (frontend forms) | Partially — B can build UI against a mocked API while A finishes backend | 1 | Auth endpoints + login/register UI merged |
| 3 | User & Profile Management | Developer A (backend) + Developer B (frontend) | Yes, once Module 2's contract is agreed | 2 | Profile endpoints + UI merged |
| 4 | Lost Item Reporting | Developer A (backend) + Developer B (frontend) | Yes | 3 | Lost report CRUD + form merged |
| 5 | Found Item Reporting | Developer A (backend) + Developer B (frontend) | **Yes — fully parallel with Module 4** if `ItemCategories` schema is agreed first | 3 | Found report CRUD + form merged |
| 6 | Image & File Management | Developer A | Yes, once 4/5 entities exist | 4, 5 | Upload/storage abstraction merged |
| 7 | Search & Location | Developer A (backend) + Developer B (map UI) | Yes | 4, 5 | Search endpoints + map/filter UI merged |
| 8 | Basic Matching Engine | Developer A | Mostly solo (backend logic); B can build UI stubs in parallel | 4, 5 | Scoring service merged + unit tested |
| 9 | AI Matching Engine | **Shared** (A wires .NET client, B or A builds Python service depending on comfort — genuinely collaborative) | Partially | 8, 6 | AI service + fallback path merged |
| 10 | Match Management | Developer A (backend) + Developer B (frontend) | Yes | 8 | Match list/accept/reject merged |
| 11 | Ownership Claims | Developer A (backend) + Developer B (frontend) | Yes | 10 | Claim flow merged |
| 12 | Secure Communication | Developer B (frontend-heavy) + Developer A (backend) | Yes | 10, 11 | Messaging merged |
| 13 | Organization Management | Developer A (backend) + Developer B (frontend) | Yes | 2, 3 | Org registration/verification merged |
| 14 | Organization Portal | Developer A (backend) + Developer B (frontend) | Yes | 13, 5, 11 | Org portal merged |
| 15 | Item Lifecycle & Recovery | Developer A | No — cross-cutting, needs care | 4, 5, 10, 11, 14 | Lifecycle service merged, other modules updated to use it |
| 16 | Notifications | Developer A (backend) + Developer B (in-app UI) | Yes | 10, 11, 14, 15 | Notification dispatch + UI merged |
| 17 | Fraud Prevention & Trust | **Shared** | Partially | 11, 3 | Trust signals + review-queue integration merged |
| 18 | Admin Dashboard | Developer B (frontend-heavy) + Developer A (endpoints) | Yes | 2 (basic), 13/17/19 (full) | Admin views merged |
| 19 | Analytics | Developer A (queries) + Developer B (charts) | Yes | 15, 14 | Analytics endpoints + dashboard merged |
| 20 | Security Hardening | **Shared** | No — full-team review pass | All MVP/Post-MVP modules | Checklist completed and documented |
| 21 | Testing | **Shared** (ongoing) + Developer A (backend), Developer B (frontend/E2E) | Yes, continuously | Ongoing from Module 2 | Test suite green in CI |
| 22 | Docker & Local Dev | Developer A | No | 1 | `docker compose up` works for both devs |
| 23 | AWS Deployment | **Shared** | No — coordinate carefully | 22, 6, 16, 7/9 | Staging deployment verified |
| 24 | CI/CD | Developer A (or whoever sets up GitHub Actions first) | No, but low effort | 0 (basic), 22/23 (full) | CI green, deploy pipeline documented |
| 25 | Final Integration | **Shared** | No | Everything | Full walkthrough passes |

### Collaboration points (do these together, not solo)

- **Module 0** — agree on conventions before anyone writes feature code.
- **Module 9 (AI Matching)** — spans .NET + Python; needs both developers aligned on the contract between services.
- **Module 15 (Lifecycle)** — touches almost every other module's status handling; a solo change here risks breaking the other developer's in-flight work, so coordinate timing.
- **Module 17 (Fraud Prevention)**, **Module 20 (Security Hardening)**, **Module 23 (AWS Deployment)**, **Module 25 (Final Integration)** — cross-cutting, highest risk of merge conflicts and blind spots if done solo.

### Working in parallel safely

1. **Agree on shared contracts before splitting work.** For any module split between backend/frontend (e.g., Module 4), settle the DTO shape first — write it down in the PR description or a shared doc — so the frontend dev isn't blocked waiting for the backend to finish, and doesn't have to rewrite integration code later.
2. **Shared schema modules (like `ItemCategories` in Module 4/5) must be agreed before either branch starts.** Whoever starts first adds the migration; the other rebases onto it.
3. **Never both edit `Program.cs` service registration in unrelated branches at the same time** without communicating — it's a common conflict point. Keep registrations grouped by module/comment block to ease merges.
4. **Merge frequently, in small PRs**, rather than each developer sitting on a large branch for a full module. A module can still be split into several small PRs (e.g., entity+migration, then service, then controller, then frontend).
5. **Don't start a module whose Dependencies aren't merged into `dev` yet** — this is the core rule that prevents duplicated/conflicting work.

---

## Git Strategy

The repository already has `main` (stable) and `dev` (integration) branches
with a GitHub remote (`origin`). Use this flow:

- `main` — always deployable/demoable. Only updated via reviewed merge from `dev` at milestones.
- `dev` — integration branch. All feature branches branch **from** `dev` and merge **back into** `dev`.
- `feature/<module-name>` — one branch per module (or per sub-task within a large module).

### Branch naming

```
feature/<module-name>
```

Examples:
- `feature/authentication`
- `feature/lost-item-reporting`
- `feature/found-item-reporting`
- `feature/matching-engine`
- `feature/ownership-claims`

(Exact names per module are listed in each module section above.)

### Pull requests

- Every change to `dev` or `main` goes through a PR — no direct commits to either.
- PR description should state: which module/task it addresses, what changed, and how it was tested.
- Link the PR to the relevant module number in this file.

### Code review

- At least one review from the other developer before merging, even on a two-person team — a second pair of eyes catches missed authorization checks, validation gaps, and lifecycle violations.
- Reviewer checks: does it follow the folder conventions from Module 0, does it respect module Dependencies, are there tests for new logic.

### Commit messages

- Use short, imperative-mood messages: `Add lost report creation endpoint`, not `added stuff` or `fix`.
- Reference the module when useful: `Module 4: add lost report validation`.

### Avoiding direct commits to `main`

- Branch protection should require PRs into `main` (and ideally `dev`) with at least one approval and passing CI (once Module 24 lands).

### Keeping branches small

- Prefer one module → several small PRs (entity/migration → service → controller → frontend) over one giant PR per module.
- A branch that's open for more than a few days risks painful conflicts — merge early and often.

### Resolving merge conflicts

- Rebase your feature branch onto the latest `dev` before opening a PR, not after.
- For migration conflicts specifically: never edit a teammate's already-applied migration — add a new migration on top.
- If a conflict touches logic neither of you fully understands (e.g., the other's in-progress module), resolve it together rather than guessing.

---

## Dependency Map

```
Module 0: Project Foundation
        ↓
Module 1: Database Foundation
        ↓
Module 2: Authentication & Authorization
        ↓
Module 3: User & Profile Management
        ↓
Module 4: Lost Item Reporting  ──┐
                                  ├── (parallel, shared ItemCategories schema)
Module 5: Found Item Reporting ──┘
        ↓
Module 6: Image & File Management
        ↓
Module 7: Search & Location
        ↓
Module 8: Basic Matching Engine
        ↓
Module 9: AI Matching Engine (enhances 8; non-blocking for MVP)
        ↓
Module 10: Match Management
        ↓
Module 11: Ownership Claims
        ↓
Module 12: Secure Communication
        ↓
Module 13: Organization Management ──┐
                                       ├── feeds into
Module 14: Organization Portal ◄──────┘  (also needs Module 5, 11)
        ↓
Module 15: Item Lifecycle & Recovery  (ties together 4, 5, 10, 11, 14)
        ↓
Module 16: Notifications
        ↓
Module 17: Fraud Prevention & Trust
        ↓
Module 18: Admin Dashboard  ──┐
                                ├── both feed
Module 19: Analytics ─────────┘
        ↓
Module 20: Security Hardening
        ↓
Module 21: Testing (continuous, formalized here)
        ↓
Module 22: Docker & Local Development (fast-tracked after Module 1 in practice)
        ↓
Module 23: AWS Deployment
        ↓
Module 24: CI/CD (basic pipeline fast-tracked after Module 0 in practice)
        ↓
Module 25: Final Integration & Production Readiness
```

**Fast-track exceptions** (lower risk to pull forward, noted above in each module):
- Module 22's basic `docker-compose.yml` (Postgres only) → right after Module 1.
- Module 24's basic build/test GitHub Actions workflow → right after Module 0.

Everything else should be built in the order shown, because each module's
**Dependencies** section reflects real data/API contracts the next module
consumes — starting early risks rework once the dependency's shape changes.
