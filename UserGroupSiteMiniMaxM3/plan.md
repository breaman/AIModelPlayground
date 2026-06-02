# User Group Site — Implementation Plan

## Project Context

- **Stack**: .NET 10, Blazor WebAssembly (Interactive) + Server (host), Aspire AppHost, EF Core 10 with SQL Server, ASP.NET Core Identity.
- **Solution layout** (already in place):
  - `src/UserGroupSiteMiniMaxM3.Server` — server-rendered + interactive components, Identity endpoints, EF DbContext registration.
  - `src/UserGroupSiteMiniMaxM3.Client` — WASM client components, services (e.g., `ToastService`).
  - `src/UserGroupSiteMiniMaxM3.Data` — `ApplicationDbContext` (extends `AuthDbContext`), entities, interfaces.
  - `src/UserGroupSiteMiniMaxM3.Shared` — DTOs/contracts shared by both sides.
  - `aspire/UserGroupSiteMiniMaxM3.AppHost` + `ServiceDefaults` — Aspire orchestration.
- **Identity already in place**: `User` (with `FirstName`, `LastName`, `MemberSince`), `Role`, `AuthDbContext`, register/login/passkey pages, `CustomUserClaimsPrincipalFactory`, `IUserService`/`HttpUserService`, `IEmailSender<User>`, `IToastService`/`ToastService`, lowercase URLs configured, audit logging scaffold.
- **Conventions (from repo rules)**: C# 14, file-scoped namespaces, PascalCase members, camelCase private fields, `I`-prefixed interfaces, `nameof` over string literals, nullable reference types with `is null`/`is not null`, XML doc comments on public APIs, switch expressions, pattern matching.

## High-Level Architecture

- **Domain model** lives in `UserGroupSiteMiniMaxM3.Data/Models`. `Event` and `TopicSuggestion` are new entities; user/role relationships flow through existing `IdentityDbContext` (no schema rewrite needed for users).
- **EF Core migrations** are the single source of truth for schema changes; we will not modify the existing `User`/`Role` shape except through Identity's role/user-role tables.
- **Roles** (Identity roles, stored in `AspNetRoles`): `Admin` and `Speaker`. A user can hold both.
- **Authorization** uses a combination of `[Authorize(Roles = "Admin")]` and per-record policy checks (e.g., an assigned speaker for a given event).
- **Validation** is done with `DataAnnotations` for server-side shape plus a small fluent client-side check for the multi-rule "published" requirement.
- **Markdown preview** uses a `Tabs` UX (Edit/Preview) with `Markdig` parsing server-side when rendering the event detail page; the editor's preview tab parses client-side for instant feedback.
- **Server is the source of truth** for the rendered event description (HTML is generated server-side via `Markdig` and stored or rendered on the fly). The client's edit-preview tab can use a lightweight WASM-friendly parser for instant feedback.
- **Slug generation**: client-side kebab-casing of `Title` on `onblur` if the slug input is empty.

## Step-by-Step Plan

### Step 1 — Foundations: Roles, Authorization, and User Management

**Goal:** Establish `Admin` and `Speaker` Identity roles, and provide an Admin-only user management experience that prevents admins from self-demoting.

1.1. Create a `Constants` class (in `Data/Models` or `Shared`) with role names:
- `public static class Roles { public const string Admin = "Admin"; public const string Speaker = "Speaker"; }`
- Reuse this constant everywhere instead of magic strings.

1.2. Add a one-time role-seed step:
- On app startup (after `app.Build()` but before `app.Run()`), call `await SeedRolesAsync` using `IServiceScopeFactory` to resolve `RoleManager<Role>` and create `Admin` and `Speaker` if missing. Idempotent.
- Consider also seeding a bootstrap admin from configuration (e.g., `Admin:Email` in `appsettings.json`); optional but useful for first run.

1.3. Add an admin user-management page at `/admin/users` (server component, `Roles="Admin"`):
- Table listing `UserName`, `Email`, `FirstName`, `LastName`, `MemberSince`, roles.
- "Edit Roles" modal/inline panel with two checkboxes (Admin, Speaker) plus a read-only "Speaker bio" textarea if they are a speaker (optional, not required by spec — skip unless desired).
- Server-side `ApplicationDbContext`-backed CRUD: assign/unassign roles via `UserManager.AddToRoleAsync` / `RemoveFromRoleAsync`.
- **Self-protection rule:** when the current admin opens their own row, the Admin checkbox is disabled with a tooltip: "You cannot remove your own Admin role while signed in as an admin." Server enforces the same check: if `targetUserId == currentUserId` and the request removes the Admin role, return 400 with a clear error.
- The current admin's row is visually distinct (badge "You") to make this clear.

1.4. Authorization policies (registered in `Program.cs`):
- `EventEditorPolicy`: succeeded if the user is in `Admin`, or has a per-record claim. We will instead perform per-record authorization in the page's handler using a small `IAuthorizationService` helper (`EventAccessService`) rather than a global policy, since "editor" depends on the event. (Document this decision in the helper's XML comments.)
- `RequireAdminAttribute` shortcut filter for `[Authorize(Roles = Roles.Admin)]`.

1.5. Add `IAuthorizationService`-style helper `IEventAccessService` in `Data/Services` (server-side) with:
- `Task<bool> CanEditEventAsync(ClaimsPrincipal user, Event ev, CancellationToken ct)`
- Logic: admin OR user is one of `ev.SpeakerIds`.

1.6. Update the `NavMenu` to surface `/admin/users` only when the current user is in the `Admin` role.

1.7. (Lightweight) Tests:
- Unit test for the self-demotion guard logic.
- Unit test that the role-seeding is idempotent (running twice doesn't throw or duplicate).

**Deliverable:** Admins can be created/promoted/demoted; a signed-in admin cannot demote themselves; a regular user can be marked as a Speaker without being an Admin.

---

### Step 2 — Data Model: `Event` Entity, Relationships, and Migrations

**Goal:** Persist events with all required fields and a many-to-many speaker relationship.

2.1. Add `Event` entity in `Data/Models/Event.cs`:
- `int Id` (PK).
- `string Title` (required, `[Required, MaxLength(200)]`).
- `string Slug` (required, unique, `[MaxLength(250)]`; add a unique index).
- `string? ShortDescription` (`[MaxLength(500)]`).
- `string? Description` (raw markdown; can be long, no max).
- `DateTime EventDateTime` (UTC stored; UI localizes display).
- `string? Location` (`[MaxLength(250)]`).
- `bool IsPublished`.
- `DateTime CreatedOn`, `CreatedBy`, `ModifiedOn`, `ModifiedBy` (via `FingerPrintEntityBase` base class — existing pattern).
- Navigation: `ICollection<User> Speakers` (many-to-many). Configure a join table `EventSpeakers(EventId, UserId)` in `OnModelCreating`.

2.2. Add `Event` `DbSet` to `ApplicationDbContext`; configure:
- Unique index on `Slug`.
- Cascade behavior: restrict delete on users if they are speakers of an event (use `OnDelete(DeleteBehavior.Restrict)` on the join).
- `queryFilter` for unpublished events: none globally (we filter per query for the public home page); document that decision in XML comments.

2.3. Create EF migration:
- `dotnet ef migrations add AddEvents -p ../UserGroupSiteMiniMaxM3.Data`
- Inspect the generated migration; verify the join table, unique index, and FKs.

2.4. Add `Event` DTOs in `Shared/Models/Events/`:
- `EventDto` (read shape), `EventEditDto` (write shape), `EventSummaryDto` (list shape, used on home page).

2.5. Add `EventService` (server-side) with:
- `Task<IReadOnlyList<EventSummaryDto>> ListPublishedAsync(CancellationToken ct)`
- `Task<IReadOnlyList<EventSummaryDto>> ListAllAsync(ClaimsPrincipal user, CancellationToken ct)` (admins/assigned speakers only)
- `Task<EventDto?> GetBySlugAsync(string slug, ClaimsPrincipal user, CancellationToken ct)` (unpublished allowed only for editors)
- `Task<int> CreateAsync(EventEditDto input, ClaimsPrincipal user, CancellationToken ct)`
- `Task UpdateAsync(int id, EventEditDto input, ClaimsPrincipal user, CancellationToken ct)`
- `Task DeleteAsync(int id, ClaimsPrincipal user, CancellationToken ct)`
- `Task SetSpeakersAsync(int id, IEnumerable<int> userIds, ClaimsPrincipal user, CancellationToken ct)`

2.6. Validation rules encapsulated in a `EventValidator`:
- Title and Slug are required to save.
- If `IsPublished == true`:
  - Description non-empty.
  - EventDateTime > DateTime.UtcNow (or warn if in the past — make this a soft warning, not a hard error, so admins can back-fill).
  - Location non-empty.
  - At least one speaker.
- All failures collected and returned as a `ValidationProblemDetails` (RFC 9457) for consistent API error shape.

2.7. Unit tests:
- Validator passes/fails for each rule.
- Slug uniqueness check (e.g., trying to create two events with the same slug should fail with a clear message).

**Deliverable:** Schema and persistence layer for events are in place; validation rules match the spec.

---

### Step 3 — Event Management UI (Admin & Speaker Editor Experience)

**Goal:** Admins and assigned speakers can create/edit events; the editor experience supports the spec's UX (kebab slug, markdown edit/preview, validation feedback).

3.1. Add a new Blazor page `/events` (server component) for the public listing:
- Lists published events in descending `EventDateTime` order, with title, short description, date/time (localized), location, and a "View details" link to `/events/{slug}`.
- Visible to anonymous users.
- Renders short description as plain text (no markdown), title as a link.

3.2. Add `/events/{slug}` (server component) for the public event detail page:
- Renders full description by piping the stored markdown through `Markdig` to safe HTML.
- Sanitize: use `Markdig` with `HtmlSanitizer` (or `HtmlValidator`) and `Pipeline` set to safe defaults; explicit allow-list for tags.
- Lists speakers (link to user profile later if desired; for now, just show `FirstName LastName`).

3.3. Add `/events/manage` and `/events/manage/{id}` (server components, both `[Authorize]`):
- Visible to any signed-in user; the server enforces "editor = Admin OR assigned speaker" via `IEventAccessService` per record.
- A signed-in non-editor user sees a "no events to manage" empty state with a CTA to "Suggest a topic" (links to Step 5).
- Admins see all events (published or not); assigned speakers see only the events they speak at.
- Listing columns: Title, Date/Time, Published (badge), # Speakers, Actions (Edit, [Admin-only] Delete).

3.4. Build the editor form as a reusable `EventEditor.razor` component used by both Create and Edit:
- Fields and behaviors:
  - **Title** (`<input>`, required) — bound to `model.Title`.
  - **Slug** (`<input>`, required) — auto-populated from `Title` kebab-case on `onblur` only if the slug input is currently empty; show a hint "Auto-generated from title until you edit it directly." After the user types in the slug input, do not overwrite.
  - **ShortDescription** (`<textarea>`).
  - **Description** (custom `MarkdownEditor.razor`):
    - Two tabs: **Edit** and **Preview**.
    - Edit tab: a `<textarea>` with monospace styling.
    - Preview tab: render the parsed markdown using a lightweight client-side `Markdig` call (call into a JS interop-free path — `Markdig` is a managed library and works in WASM; we can use it directly). We will register `Markdig.MarkdownPipeline` in DI for both server and client.
    - Switching tabs does not save data — it's purely a preview.
  - **EventDateTime** (`<input type="datetime-local">`, bound as `DateTime`, treat as local, convert to UTC for storage).
  - **Location** (`<input>`).
  - **Speakers** (multi-select) — uses a combobox/pill list of users. We will use a simple multi-select built on a list of checkboxes grouped by name, or a `SelectMultiple` with type-ahead. For simplicity in v1: a search-as-you-type text box + a list of selected speakers with × buttons to remove. The list of candidates comes from `GET /api/users/speakers`.
  - **IsPublished** (checkbox) — show inline help text: "Publishing requires Description, Date/Time, Location, and at least one speaker."
- Save button is disabled until Title and Slug are non-empty.
- On submit:
  - POST/PUT to `EventService`.
  - On 400 validation errors, render field-level errors in a Bootstrap `alert` and per-field `is-invalid` styling.
  - On success, redirect to the manage list and show a toast ("Event saved").

3.5. Server-side enforcement of "editor":
- Use `IEventAccessService` in the page's lifecycle (`OnInitializedAsync` for edit, and inside `EventService.UpdateAsync`/`DeleteAsync`).
- The "Speakers" picker should not allow removing oneself if the current user is not an Admin and not also an Admin (defensive: speaker-editors cannot unassign themselves from the event they are editing). Spec says editors are Admin OR assigned speakers; we'll allow a speaker to add/remove other speakers but not themselves. Document this in a tooltip.

3.6. Add `Delete` confirmation modal on the manage list (admin only).

3.7. Tests:
- Component tests for `EventEditor`: typing in Title then blurring sets Slug; editing Slug directly disables auto-fill.
- Component tests for the markdown editor: switching tabs preserves the in-progress text.
- Integration test: an Admin can create+publish an event; a non-editor user is forbidden from editing.

**Deliverable:** Full create/edit/list/detail flow for events, with the spec's UX details and authorization boundaries.

---

### Step 4 — Home Page Wiring + Header User Experience

**Goal:** The home page lists published events for everyone; the header shows "Welcome, `<firstName>`" and a logout menu for signed-in users.

4.1. Replace `Components/Pages/Home.razor` content with the published-events list (reusing the listing markup from Step 3.1). Keep `Home.razor.cs` lean; move data loading to a service call.

4.2. Update `Components/Layout/MainLayout.razor` (or whichever layout is active) to add a top-right header element:
- If `AuthenticationStateProvider` reports authenticated:
  - Render `<span>Welcome, {firstName}</span>` (fall back to `UserName` if `FirstName` is null).
  - Render a Bootstrap dropdown (`<div class="dropdown">`) with a "Log out" item.
    - The logout item posts an antiforgery-protected form to the existing `/Account/Logout` Identity endpoint (already present in the template). This must be a real form post, not a JS `fetch`, so antiforgery tokens remain valid.
- If anonymous: render "Log in" and "Register" links pointing to the existing Identity pages (`/Account/Login`, `/Account/Register`).

4.3. The logout form needs a route handler — verify whether the existing Identity endpoint at `/Account/Logout` accepts a POST with the antiforgery token; if not, add a small `Logout.razor` page with a button that submits the form (mirroring Blazor's standard pattern).

4.4. Ensure the header is responsive on mobile (collapse to a hamburger if necessary — defer to existing Bootstrap NavMenu behavior; just style consistently).

4.5. Tests:
- A `bUnit` test or render test confirming "Welcome, Alice" appears for a signed-in user and the login/register links appear for an anonymous user.

**Deliverable:** Home page lists published events; header shows the welcome text and a logout dropdown for signed-in users.

---

### Step 5 — Topic Suggestions, Voting, and Volunteering

**Goal:** Any signed-in user can suggest a topic, vote once per topic, and volunteer to speak on topics that don't already have a volunteer.

5.1. Add `TopicSuggestion` entity in `Data/Models/TopicSuggestion.cs`:
- `int Id` (PK).
- `string Title` (`[Required, MaxLength(200)]`).
- `string? Description` (markdown optional, supports the same `Markdig` rendering pipeline as event descriptions).
- `int SuggestedByUserId` (FK to `User`).
- `int? VolunteerSpeakerId` (nullable FK to `User`; null until someone volunteers).
- `DateTime CreatedOn`, `CreatedBy`, `ModifiedOn`, `ModifiedBy` (via `FingerPrintEntityBase`).
- Navigation: `User SuggestedBy`, `User? VolunteerSpeaker`, `ICollection<TopicVote> Votes`.

5.2. Add `TopicVote` entity in `Data/Models/TopicVote.cs`:
- Composite key `(TopicSuggestionId, UserId)`.
- Configure as a join entity so each (topic, user) pair can exist at most once (the natural unique index enforces "one vote per user per topic").
- `DateTime CreatedOn` for display/audit.

5.3. Add `DbSet<TopicSuggestion> TopicSuggestions` and `DbSet<TopicVote> TopicVotes` to `ApplicationDbContext`; configure:
- Unique index on `(TopicSuggestionId, UserId)` on `TopicVote`.
- `OnDelete(DeleteBehavior.Cascade)` from `TopicSuggestion` to its votes.
- `OnDelete(DeleteBehavior.Restrict)` to `User` for both `SuggestedBy` and `VolunteerSpeaker` (don't allow deleting a user who has suggested or volunteered).
- Global `queryFilter` to exclude soft-deleted items? Not in scope unless requested.

5.4. Create EF migration: `dotnet ef migrations add AddTopicSuggestions -p ../UserGroupSiteMiniMaxM3.Data`.

5.5. Add DTOs in `Shared/Models/Topics/`:
- `TopicSuggestionDto`, `TopicSuggestionEditDto`, `TopicSuggestionSummaryDto` (includes vote count, has-voted-by-current-user flag, has-volunteer flag, suggester and volunteer display names).

5.6. Add `TopicService` (server-side) with:
- `Task<IReadOnlyList<TopicSuggestionSummaryDto>> ListAsync(ClaimsPrincipal user, CancellationToken ct)`
- `Task<int> CreateAsync(TopicSuggestionEditDto input, ClaimsPrincipal user, CancellationToken ct)`
- `Task VoteAsync(int topicId, ClaimsPrincipal user, CancellationToken ct)` (idempotent: if already voted, no-op success).
- `Task UnvoteAsync(int topicId, ClaimsPrincipal user, CancellationToken ct)`.
- `Task VolunteerAsync(int topicId, ClaimsPrincipal user, CancellationToken ct)` — 409 Conflict if `VolunteerSpeakerId` is already set by another user; idempotent if the same user is re-volunteering.
- `Task WithdrawVolunteerAsync(int topicId, ClaimsPrincipal user, CancellationToken ct)` (only the volunteer themselves or an Admin).

5.7. Authorization:
- All endpoints require an authenticated user.
- Admin override not strictly required for v1, but the Volunteer-withdraw should allow an Admin to clear any volunteer's status.

5.8. UI:
- `/topics` (server component, `[Authorize]`):
  - "Suggest a topic" form at the top: title (required), description (optional, markdown).
  - List of suggestions, each row:
    - Title, short description, "Suggested by Alice on 2026-06-02", vote count, volunteer (if any) with a "Speak on this" button (disabled if someone already volunteered; current volunteer sees "Withdraw").
    - "Vote" / "Unvote" toggle button (uses current-user context).
  - Sorting: unvolunteered topics first (so the most-needed bubbles to the top), then by descending vote count, then by `CreatedOn` desc.
- Add a link in the NavMenu to `/topics` for authenticated users.

5.9. Tests:
- Voting twice by the same user is a no-op (vote count stays 1).
- Volunteering by user B when user A is already the volunteer returns 409.
- Volunteering by user A twice is idempotent.

**Deliverable:** Logged-in users can suggest, vote (one per topic), and volunteer on topics; UX surfaces the "no volunteer yet" affordance and prevents double-volunteers.

---

### Step 6 — Cross-Cutting: Error Handling, API Surface, Logging, and Tests

**Goal:** Production-grade concerns layered on top of the feature work.

6.1. Centralized API endpoints for the editor forms:
- Use server-side Blazor components for the editor pages; only add Minimal API endpoints if/where a non-Blazor client needs them (e.g., a `/api/users/speakers` lookup for the speaker picker). Document this trade-off in a comment on the endpoint.
- All API responses use `ProblemDetails` (`Results.Problem(...)`) on errors with a stable `traceId`.

6.2. Global exception handler:
- A small middleware that catches unhandled exceptions, logs with `Serilog` (already wired), and returns a sanitized error page for users.
- For API calls, return `application/problem+json`.

6.3. Logging:
- `Serilog` is already configured. Add structured log events for:
  - Event published/unpublished transitions.
  - Role changes (who changed whom, from what to what).
  - Volunteer assignments/withdrawals.
- Include the actor's user id in all log events.

6.4. Health checks:
- `Aspire` already adds default endpoints. Add a SQL Server readiness check to ensure the DB is reachable before the first request completes.

6.5. Test infrastructure:
- Add a test project: `tests/UserGroupSiteMiniMaxM3.Tests` (xUnit + bUnit + FluentAssertions).
- Add an in-memory or SQLite-backed test fixture for `ApplicationDbContext` so validator and service tests don't need SQL Server.
- Add an integration test fixture that boots the Server project in-process (using `WebApplicationFactory<Program>`) for end-to-end checks of the create/publish flow.

6.6. CI hints:
- Add a `dotnet format` step and a `dotnet test` step to `.github/workflows/`.

**Deliverable:** The site is observability- and test-ready; errors and authorization are uniformly surfaced.

---

### Step 7 — Polish, Accessibility, and Documentation

**Goal:** Ship-ready UX and a clear README.

7.1. Accessibility:
- All form fields have associated `<label>`s.
- Markdown preview tab uses `role="tablist"` with `aria-selected` and keyboard navigation (left/right arrows).
- Color contrast passes WCAG AA (verify with a quick Lighthouse audit).
- The "Welcome, `<firstName>`" dropdown has `aria-haspopup="menu"` and proper focus management.

7.2. Empty / loading / error states:
- Home page with no events: friendly empty state ("No upcoming events. Check back soon!").
- Manage list with no events: empty state with a "Create your first event" CTA.
- Topic suggestions empty: empty state with "Be the first to suggest a topic."

7.3. Seed data (Development only, behind a config flag):
- A few sample speakers and a couple of past+upcoming events so the UI is exercised on first run.

7.4. Update the repo `README.md`:
- Quick start (Aspire + EF migrations).
- Role overview (Admin / Speaker / Member).
- Links to the relevant pages: `/`, `/events`, `/events/manage`, `/topics`, `/admin/users`.
- Notes on Markdown rendering and slug generation.

7.5. Final pass:
- Run `dotnet format`.
- Run the full test suite.
- Run `dotnet build` and confirm zero warnings (treat warnings as errors is on in `Directory.Build.props` — verify and document).

**Deliverable:** A polished, accessible, documented site.

---

## Decisions and Trade-offs to Document in Code Comments

- **Markdown safety**: server-rendered HTML goes through `HtmlSanitizer` with an explicit allow-list; we never `HtmlString` raw user input.
- **UTC vs local times**: `EventDateTime` is stored as UTC; the UI binds `<input type="datetime-local">` in the user's local time. The conversion happens in `EventService` (parse as `DateTimeKind.Local`, convert to UTC on save; convert to local on read for display).
- **Self-demotion guard** is enforced both in the UI (disabled checkbox) and in the server (`UserManager` call wrapped in a service that checks `targetUserId == currentUserId` and `isRemovingAdmin`).
- **Per-record authorization** ("editor of this event") is implemented with a small service rather than a global policy, because the policy is data-dependent.
- **No global query filter for unpublished events** — the public list query explicitly filters `IsPublished == true`. This makes the intent obvious at the call site and avoids surprising shadowing in admin queries.
- **Slugs** are user-overridable; once the user types in the slug field, auto-generation stops. Uniqueness is enforced at the DB level.
- **Topic vote uniqueness** is enforced by the composite key on `(TopicSuggestionId, UserId)` — the natural shape of the data guarantees "one vote per user per topic" without extra application logic.
- **Volunteer uniqueness** is enforced at the application level (the `Volunteer` endpoint returns 409 if already taken), since the schema allows a nullable FK. We considered a unique filtered index but kept the check in the service for clearer error messages.

## Open Questions (worth confirming before/during implementation)

1. **Should an Admin be able to assign a speaker who is not in the "Speaker" role?** The spec says users can be "Admins, Speakers, or both." It implies a user is in the Speaker role if they speak. Decision: yes, a speaker doesn't have to be in the `Speaker` role to be assigned to an event (the role is a label, the assignment is a relationship). Document this in the user-management page tooltip.
2. **Past-dated events**: should publishing an event with a past date warn or block? Plan: soft warn, don't block (admins back-fill).
3. **Markdown library**: confirm `Markdig` is acceptable (it's MIT-licensed and the most popular for .NET). Add to `Directory.Packages.props` once approved.
4. **Bootstrap vs Tailwind**: the project uses Bootstrap SCSS already; we'll stay with Bootstrap.
5. **Email confirmation**: Identity is set to `RequireConfirmedAccount = true`; do we need a real email sender for production, or is `IdentityNoOpEmailSender` acceptable for v1? Plan: keep `NoOp` for now and add a comment that real SMTP config is a follow-up.
6. **Pagination**: not required by spec, but add a "load more" or simple paging on the home page if event count grows. Defer until needed.
