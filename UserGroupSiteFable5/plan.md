# User Group Meeting Site — Implementation Plan

A website to manage technical user group meetings: self-registering users, admin-managed
roles (Admin/Speaker), admin-created events with Markdown descriptions and publish rules,
and community topic suggestions with voting and speaker volunteering.

## Existing Foundation (do not rebuild)

- **.NET 10 Blazor Web App** — WASM interactive render mode. Interactive pages live in
  `src/UserGroupSiteFable5.Client`; static SSR pages (Identity account UI, Home) live in
  `src/UserGroupSiteFable5.Server/Components`.
- **ASP.NET Identity** with `int` keys, custom `User` (`FirstName`, `LastName`, `MemberSince`)
  and `Role` in `src/UserGroupSiteFable5.Data`. Self-registration, login/logout endpoints, and
  account pages are already scaffolded under `Server/Components/Account`.
- **EF Core + SQL Server** via Aspire (`aspire/UserGroupSiteFable5.AppHost`), with audit
  fingerprinting in `AuthDbContext`. No migrations exist yet.
- **Shared** project for cross-boundary contracts (currently `IToastService`); `ToastService`
  in Client for notifications.
- Bootstrap SCSS pipeline (`npm run sass-dev` in Server project), Serilog, lowercase URLs.

**Architecture rule for new work:** interactive UI goes in the Client (WASM) project and talks
to minimal API endpoints in the Server project; DTOs and validation contracts go in Shared;
entities and EF configuration go in Data. Public, read-only pages (Home) stay static SSR in
the Server project.

---

## Phase 0 — Project bootstrap & first migration

1. `dotnet tool restore` (EF tool is a local dotnet tool).
2. `npm install` then `npm run sass-dev` (or `sass-prod`) in `src/UserGroupSiteFable5.Server`
   to generate the Bootstrap CSS — required before first build/run.
3. Verify the solution builds: `dotnet build UserGroupSiteFable5.slnx`.

> Hold the initial migration until Phase 1 entities exist so `InitialDatabase` includes the
> full schema in one migration. Migrations apply automatically when Aspire starts the app.

## Phase 1 — Domain model & database

New entities in `src/UserGroupSiteFable5.Data/Models` (inherit `FingerPrintEntityBase` for
auditing where appropriate):

1. **Event**
   - `Title` (required, max length), `Slug` (required, unique index, max length)
   - `ShortDescription`, `Description` (Markdown source, `nvarchar(max)`)
   - `StartsAt` (date/time of event), `Location`
   - `IsPublished` (bool)
   - `Speakers` — many-to-many to `User` via explicit join entity `EventSpeaker`
     (`EventId`, `UserId`, composite key) so speaker assignment is queryable for authorization.
2. **TopicSuggestion**
   - `Title`, `Description`, `SuggestedByUserId` → `User`
   - `VolunteerUserId` (nullable) → `User` — at most one volunteer per topic
   - `Votes` — collection of `TopicVote`
3. **TopicVote**
   - `TopicSuggestionId` + `UserId` composite primary key (enforces one vote per user per topic
     at the database level)
4. Register `DbSet`s and entity configuration (unique slug index, max lengths, delete
   behaviors — restrict user deletes from votes/speakers) on `ApplicationDbContext`.
5. **Role seeding**: seed `Admin` and `Speaker` roles (and a dev-only initial admin user) —
   either via `HasData` in model config or a startup seeding step alongside migration.
6. Create the migration:
   `cd src/UserGroupSiteFable5.Server && dotnet ef migrations add InitialDatabase -p ../UserGroupSiteFable5.Data`
7. Run via Aspire (`cd aspire/UserGroupSiteFable5.AppHost && dotnet watch`) and confirm the
   schema applies and seeded roles exist.

## Phase 2 — Shared contracts & server API surface

1. DTOs in `src/UserGroupSiteFable5.Shared` (with DataAnnotations so the same validation runs
   in WASM forms and server endpoints):
   - `EventSummaryDto`, `EventDetailDto`, `EventEditDto` (includes speaker user IDs)
   - `UserAdminDto` (id, name, email, isAdmin, isSpeaker), `SpeakerDto` (id, display name)
   - `TopicSuggestionDto` (incl. vote count, whether current user voted, volunteer name)
2. Minimal API endpoint groups in `src/UserGroupSiteFable5.Server` (e.g. `Endpoints/` folder),
   all under `/api/...`, mapped in `Program.cs`:
   - `GET /api/events/published` — anonymous
   - `GET /api/events`, `GET /api/events/{id}`, `POST /api/events`, `PUT /api/events/{id}` —
     authorization per Phase 4 rules
   - `GET /api/users`, `PUT /api/users/{id}/roles` — Admin only
   - `GET /api/speakers` — users in Speaker role (for the event speaker picker)
   - `GET/POST /api/topics`, `POST /api/topics/{id}/vote`, `POST /api/topics/{id}/volunteer` —
     any authenticated user
3. Authorization policies in `Program.cs`: `AdminOnly` (role Admin); event-editor checks are
   resource-based (Admin **or** assigned speaker of that event) — implement as a small service
   used inside the event endpoints rather than a static policy.
4. Register a typed/named `HttpClient` in the Client `Program.cs` (base address = host) for
   calling these APIs from WASM, plus thin client services (`EventApiClient`, `UserApiClient`,
   `TopicApiClient`) registered in DI.

## Phase 3 — Header / layout (logged-in experience)

1. Update `Server/Components/Layout/MainLayout.razor` + `NavMenu.razor`:
   - `<AuthorizeView>`: when authenticated show a **"Welcome, \<FirstName\>"** Bootstrap
     dropdown in the header (fall back to email if `FirstName` is null — it's nullable).
   - Dropdown contains **Log out** (form POST to the existing Identity logout endpoint,
     pattern already used by the Identity scaffold) and a link to account management.
   - When anonymous: Login / Register links.
2. Nav links gated by role: "Manage Users" (Admin), "Events" admin list (Admin or Speaker),
   "Topic Suggestions" (any authenticated user).
3. The first-name claim: confirm `CustomUserClaimsPrincipalFactory` emits `FirstName` (add a
   claim if it doesn't) so the header doesn't need a DB hit.

## Phase 4 — Admin: user & role management

Page: `Client/Components/Pages/Admin/Users.razor` (`@attribute [Authorize(Roles = "Admin")]`,
interactive WASM) backed by the Admin API.

1. Table of all users (name, email, member since) with checkboxes/toggles for **Admin** and
   **Speaker** roles; a user may hold both.
2. **Self-lockout guard**: the row for the currently logged-in admin renders its Admin toggle
   disabled, and the server endpoint independently rejects any request where the acting admin
   removes their own Admin role (server-side check is the source of truth).
3. Toast feedback on save success/failure via existing `IToastService`.

## Phase 5 — Event management (create/edit)

Pages: `Client/Components/Pages/Events/EventList.razor` (editor's list) and
`EventEdit.razor` (`/events/new`, `/events/{id:int}/edit`), interactive WASM.

1. **Access**: page requires authentication; the list shows all events for Admins and only
   assigned events for Speakers; the server enforces per-event editor rules (Admin or assigned
   speaker) on read-for-edit and update. Only Admins can create events and modify the speaker
   list.
2. **Form fields**: Title, Slug, Short Description, Description (Markdown), Date/Time
   (`InputDate` with `InputDateType.DateTimeLocal`), Location, multi-select speaker picker
   (from `GET /api/speakers`), Published checkbox.
3. **Slug auto-fill**: on the Title input's `onfocusout`/`onblur`, if Slug is empty, populate
   it with the kebab-cased title (lowercase, alphanumerics, hyphens; implemented as a shared
   `SlugHelper` in Shared so the server can reuse it for validation/normalization).
4. **Markdown editor component** (`Client/Components/MarkdownEditor.razor`, reusable):
   - Bootstrap tabs: **Edit** (textarea bound to the field) and **Preview** (renders the
     current text without saving).
   - Add **Markdig** to `Directory.Packages.props` and reference it from Client (preview,
     runs in WASM) and Server (rendering on public pages). Sanitize/disable raw HTML
     (`DisableHtml`) to prevent XSS.
5. **Validation** (two tiers, mirrored client + server):
   - Always: Title and Slug required (slug also unique — server check with friendly error).
   - When `IsPublished` is checked: Description, Date/Time, Location, and ≥ 1 speaker are
     required. Implement with `IValidatableObject` on `EventEditDto` so both the WASM
     `EditForm` and the server endpoint enforce it identically.
6. Save → toast + redirect to event list.

## Phase 6 — Topic suggestions

Page: `Client/Components/Pages/Topics/Topics.razor` (`@attribute [Authorize]`, interactive WASM).

1. List all suggestions: title, description, suggester, vote count, volunteer (if any).
2. **Suggest**: simple form (title + description) any authenticated user can submit.
3. **Vote**: vote button per topic; disabled/“voted” state when the current user already voted
   (composite PK makes double-votes impossible server-side; surface a clean error regardless).
4. **Volunteer**: "Volunteer to speak" button shown only when `VolunteerUserId` is null; the
   server endpoint guards against races (reject if a volunteer already exists). Show the
   volunteer's name once claimed.
5. Sort by vote count descending so popular topics float to the top.

## Phase 7 — Public home page

Update `Server/Components/Pages/Home.razor` (+ code-behind), static SSR, anonymous:

1. Query published events ordered by `StartsAt` **descending**, injected `ApplicationDbContext`
   directly (server-rendered, no API hop needed).
2. Card/list per event: Title, date/time, location, short description, speaker names, and the
   Markdown description rendered via Markdig (or link to a `/events/{slug}` detail page that
   renders it — detail page is a stretch item, same SSR pattern).
3. Verify the page renders for an anonymous visitor.

## Phase 8 — Polish & verification

1. **Tests** (new `tests/UserGroupSiteFable5.Tests` project): unit tests for `SlugHelper`,
   `EventEditDto` publish validation, and the role-management self-lockout guard; endpoint
   tests for editor authorization (admin vs. assigned speaker vs. other speaker) and
   single-vote/single-volunteer enforcement.
2. End-to-end pass via Aspire: register a user → promote to Admin (seeded admin) → create a
   draft event → assign a speaker → publish (watch validation kick in) → verify home page →
   suggest/vote/volunteer on topics → log out from the header dropdown.
3. Run `npm run sass-prod`, fix any `.editorconfig`/analyzer warnings, update `README.md`
   with feature overview.

---

## Build order & dependencies

```
Phase 0 ─→ Phase 1 ─→ Phase 2 ─→ Phase 3 (independent of 4–6 once 2 is done)
                          ├─→ Phase 4 (users/roles)
                          ├─→ Phase 5 (events; needs Speaker role from 4's seeding only)
                          └─→ Phase 6 (topics)
                                Phase 5 ─→ Phase 7 (home needs published events)
                                All ─→ Phase 8
```

## Key decisions / notes

- **WASM + API split**: the app has no interactive-server render mode, so all interactive
  pages must live in the Client project and use HTTP APIs — keep business rules in server
  endpoints; client-side checks are UX only.
- **Markdig** is the only new NuGet dependency (added centrally in `Directory.Packages.props`).
- **Authorization is resource-based for events** (editor = Admin ∪ assigned speakers), so a
  plain role policy is insufficient — a small `IEventAuthorizationService` keeps it testable.
- **Razor quirk** (from prior projects): when placing components inside `@if`/`@foreach`
  blocks, wrap them in an HTML element.
