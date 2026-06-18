# Plan — Technical User Group Meeting Site (GLM-5.2)

> Branch: `12-glm-52` · Model: `glm-5.2` · Target: `net10.0` · Created: 2026-06-17
> Solution root: `UserGroupSiteGlm52/` (Aspire + Blazor Web App + EF Core + Identity)

This document is the multi-step implementation plan for a website that manages
technical user group meetings: user self-registration with admin-managed roles,
event authoring with a Markdown editor, and community topic suggestions with
voting and volunteering.

---

## 1. Project Context & Existing Scaffolding

The repo is pre-scaffolded from an Aspire + Blazor + Identity template. The
plan builds **on top of** what already exists — it does not re-scaffold.

**Solution projects**

| Project | SDK | References | Role |
|---|---|---|---|
| `src/UserGroupSiteGlm52.Server` | `Microsoft.NET.Sdk.Web` | `Client`, `Data`, `ServiceDefaults` | Host app, static SSR pages, Identity UI, Minimal API endpoints, server-side service impls |
| `src/UserGroupSiteGlm52.Client` | `Microsoft.NET.Sdk.BlazorWebAssembly` | `Shared` | WebAssembly-interactive components + HTTP service impls |
| `src/UserGroupSiteGlm52.Data` | `Microsoft.NET.Sdk` | `Shared` | EF Core entities, `ApplicationDbContext`, audit |
| `src/UserGroupSiteGlm52.Shared` | `Microsoft.NET.Sdk` | (none) | DTOs + service interfaces (the contract layer) |
| `aspire/UserGroupSiteGlm52.AppHost` | AppHost | — | Orchestrates SQL Server container + Server project; auto-runs EF migrations on start |
| `aspire/UserGroupSiteGlm52.ServiceDefaults` | — | — | OTel/health checks/service discovery; `Constants.DatabaseConnectionString = "usergroupsiteglm52db"` |

**Already implemented (do not rebuild):**
- Full ASP.NET Core Identity UI under `Server/Components/Account/` (register,
  login, confirm email, forgot/reset password, 2FA, passkeys, manage profile).
  Self-registration works out of the box.
- `AuthDbContext` (abstract) — `IdentityDbContext<User, Role, int>` with the
  `AuditLogs` DbSet and automatic **fingerprinting** (`CreatedBy/On`,
  `ModifiedBy/On` on `FingerPrintEntityBase`) and **audit logging** of every
  add/update/delete via `SaveChangesAsync`. The current user id is injected
  through `IUserService` (`HttpUserService` reads the NameIdentifier claim).
- `ApplicationDbContext : AuthDbContext` — currently **empty** (no app DbSets).
  This is where event/topic DbSets are added.
- Roles are enabled: `Role : IdentityRole<int>`, `AddRoles<Role>()`.
- `CustomUserClaimsPrincipalFactory` adds a `FirstName` claim.
- `NavMenu.razor` already renders **"Welcome, {FirstName}"** in a dropdown with
  **Manage Account** and **Log out** (form POST to `/Account/Logout`) for
  authenticated users, and **Register / Log in** for anonymous users. → The
  header/logout requirement is already satisfied; we only add nav links.
- Toast service (`IToastService` in `Shared`, `ToastService` in `Client`) wired
  into a `ToastContainer` (`@rendermode InteractiveWebAssembly`) in `MainLayout`.
- Serilog (console + MSSqlServer `Logs` sink), OTel, health checks.
- `Home.razor` is a static SSR placeholder (`@page "/"`).
- `dotnet-ef` tool is declared in `.config/dotnet-tools.json`.

**Not yet done (this plan):**
- No EF migration exists. `InitialDatabase` must be created (Aspire runs it on
  start, so the app cannot create tables until a migration file is present).
- No app entities, DTOs, service contracts, API endpoints, or feature pages.
- No role seed / initial admin user (needed so someone can manage roles).

---

## 2. Architecture & Conventions to Follow

From `.claude/rules/csharp.instructions.md` and `blazor.instructions.md`:

- **Render mode policy (hard rule):** never use `InteractiveServer`. Every
  interactive component uses `@rendermode InteractiveWebAssembly` and **lives in
  the `Client` project**. Public, read-only pages stay static SSR in `Server`.
- **Dual-mode service pattern** for every WebAssembly page that shows data:
  1. Define the service interface in `Shared`.
  2. Server impl (DB-backed) registered in `Server/Program.cs`.
  3. Client impl (HTTP to Minimal API) registered in `Client/Program.cs`.
  4. Code-behind holds pre-rendered data on `[PersistentState]` properties and
     fetches with `??=` in `OnInitializedAsync` so it only re-fetches on
     hydration when state was not restored.
- **No `@code` blocks** in `.razor` — always code-behind (`.razor.cs`) or a
  service.
- **Forms:** every input wrapped in a Bootstrap `form-floating` div; use
  `InputText`/`InputTextArea`/`InputSelect` + `ValidationMessage`.
- **Styling:** Bootstrap utility classes first; component-specific CSS in a
  `.razor.css` beside the component; global additions in
  `Server/styles/site.scss` (compiled via `npm run sass-dev|sass-prod`).
  **Icons:** bootstrap-icons, used consistently.
- **C# 14** features (records, pattern matching, file-scoped namespaces,
  `nameof`, switch expressions). `TreatWarningsAsErrors=True` — code must be
  warning-clean. Central package management → add every new NuGet package to
  `Directory.Packages.props` and reference it (no inline `Version`).
- **Auth:** cookie-based Identity (the scaffold uses `IdentityConstants`,
  **not** JWT). Follow the existing pattern; do not introduce JWT.
- **Naming:** PascalCase members, camelCase locals, `I`-prefix interfaces.
- **Blazor quirk (from memory):** inside `@if` / `@foreach`, wrap rendered
  components in an HTML element (e.g. a `<div>` or `<li>`), not bare components.
- **Build prerequisite:** run `npm install` in `Server/` before the first build
  (the `CopyBootstrapJs` MSBuild target copies `bootstrap.bundle.min.js` from
  `node_modules`).

---

## 3. Key Decisions & Assumptions

1. **Role names:** `Admin` and `Speaker` (created by the seeder). A user may hold
   either or both via standard `UserManager` role membership.
2. **Render-mode split:**
   - **Static SSR (`Server`):** Home (public event list), Event detail (public,
     Markdown rendered server-side), plus the existing Identity pages.
   - **WebAssembly (`Client`):** Event editor, Topic suggestions, Admin user
     management — anything with rich client interactivity.
3. **Home ordering:** published events ordered by `EventDate` **descending**
   (newest first).
4. **Event speakers come from the `Speaker` role.** The editor's speaker picker
   lists users in the `Speaker` role; assigning them creates `EventSpeaker`
   rows. An "assigned speaker" (a user on the event's `EventSpeaker` list) may
   edit that event.
5. **Create vs edit authorization:** creating an event is **Admin-only** (a
   speaker cannot be assigned before the event exists). Editing is **Admin or an
   assigned speaker**. Enforcement is server-side at the API; the page uses
   `[Authorize]` for UX gating only.
6. **Topic suggestions page requires login** (all three actions — suggest, vote,
   volunteer — require login per the spec, so the page is gated to authenticated
   users).
7. **Markdown:** `Markdig` renders Markdown → HTML on both server (public detail)
   and client (live preview). Server-side rendering is sanitized with
   `Ganss.HtmlSanitizer` before emitting `MarkupString` (public, untrusted
   content). The WASM live preview renders Markdig output directly (it is the
   editor's own typing; stored/served content is sanitized server-side) to keep
   the sanitizer out of the WASM payload.
8. **Slug:** required to save; the UI auto-fills it from the title (kebab-case)
   on `@onblur` when empty. Server requires non-empty; a unique index + server
   check prevents duplicate slugs (sensible default for URL routing).
9. **Seeding:** a startup seeder creates the `Admin`/`Speaker` roles and a
   default admin user (email/password from `Configuration`/user secrets, with a
   documented dev fallback). Without this, no one can manage roles.
10. **Audit:** automatic — `AuthDbContext` already logs every entity change to
    `AuditLogs`, so events/topics/users changes are audited for free.
11. **Confirmation caveat:** `RequireConfirmedAccount = true` + the no-op
    `IdentityNoOpEmailSender` means new registrants cannot log in until
    confirmed. The template's `RegisterConfirmation` page provides a dev
    "click here to confirm" link. This is a dev-only friction to be aware of; no
    code change required for the plan.

---

## 4. Data Model (`UserGroupSiteGlm52.Data/Models/`)

All new entities extend `FingerPrintEntityBase` (Id + CreatedBy/On +
ModifiedBy/On) so they participate in automatic fingerprinting and audit,
**except** the lightweight vote/volunteer tables which extend `EntityBase` and
carry their own timestamp + user columns.

```
Event : FingerPrintEntityBase
  Title            string   [Required] [MaxLength(200)]
  Slug             string   [Required] [MaxLength(200)]   // unique index
  ShortDescription string?  [MaxLength(300)]
  Description      string?                              // Markdown
  EventDate        DateTime?                           // required when published
  Location         string?  [MaxLength(200)]            // required when published
  IsPublished      bool     = false
  Speakers         ICollection<EventSpeaker>           // many-to-many via join

EventSpeaker : FingerPrintEntityBase   // join: Event ↔ User
  EventId  int   [FK]   Event  Event
  UserId   int   [FK]   User   User
  // unique index (EventId, UserId)

TopicSuggestion : FingerPrintEntityBase
  Title              string   [Required] [MaxLength(200)]
  Description        string?                       // Markdown (optional)
  SuggestedByUserId  int      [FK]   User SuggestedByUser
  Votes              ICollection<TopicVote>
  Volunteer          TopicVolunteer?               // 1:1 (at most one)

TopicVote : EntityBase                 // one vote per user per topic
  TopicSuggestionId  int  [FK]   TopicSuggestion Topic
  UserId             int  [FK]   User User
  VotedOn            DateTime
  // unique index (TopicSuggestionId, UserId)

TopicVolunteer : EntityBase            // at most one volunteer per topic
  TopicSuggestionId  int  [FK] [unique]   TopicSuggestion Topic
  UserId             int  [FK]            User User
  VolunteeredOn      DateTime
```

`ApplicationDbContext` gains:

```csharp
public DbSet<Event> Events => Set<Event>();
public DbSet<EventSpeaker> EventSpeakers => Set<EventSpeaker>();
public DbSet<TopicSuggestion> TopicSuggestions => Set<TopicSuggestion>();
public DbSet<TopicVote> TopicVotes => Set<TopicVote>();
public DbSet<TopicVolunteer> TopicVolunteers => Set<TopicVolunteer>();
```

`OnModelCreating` configures: relationships + cascades, `Event.Slug` unique
index, `EventSpeaker` composite unique index, `TopicVote` composite unique
index, `TopicVolunteer` unique index on `TopicSuggestionId`, `Topic`→`User` FKs.
(A DB-level unique index is the backstop for "one vote per user" and "one
volunteer per topic".)

---

## 5. DTOs & Service Contracts (`UserGroupSiteGlm52.Shared/`)

`Shared` holds everything the Client is allowed to see (no EF models, no
`User`/`Role` — those stay in `Data`). Add:

- `Shared/Models/` — records: `EventSummaryDto`, `EventDetailDto`, `EventEditDto`
  (incl. `IReadOnlyList<SpeakerOptionDto>` for the picker + selected ids),
  `SpeakerDto`/`SpeakerOptionDto`, `TopicDto` (with `VoteCount`,
  `HasCurrentUserVoted`, `VolunteerName?`, `HasCurrentUserVolunteered`),
  `TopicInputDto`, `UserWithRolesDto` (Id, Email, FirstName, LastName, IsAdmin,
  IsSpeaker, IsCurrentUser).
- `Shared/Services/IEventService.cs` —
  `GetPublishedEventsAsync()`, `GetPublishedBySlugAsync(slug)`,
  `GetEditableListAsync()` (admin/speaker), `GetForEditAsync(id)`,
  `CreateAsync(EventEditDto)`, `UpdateAsync(id, EventEditDto)`.
- `Shared/Services/ITopicService.cs` —
  `GetTopicsAsync()`, `SuggestAsync(TopicInputDto)`, `VoteAsync(id)`,
  `UnvoteAsync(id)`, `VolunteerAsync(id)`.
- `Shared/Services/IUserAdminService.cs` —
  `GetUsersAsync()`, `UpdateRolesAsync(id, isAdmin, isSpeaker)`.
- `Shared/Services/IMarkdownService.cs` — `ToHtml(markdown)` (Markdig). One
  implementation used by both Server (public detail) and Client (preview).
- `Shared/SlugHelper.cs` — `ToSlug(title)` → kebab-case (used by the editor's
  `@onblur` and as a server fallback).

DTOs carry DataAnnotations (`[Required]`, `[MaxLength]`) for client form
validation; cross-field/business rules are enforced server-side (see §9).

---

## 6. Step-by-Step Implementation Plan

### Phase 0 — Environment prerequisites
- [ ] `cd src/UserGroupSiteGlm52.Server && npm install` (enables the
      `CopyBootstrapJs` build target; required before first build).
- [ ] `dotnet tool restore` (installs `dotnet-ef`).
- [ ] Add packages to `Directory.Packages.props`: `Markdig`,
      `Ganss.HtmlSanitizer`. Reference `Markdig` from `Shared`; reference
      `Ganss.HtmlSanitizer` from `Server`.

### Phase 1 — Data model & migration (`Data`)
- [ ] Add entities `Event`, `EventSpeaker`, `TopicSuggestion`, `TopicVote`,
      `TopicVolunteer` per §4.
- [ ] Add DbSets + `OnModelCreating` configuration to `ApplicationDbContext`.
- [ ] Create the migration (no migration exists yet):
      `cd src/UserGroupSiteGlm52.Server && dotnet ef migrations add InitialDatabase -p ../UserGroupSiteGlm52.Data`
      → produces Identity tables + all app tables in one migration. Aspire will
      apply it on start.

### Phase 2 — Shared contracts (`Shared`)
- [ ] Add DTOs under `Shared/Models/`.
- [ ] Add `IEventService`, `ITopicService`, `IUserAdminService`,
      `IMarkdownService` under `Shared/Services/`.
- [ ] Add `MarkdownService` (Markdig impl of `IMarkdownService`) and
      `SlugHelper` under `Shared/Services/`.

### Phase 3 — Server services, seeding, API endpoints (`Server`)
- [ ] `Server/Services/ServerEventService.cs`, `ServerTopicService.cs`,
      `ServerUserAdminService.cs` — DB-backed impls of the `Shared` interfaces
      (registered in `Program.cs`).
- [ ] `Server/Services/DataSeeder.cs` — creates `Admin`/`Speaker` roles and a
      default admin from configuration; idempotent; invoked from `Program.cs`
      after migrations (Aspire runs migrations first, so seed on startup).
- [ ] Minimal API endpoints under a new `Server/Endpoints/` (or an extensions
      class) mapped in `Program.cs` — see §7. Each endpoint enforces auth:
      `RequireAuthorization()` / `RequireAuthorization("Admin")` + manual
      assigned-speaker checks for event edit. Define an `"Admin"` policy in
      `Program.cs` (`RequireRole("Admin")`).
- [ ] Register services + `HttpClient` is already configured via
      `AddServiceDefaults` (resilience + service discovery). Ensure
      `IHttpContextAccessor` is added (needed by `HttpUserService`).

### Phase 4 — Client services (`Client`)
- [ ] `Client/Services/ClientEventService.cs`, `ClientTopicService.cs`,
      `ClientUserAdminService.cs` — HTTP impls calling the Minimal API via the
      registered `HttpClient` (base address already set in `Client/Program.cs`).
- [ ] Register the three client impls + `IMarkdownService` (the `Shared`
      `MarkdownService`) in `Client/Program.cs`.

### Phase 5 — Public UI (static SSR, `Server`)
- [ ] Replace `Home.razor`/`Home.razor.cs` with a list of published events
      ordered by `EventDate` desc. Use a server-side `IEventService` call (no
      WebAssembly needed — public + SEO-friendly). Wrap each event card in an
      HTML element inside `@foreach` (Blazor quirk). Link each to
      `/events/{slug}`.
- [ ] Add `Server/Components/Pages/EventDetail.razor` (+`.cs`) at
      `@page "/events/{Slug}"` — public, published events only; renders
      `Description` via `IMarkdownService.ToHtml` → sanitize with
      `Ganss.HtmlSanitizer` → `MarkupString`. Shows title, short/description,
      date/time, location, speakers. 404/`NotFound` for missing/unpublished
      (anonymous) ; unpublished visible to Admin/assigned speaker.
- [ ] Add nav links to `NavMenu.razor`: **Events** (home, public),
      **Topics** (`/topics`, login-gated), **Manage Events** (`/admin/events`,
      Admin), **Users** (`/admin/users`, Admin). Keep the existing
      Welcome/Logout dropdown.

### Phase 6 — Interactive UI (WebAssembly, `Client`) — dual-mode pattern
- [ ] **`MarkdownEditor.razor`** (+`.cs`) — reusable component: Bootstrap
      `nav-tabs` with **Edit** (textarea `@bind` value) and **Preview** tabs;
      preview renders `IMarkdownService.ToHtml(value)` as `MarkupString`.
      `@rendermode InteractiveWebAssembly`. Used by the Event editor (and
      optionally the topic form).
- [ ] **Event editor** `Client/Components/Pages/EventEditor.razor` (+`.cs`) —
      `@page "/admin/events"` (create) and `@page "/admin/events/{id:int}"`
      (edit); `@rendermode InteractiveWebAssembly`; `[Authorize]` (create) /
      `[Authorize]` (edit — server enforces admin-or-speaker). Features:
      - Title input (`form-floating`); **Slug** input with `@onblur` → if empty,
        `Slug = SlugHelper.ToSlug(Title)`.
      - ShortDescription, EventDate, Location inputs.
      - `MarkdownEditor` bound to `Description`.
      - Speaker multi-select (`InputSelect`/checkbox list of `SpeakerOptionDto`).
      - `IsPublished` checkbox.
      - `[PersistentState]` for the event + speaker options; `??=` fetch in
        `OnInitializedAsync`.
      - Client validation (DataAnnotations) + server validation results
        surfaced (toast + validation messages). On success → navigate to the
        event detail / manage list.
- [ ] **Topic suggestions** `Client/Components/Pages/Topics.razor` (+`.cs`) —
      `@page "/topics"`; `@rendermode InteractiveWebAssembly`; `[Authorize]`.
      - `[PersistentState]` list of `TopicDto`; `??=` fetch.
      - "Suggest a topic" form (title + optional Markdown description) → POST.
      - Each topic row: vote button (toggles; one vote per user), vote count,
        volunteer button (disabled if a volunteer already exists; shows the
        volunteer's name), "Suggested by {name}". Wrap rows in an element.
      - Toast feedback for each action; refresh list after mutations.
- [ ] **Admin user management** `Client/Components/Pages/AdminUsers.razor`
      (+`.cs`) — `@page "/admin/users"`; `@rendermode InteractiveWebAssembly`;
      `[Authorize(Roles = "Admin")]`.
      - `[PersistentState]` list of `UserWithRolesDto`; `??=` fetch.
      - Per-user Admin/Speaker toggles (checkboxes/switches). **Disable the
        Admin toggle for the current user** (UI guard) and the server rejects
        removing Admin from self (enforcement). Wrap rows in an element.
      - Toast feedback; optimistic or re-fetch after each change.

### Phase 7 — Polish & verification
- [ ] Confirm `TreatWarningsAsErrors` build is clean: `dotnet build` (after
      `npm install`).
- [ ] Run via Aspire: `cd aspire/UserGroupSiteGlm52.AppHost && dotnet watch`
      (auto-creates DB, applies `InitialDatabase`, runs seeder).
- [ ] Compile Bootstrap CSS if styles changed: `npm run sass-dev`.
- [ ] Walk the §11 verification checklist against every requirement.
- [ ] Update `README.md` if any run step changed.

---

## 7. API Endpoint Reference (Minimal API, `Server`)

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | `/api/events` | none | Published events, `EventDate` desc |
| GET | `/api/events/{slug}` | none | Published event detail (Admin/speaker may see unpublished) |
| GET | `/api/events/manage` | Admin | All events for the manage list |
| GET | `/api/events/{id}` | Admin **or assigned speaker** | Event for editing |
| POST | `/api/events` | Admin | Create (requires Title+Slug) |
| PUT | `/api/events/{id}` | Admin **or assigned speaker** | Update; if `IsPublished`, require Description+Date+Location+≥1 speaker |
| GET | `/api/topics` | logged-in | Topics with current-user vote/volunteer state |
| POST | `/api/topics` | logged-in | Suggest a topic |
| POST | `/api/topics/{id}/vote` | logged-in | Vote (one per user — DB unique index + server check) |
| DELETE | `/api/topics/{id}/vote` | logged-in | Remove own vote |
| POST | `/api/topics/{id}/volunteer` | logged-in | Volunteer (only if none exists) |
| GET | `/api/admin/users` | Admin | Users with role flags |
| PUT | `/api/admin/users/{id}/roles` | Admin | Set Admin/Speaker; **reject removing Admin from self** |

All endpoints return `ProblemDetails` (RFC 9457) on validation/authorization
failure so the client can surface errors consistently.

---

## 8. UI Page Inventory

| Page | Route | Project | Render mode | Auth |
|---|---|---|---|---|
| Home (published events) | `/` | Server | Static SSR | public |
| Event detail | `/events/{Slug}` | Server | Static SSR | public (published) |
| Register/Login/Manage | `/Account/...` | Server | Static SSR (existing) | mixed |
| Event editor | `/admin/events`, `/admin/events/{id}` | Client | `InteractiveWebAssembly` | Admin (create) / Admin or speaker (edit) |
| Topic suggestions | `/topics` | Client | `InteractiveWebAssembly` | logged-in |
| Admin users | `/admin/users` | Client | `InteractiveWebAssembly` | Admin |

---

## 9. Validation & Business Rules (enforced server-side)

- **Save:** Title and Slug required (DataAnnotations + endpoint check).
- **Publish:** setting `IsPublished = true` requires non-empty Description, a
  set `EventDate`, non-empty Location, and ≥1 `EventSpeaker`. Endpoint returns a
  validation problem otherwise.
- **Slug:** kebab-case auto-fill on the client (`@onblur`); server requires
  non-empty and rejects duplicates (unique index).
- **Vote:** at most one per user per topic (composite unique index + check).
- **Volunteer:** at most one per topic (unique index on `TopicSuggestionId` +
  check); the API refuses if a volunteer already exists.
- **Admin self-removal:** the user-management endpoint refuses to remove the
  `Admin` role from the calling user; the UI additionally disables that toggle.
- **Authorization:** the API is the security boundary — `[Authorize]` on pages
  is UX only. Event edit checks `IsInRole("Admin") || assignedSpeaker`.

---

## 10. Requirements Traceability

| Requirement | Where satisfied |
|---|---|
| Users self-register | Existing Identity `Register.razor` (no change) |
| Admin manages users → set Admin/Speaker/both | `AdminUsers.razor` + `PUT /api/admin/users/{id}/roles` |
| Admin cannot remove own Admin role while managing | UI disables toggle for self + server rejects |
| Admin create/edit event | `EventEditor.razor` + `/api/events` (create: Admin; edit: Admin or speaker) |
| Event fields: Title, Slug, ShortDescription, Description (Markdown), Date/Time, Location, Speakers, IsPublished | `Event` entity + editor form |
| Slug auto-fills kebab-case from title on blur | `@onblur` → `SlugHelper.ToSlug` in `EventEditor` |
| Description Markdown with edit + preview tabs | `MarkdownEditor.razor` (Edit/Preview nav-tabs) |
| Title + Slug required before save | DataAnnotations + endpoint check |
| Publish requires Description, Date/Time, Location, ≥1 speaker | Endpoint publish-guard (§9) |
| Editor = any Admin or an assigned speaker | Endpoint assigned-speaker check |
| Any logged-in user may suggest a topic | `Topics.razor` suggest form + `POST /api/topics` |
| Any logged-in user may vote once per topic | vote button + `POST /api/topics/{id}/vote` + unique index |
| Any logged-in user may volunteer on a topic with no volunteer | volunteer button + `POST /api/topics/{id}/volunteer` + unique index |
| Home lists published events, descending, visible to all | `Home.razor` static SSR, `EventDate` desc |
| Logged-in header "Welcome, {firstName}" | Existing `NavMenu.razor` (FirstName claim) |
| Log out as a dropdown item under Welcome | Existing `NavMenu.razor` dropdown (form POST `/Account/Logout`) |

---

## 11. Verification Checklist

- [ ] `npm install` + `dotnet build` succeeds with warnings-as-errors.
- [ ] `dotnet watch` (AppHost) starts; SQL container + Server come up;
      `InitialDatabase` applies; seeder creates roles + default admin.
- [ ] Register a user (confirm via the dev link); log in; header shows
      "Welcome, {FirstName}"; log out works from the dropdown.
- [ ] As the seeded admin: promote a user to Speaker; create an event (title →
      slug auto-fills on blur; Markdown preview tab renders); save as draft with
      only title+slug; attempt to publish without date/location/speaker →
      blocked; fill required fields + assign a Speaker → publish succeeds.
- [ ] Anonymous visitor sees the published event on Home (descending) and on
      `/events/{slug}` with rendered Markdown; unpublished events are not shown.
- [ ] Assigned speaker (non-admin) can edit their event; a non-speaker user
      cannot (server returns 403).
- [ ] Logged-in user can suggest a topic, vote once (second vote is rejected),
      and volunteer (a second user volunteering on an already-volunteered topic
      is blocked); the volunteer's name is shown.
- [ ] Admin cannot uncheck their own Admin role (disabled in UI + rejected by
      server); can manage other users' roles.
- [ ] Toast notifications appear for create/vote/volunteer/role-change success
      and errors.

---

## 12. Risks / Gotchas

- **No migration yet** — the app will not create tables until `InitialDatabase`
  is generated (Phase 1). Aspire applies it automatically once it exists.
- **`npm install` before build** — the `CopyBootstrapJs` target fails otherwise.
- **`@foreach`/`@if` with bare components** — wrap in an HTML element (memory
  quirk); relevant to Home, Topics, and AdminUsers lists.
- **`RequireConfirmedAccount` + no-op email sender** — new registrants must be
  confirmed; use the dev confirm link on `RegisterConfirmation` during testing.
- **Client has no `Data` reference** — never leak EF entities to the Client;
  everything crosses as `Shared` DTOs.
- **WASM payload** — keeping `Ganss.HtmlSanitizer` server-side only avoids
  shipping it to the browser; the live preview uses Markdig directly.
- **Authorization boundary** — Blazor `[Authorize]` is UX; the Minimal API is
  the real enforcement for event-edit, voting, volunteering, and role changes.
- **Markdown XSS** — always sanitize rendered Markdown on the public
  server-rendered detail page before emitting `MarkupString`.