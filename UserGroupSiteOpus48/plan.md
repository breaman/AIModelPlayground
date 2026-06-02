# Implementation Plan — Technical User Group Site

This plan implements a website for managing technical user group meetings on top of the
existing **breaman.blazor** template. It is broken into ordered phases. Each phase lists the
work, the files involved, and acceptance criteria. Phases are intended to be executed in order;
later phases assume earlier ones are complete.

## Template Conventions (read before starting any phase)

These are hard constraints derived from the existing codebase and `.claude/rules/*`. Follow them
exactly — they override any general habits.

- **Stack:** .NET 10, C# 14, Blazor Web App with Aspire orchestration, EF Core + SQL Server,
  ASP.NET Identity, Bootstrap 5 + Bootstrap Icons, Serilog.
- **Render mode:** Use `@rendermode InteractiveWebAssembly` **only**. Never use
  `InteractiveServer`/`InteractiveServerRenderMode`. Any interactive component must live in the
  **Client** project (`src/UserGroupSiteOpus48.Client`).
- **Pre-render / data fetch pattern (dual-mode services):** For any interactive page that needs
  data, define the service **interface in the Shared project**, a **Client** implementation that
  calls an HTTP API (`HttpClient`), and a **Server** implementation that hits the data layer
  directly. Use `[PersistentState]` on the data properties and `??=` in `OnInitializedAsync` (see
  `.claude/rules/blazor.instructions.md` "InteractiveWebAssembly Pre-Rendering Pattern").
- **Server APIs:** Because the client runs in WASM, every data operation it performs needs a
  server HTTP endpoint (minimal APIs under e.g. `/api/...`). Secure them with authorization.
- **Code-behind only:** No `@code { }` blocks in `.razor` files. Use `*.razor.cs` partial classes
  or service classes.
- **Entities:** Live in `src/UserGroupSiteOpus48.Data/Models`. Extend `FingerPrintEntityBase`
  (gives `Id`, `CreatedOn/By`, `ModifiedOn/By` — populated automatically) for auditable entities,
  or `EntityBase` for simple ones. The `AuthDbContext` already auto-applies fingerprinting and
  full audit logging on `SaveChanges`. Register every entity as a `DbSet` on `ApplicationDbContext`.
- **Identity:** `User : IdentityUser<int>` and `Role : IdentityRole<int>` (int keys). Roles are
  enabled (`AddRoles<Role>()`). `RequireConfirmedAccount = true`. `FirstName` is already exposed as
  a claim via `CustomUserClaimsPrincipalFactory`. `IUserService.UserId` gives the current user id.
- **Forms / UI:** Wrap every input in a Bootstrap `form-floating` div with a `<label>` and
  `<ValidationMessage>`. Use Bootstrap Icons (`bi bi-*`). Component-specific CSS goes in a sibling
  `*.razor.css`. SCSS lives in `src/UserGroupSiteOpus48.Server/styles/site.scss` and is compiled
  via `npm run sass-dev` / `sass-prod`.
- **Notifications:** Use the existing `IToastService` for user feedback.
- **Validation:** DataAnnotations or FluentValidation in forms; server endpoints re-validate.
- **Migrations:** `cd src/UserGroupSiteOpus48.Server && dotnet ef migrations add <Name> -p ../UserGroupSiteOpus48.Data`.
  Migrations are applied automatically when Aspire starts (`RunDatabaseUpdateOnStart`).
- **Run:** `cd aspire/UserGroupSiteOpus48.AppHost && dotnet watch`.

> **Already satisfied by the template:** The header already shows `Welcome, @FirstName` with a
> dropdown containing **Manage Account** and a **Log out** form (`NavMenu.razor`). The home route,
> auth (register/login/logout), and audit logging exist. Do **not** rebuild these — extend them.

---

## Phase 0 — Dependencies & Domain Roles Constants

**Goal:** Add the one external dependency and shared constants the rest of the work needs.

1. Add **Markdig** (Markdown → HTML) to `Directory.Packages.props` as a `<PackageVersion>` and
   reference it from the project(s) that render Markdown (Server for API-side rendering and Client
   for the live preview tab — render Markdown in a shared helper so both can use it).
2. Create a `RoleNames` static class in the **Shared** project with `const string Admin = "Admin"`
   and `const string Speaker = "Speaker"` so role strings are never hard-coded.
3. Create an authorization policy constants class if helpful (e.g. `Policies.AdminOnly`).

**Acceptance:** Solution restores and builds (`dotnet build`).

---

## Phase 1 — Domain Model & EF Core

**Goal:** Model events, speakers, topic suggestions, votes, and volunteers.

1. **Entities** in `src/UserGroupSiteOpus48.Data/Models` (all extend `FingerPrintEntityBase` unless
   noted):
   - `Event`: `Title` (required, MaxLength), `Slug` (required, unique, MaxLength),
     `ShortDescription`, `Description` (Markdown, long text), `EventDateTime` (`DateTime?`),
     `Location` (`string?`), `IsPublished` (`bool`). Navigation: `ICollection<EventSpeaker> Speakers`.
   - `EventSpeaker` (join table Event ↔ User): `EventId`, `UserId`, navs `Event`, `User`.
     Composite unique index on (`EventId`, `UserId`).
   - `TopicSuggestion`: `Title` (required), `Description` (`string?`, optional Markdown),
     `VolunteerUserId` (`int?`, nullable — the single volunteer speaker), nav `Volunteer`,
     `SuggestedByUserId`, nav `SuggestedBy`. Navigation: `ICollection<TopicVote> Votes`.
   - `TopicVote` (join TopicSuggestion ↔ User): `TopicSuggestionId`, `UserId`. **Unique index on
     (`TopicSuggestionId`, `UserId`)** to enforce "one vote per user per topic".
2. **DbSets:** Add `Events`, `EventSpeakers`, `TopicSuggestions`, `TopicVotes` to
   `ApplicationDbContext` (or a partial). Configure relationships, the unique indexes (Slug,
   EventSpeaker pair, TopicVote pair), and `OnDelete` behaviors in `OnModelCreating`
   (override in `ApplicationDbContext`, calling `base.OnModelCreating` first since Identity needs it).
3. **Migration:** `dotnet ef migrations add AddDomainModel -p ../UserGroupSiteOpus48.Data`.

**Acceptance:** Migration generates; app starts via Aspire and applies it cleanly; tables exist.

---

## Phase 2 — Role & Admin Seeding

**Goal:** Ensure `Admin` and `Speaker` roles exist and there is a bootstrap admin.

1. Add a hosted seeding routine (e.g. `DbSeeder` invoked at server startup, after migrations) that:
   - Creates the `Admin` and `Speaker` roles via `RoleManager<Role>` if missing.
   - Creates/promotes a configurable bootstrap admin user (email/password from configuration /
     user-secrets) so there is always at least one admin. Confirm the account
     (`EmailConfirmed = true`) so login works given `RequireConfirmedAccount = true`.
2. Wire it into `Program.cs` guarded by the `isMigrations` check (don't run during `ef.dll`).

**Acceptance:** Fresh DB yields both roles and a working admin login.

---

## Phase 3 — Shared Contracts (DTOs & Service Interfaces)

**Goal:** Define the cross-project surface so Client and Server implement the same contracts.

1. In **Shared**, add DTOs (records) for transport: `EventDto`, `EventEditDto`, `EventListItemDto`,
   `SpeakerDto`, `TopicSuggestionDto`, `UserAdminDto`, etc. Keep them free of EF types.
2. In **Shared**, add service interfaces:
   - `IEventService` — list published events (desc by date), get by slug/id, create, update,
     list assignable speakers.
   - `ITopicService` — list suggestions (with vote counts + whether current user voted + volunteer),
     suggest, vote, volunteer.
   - `IUserAdminService` — list users, set/unset Admin & Speaker roles.
3. Add a shared `MarkdownRenderer` helper (wrapping Markdig) usable by both Client (preview) and
   Server (stored render), with safe/sanitized output.

**Acceptance:** Shared project builds; interfaces compile and are referenced nowhere yet.

---

## Phase 4 — Server Services, API Endpoints & Authorization

**Goal:** Implement server-side data services and the HTTP API the WASM client calls.

1. **Server implementations** of the Phase 3 interfaces (e.g. `ServerEventService`,
   `ServerTopicService`, `ServerUserAdminService`) using `ApplicationDbContext` directly.
   Encapsulate business rules here (see Phase rules below).
2. **Minimal API endpoints** (e.g. `EventEndpoints`, `TopicEndpoints`, `UserAdminEndpoints` mapped
   in `Program.cs` under `/api`). Public read endpoints for published events; everything else
   requires authentication; admin/user-management endpoints require the `Admin` policy.
3. **Authorization policies:** Register `AdminOnly`. For event editing, enforce "editor = any Admin
   **or** an assigned speaker of that event" inside the service/endpoint (resource-based check),
   since it depends on the specific event.
4. **Business rules to enforce server-side (authoritative):**
   - Event: `Title` and `Slug` required to save. `Slug` unique.
   - Event publish guard: if `IsPublished == true`, require `Description`, `EventDateTime`,
     `Location`, and **≥1 speaker**; otherwise reject with a clear validation error.
   - Topic vote: reject a second vote by the same user for the same topic (rely on unique index +
     friendly error).
   - Volunteer: only allow if the topic has no existing volunteer.
   - User admin: an admin **cannot remove their own Admin role** (reject self-demotion).
5. Register all server services in `Server/Program.cs`.

**Acceptance:** Endpoints reachable; rules verified via quick manual calls or tests; unauthorized
access blocked.

---

## Phase 5 — Client Services (HTTP)

**Goal:** Let WASM components consume the API through the shared interfaces.

1. **Client implementations** (`ClientEventService`, `ClientTopicService`, `ClientUserAdminService`)
   that call the `/api` endpoints via injected `HttpClient`.
2. Register them in `Client/Program.cs`. Register the Server implementations in `Server/Program.cs`
   (so pre-render uses the direct/DB path and hydration uses HTTP).

**Acceptance:** A trivial interactive page can fetch data both during pre-render and after hydration.

---

## Phase 6 — Admin: User Management UI

**Goal:** Admins manage who is an Admin and/or Speaker.

1. Interactive page in **Client** (e.g. `Pages/Admin/Users.razor` + code-behind), route guarded by
   the `Admin` policy (`[Authorize(Policy = ...)]` / `AuthorizeView`).
2. List users with FirstName/LastName/email and toggles/checkboxes for **Admin** and **Speaker**
   roles, saving via `IUserAdminService`.
3. Enforce in UI **and** server: the current admin cannot uncheck their own Admin role (disable the
   control with an explanatory tooltip and reject server-side).
4. Use `IToastService` for success/error feedback.

**Acceptance:** Admin can grant/revoke roles; self-demotion is blocked; non-admins can't reach the page.

---

## Phase 7 — Event Create/Edit UI

**Goal:** Editors create and edit events with the specified fields and UX.

1. Interactive page in **Client** (e.g. `Pages/Events/EditEvent.razor`), accessible to **editors**
   (any Admin, or a speaker assigned to that event for edit; Admins for create). Verify on server.
2. Form (each input in a `form-floating` wrapper):
   - **Title** (required).
   - **Slug** (required) — on the Title field's `onblur`, if Slug is empty, populate it with the
     **kebab-case** of the Title. Implement kebab-case in C# (lowercase, spaces/punctuation →
     single hyphens, trim). Slug remains editable.
   - **Short Description**.
   - **Description** — Markdown field with **Edit** and **Preview** tabs (Bootstrap nav-tabs). The
     Preview tab renders the current text via the shared `MarkdownRenderer` live, without saving.
   - **Date/Time** (`InputDate`/datetime), **Location**.
   - **Speakers** — multi-select of users in the `Speaker` role (one or more).
   - **Published** — checkbox/switch.
3. **Client-side validation mirrors server rules:** Title+Slug always required; when Published is
   on, require Description, Date/Time, Location, and ≥1 speaker. Show messages; block save.
4. Save via `IEventService`; toast on success; redirect to the event or list.

**Acceptance:** Slug auto-fills on blur; Markdown preview works; publish validation enforced both
client and server; assigned speaker can edit their event.

---

## Phase 8 — Public Home Page & Event Detail

**Goal:** Anyone can browse published events.

1. Replace the placeholder `Home.razor` content with a list of **published events in descending
   order** (by event date/time). Visible to anonymous and authenticated users. Use the dual-mode
   `IEventService` with `[PersistentState]` so the list pre-renders.
2. Each item links to an **event detail** page (public, by slug) showing the rendered Markdown
   Description, date/time, location, and speakers.
3. Provide an "Events" admin/editor entry point (list incl. unpublished for editors) — published-only
   for the public.

**Acceptance:** Logged-out visitor sees only published events, newest first; detail page renders
Markdown and speaker info.

---

## Phase 9 — Topic Suggestions UI

**Goal:** Logged-in users suggest topics, vote, and volunteer.

1. Interactive page in **Client** (e.g. `Pages/Topics/Topics.razor`), requires authentication.
2. **Suggest:** form to add a topic (Title required, optional Description).
3. **Vote:** any logged-in user can upvote a topic; enforce one vote per user per topic (button
   reflects voted state; server rejects duplicates). Show vote counts.
4. **Volunteer:** any logged-in user can volunteer for a topic that has **no** volunteer yet; once
   volunteered, show the volunteer and hide the action for others.
5. Use `ITopicService` and `IToastService`.

**Acceptance:** Voting is idempotent per user; volunteering is single-occupancy; all actions require
login.

---

## Phase 10 — Navigation & Role-Aware Menu

**Goal:** Surface the new areas appropriately.

1. Extend `NavMenu.razor` with links to **Events**, **Topics**, and (Admin-only) **Users** /
   **Manage Events**, using `AuthorizeView` with role checks so links appear only when relevant.
   Keep the existing Welcome/Logout dropdown intact.

**Acceptance:** Menu items show/hide based on auth state and roles.

---

## Phase 11 — Validation, Error Handling & Polish

1. Centralize validation (FluentValidation or DataAnnotations) shared where possible; ensure server
   endpoints always re-validate (never trust the client).
2. Friendly error surfaces: `ErrorBoundary` where helpful, problem-details responses from APIs,
   toasts for user-facing failures, Serilog for server logging.
3. Accessibility & consistency: labels, `form-floating`, consistent Bootstrap Icons, lowercase URLs
   (route option already enabled).
4. Compile SCSS (`npm run sass-prod`) and verify styling.

**Acceptance:** Bad input is handled gracefully end-to-end; no unstyled/blocking states.

---

## Phase 12 — Tests & Verification

1. **Unit tests** for business rules: slug kebab-casing, publish validation, one-vote-per-user,
   single-volunteer, admin self-demotion block. Mock dependencies per `.claude/rules` (xUnit + Moq/
   NSubstitute). Follow existing naming style; no Arrange/Act/Assert comments.
2. **Component/E2E** smoke tests using the project's Playwright skill for: home list ordering,
   register→login, create+publish event (with validation), suggest/vote/volunteer, admin role
   management.
3. **Full verification:** `dotnet build`; run via Aspire (`dotnet watch` in AppHost); confirm
   migrations apply, seeding runs, and each user story works.

**Acceptance:** Build is clean; tests pass; all requirements demonstrably work in the running app.

---

## Requirements Traceability

| Requirement | Phase |
|---|---|
| Self-registration | Template (existing) |
| Admin manages Admin/Speaker roles | 2, 4, 6 |
| Admin cannot remove own Admin role | 4, 6 |
| Event fields (title, slug, short desc, markdown desc, datetime, location, speakers, published) | 1, 7 |
| Slug auto kebab-case on title blur | 7 |
| Markdown edit/preview tabs | 0, 3, 7 |
| Title+Slug required to save | 4, 7 |
| Publish requires desc/datetime/location/≥1 speaker | 4, 7 |
| Editor = admin or assigned speaker | 4, 7 |
| Suggest topics | 9 |
| Vote once per topic per user | 1, 4, 9 |
| Volunteer for un-volunteered topics | 1, 4, 9 |
| Home lists published events desc, public | 8 |
| Welcome, <firstName> + logout dropdown | Template (existing), 10 |
