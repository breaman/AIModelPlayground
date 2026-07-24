# User Group Site — Implementation Plan

A website for managing technical user group meetings: events, speakers, topic suggestions, and
user administration.

This plan is written against the current repository state (the `breaman.blazor` template, commit
`2fae0fa`) and follows the conventions in `.claude/rules/csharp.instructions.md` and
`.claude/rules/blazor.instructions.md`.

---

## 0. Baseline: what already exists

Understanding the starting point avoids rebuilding things the template already provides.

| Concern | Status in the template |
| --- | --- |
| Solution layout | `src/{Data,Shared,Client,Server}` + `aspire/{AppHost,ServiceDefaults}` |
| Database | SQL Server via Aspire container (`AppHost.cs`), EF Core 10 |
| Identity | `IdentityDbContext<User, Role, int>`, roles enabled, passkeys, 2FA, full Account UI |
| `User` | `IdentityUser<int>` + `FirstName`, `LastName`, `MemberSince` |
| Auditing | `AuthDbContext` writes `AuditLog` rows and stamps `FingerPrintEntityBase` on save |
| Claims | `CustomUserClaimsPrincipalFactory` already adds a `FirstName` claim |
| Header | `NavMenu.razor` **already** renders "Welcome, {FirstName}" with a dropdown containing Log out |
| Toasts | `IToastService` / `ToastContainer` wired into `MainLayout` |
| Home page | `Server/Components/Pages/Home.razor` — placeholder text |
| Migrations | **None yet.** `src/UserGroupSiteOpus5.Data/Migrations/` does not exist |
| Tests | **No test project exists** |

Two consequences worth calling out up front:

1. **The initial migration has not been created.** Rather than create an Identity-only migration
   now and a domain migration later, define the domain entities first (Phase 2) and emit a single
   `InitialDatabase` migration that covers both.
2. **Much of the "UI Pieces" requirement is already satisfied.** Phase 9 is verification plus
   navigation links, not new construction.

### Architectural constraints (non-negotiable, from the repo rules)

- **Never** `@rendermode InteractiveServer`. Interactive components use
  `@rendermode InteractiveWebAssembly` **and must live in the `Client` project**.
- Interactive components follow the dual-service pattern: interface in `Shared`, an HTTP-calling
  implementation in `Client`, a database-calling implementation in `Server`, and `[PersistentState]`
  on the pre-rendered data properties.
- No `@code` blocks in `.razor` files — always a `.razor.cs` code-behind.
- Bootstrap 5 for styling, Bootstrap Icons for icons, form inputs wrapped in `form-floating`.
- `TreatWarningsAsErrors=True` is on solution-wide — new code must build clean.

**Render-mode decision rule used throughout this plan:** a page that needs no interactivity stays
static SSR in the `Server` project (better first paint, works for anonymous/SEO traffic); anything
with buttons, forms, or live preview becomes an `InteractiveWebAssembly` component in the `Client`
project.

| Page | Project | Render mode | Why |
| --- | --- | --- | --- |
| Home (published events) | Server | Static SSR | Read-only, anonymous, SEO-relevant |
| Event detail | Server | Static SSR | Read-only |
| Event create/edit | Client | InteractiveWebAssembly | Form, markdown preview, slug-on-blur |
| Admin → Users | Client | InteractiveWebAssembly | Role toggles |
| Topic suggestions | Client | InteractiveWebAssembly | Vote / volunteer / create |

---

## 1. Prerequisites and environment verification

**Goal:** a running baseline before any feature code is written.

1. `cd src/UserGroupSiteOpus5.Server && npm install` then `npm run sass-dev` — the template's
   `site.css` is generated from `styles/site.scss` and is not committed.
2. `dotnet tool restore` (installs `dotnet-ef` 10.0.10 as a local tool).
3. `dotnet build` from the repo root to confirm a clean baseline.
4. `aspire run` — confirm the SQL Server container starts and the site loads.

> If the build fails with **RZ1021**, run `dotnet build-server shutdown` and rebuild. This is a
> known SDK 10.0.301 issue documented in `.claude/rules/blazor.instructions.md`; do not "fix" the
> Razor files.

**Exit criteria:** site loads at its HTTPS endpoint, Aspire dashboard shows a healthy server.

---

## 2. Domain model and database

**Goal:** all entities, EF configuration, one initial migration, and seed data.

### 2.1 Extend the shared constants

`Shared/Common/FieldLengths.cs` — add:

```csharp
public const int EventTitle = 200;
public const int Slug = 200;
public const int ShortDescription = 500;
public const int Location = 300;
public const int TopicTitle = 200;
```

`Data/Common/ColumnTypes.cs` — add:

```csharp
/// <summary>Unbounded Markdown body text.</summary>
public const string MarkdownText = "nvarchar(max)";
```

### 2.2 Entities (`src/UserGroupSiteOpus5.Data/Models/`)

All derive from `FingerPrintEntityBase` so they pick up created/modified stamping and audit
logging for free.

**`Event`**

| Property | Type | Notes |
| --- | --- | --- |
| `Title` | `string` | Required, `FieldLengths.EventTitle` |
| `Slug` | `string` | Required, `FieldLengths.Slug`, **unique index** |
| `ShortDescription` | `string?` | `FieldLengths.ShortDescription` |
| `Description` | `string?` | Markdown source, `ColumnTypes.MarkdownText` |
| `EventDateTime` | `DateTimeOffset?` | Nullable — only required to publish |
| `Location` | `string?` | `FieldLengths.Location` |
| `IsPublished` | `bool` | Default `false` |
| `Speakers` | `ICollection<EventSpeaker>` | |

**`EventSpeaker`** — join between `Event` and `User`. Own `Id` (inherited) plus a unique index on
`(EventId, UserId)`. A surrogate key rather than a composite one keeps it compatible with
`FingerPrintEntityBase` and the audit-log machinery.

**`TopicSuggestion`**

| Property | Type | Notes |
| --- | --- | --- |
| `Title` | `string` | Required, `FieldLengths.TopicTitle` |
| `Description` | `string?` | `FieldLengths.Description` (4000, plain text) |
| `SuggestedByUserId` | `int` | FK → `User` |
| `VolunteerUserId` | `int?` | Null until someone volunteers |
| `Votes` | `ICollection<TopicSuggestionVote>` | |

**`TopicSuggestionVote`** — `TopicSuggestionId`, `UserId`, unique index on the pair. The unique
index is what actually enforces "one vote per user per topic"; the service-layer check is a
friendlier-error convenience, not the guarantee.

### 2.3 EF configuration

- Add `IEntityTypeConfiguration<T>` classes under `Data/Configurations/` (one per entity) rather
  than fluent code inline in `OnModelCreating` — keeps `ApplicationDbContext` readable.
- Call `builder.ApplyConfigurationsFromAssembly(...)` from an `OnModelCreating` override in
  `ApplicationDbContext`. **Remember to call `base.OnModelCreating(builder)` first** — Identity's
  own mapping depends on it.
- Add `DbSet<>` properties to `ApplicationDbContext` for `Event`, `EventSpeaker`,
  `TopicSuggestion`, `TopicSuggestionVote`.
- Delete behaviour: `Cascade` from `Event` → `EventSpeaker` and `TopicSuggestion` → votes;
  `Restrict` on every FK pointing at `User` (deleting a user must not silently erase event
  history).

### 2.4 Roles and seeding

- Add `Shared/Common/RoleNames.cs` with `public const string Admin = "Admin";` and
  `Speaker = "Speaker";` — one definition consumed by both `[Authorize(Roles = ...)]` attributes
  and role-management code, so the strings cannot drift.
- Add a hosted seeder (`Server/Data/DatabaseSeeder.cs`) run at startup that:
  - Ensures both roles exist via `RoleManager<Role>`.
  - Bootstraps a first admin from configuration (`Admin:Email` / `Admin:Password`, dev values in
    user secrets — the solution already shares one `UserSecretsId`). Without this there is no way
    to get the first admin into the system.
  - Optionally seeds a couple of sample events in Development only.

### 2.5 Migration

```
cd src/UserGroupSiteOpus5.Server
dotnet ef migrations add InitialDatabase -p ../UserGroupSiteOpus5.Data
```

Aspire's `ef-migrations` resource applies it on start; no manual `database update` needed.

**Exit criteria:** `aspire run` creates the schema, roles exist, the bootstrap admin can log in.

---

## 3. Authorization model

**Goal:** one authoritative place that answers "may this user edit this event?".

1. **Policies** registered in `Server/Program.cs`:
   - `"AdminOnly"` → `RequireRole(RoleNames.Admin)`.
   - `"SpeakerOrAdmin"` → `RequireRole(RoleNames.Admin, RoleNames.Speaker)`.
2. **Event editing is resource-based**, not role-based: an editor is *any Admin* **or** *a speaker
   assigned to that specific event*. A blanket `[Authorize(Roles="Speaker")]` would wrongly let any
   speaker edit any event. Implement as an `IAuthorizationHandler`
   (`EventEditorAuthorizationHandler`) over an `EventEditRequirement`, evaluated server-side with
   the event's speaker list loaded.
3. Expose the same decision to the UI through a `CanEditEventAsync(eventId)` call on the shared
   event service so buttons can be hidden — but **the server check is the enforcement point**;
   hiding a button is cosmetic.
4. Roles already flow into the auth cookie via `AddRoles<Role>()` and the custom claims factory, so
   `AuthorizeView Roles="Admin"` works client-side with no extra work.

> **Note on role changes taking effect:** role claims live in the auth cookie. When an admin grants
> or revokes a role, the affected user's existing cookie is stale until they sign in again. Set
> `SecurityStampValidatorOptions.ValidationInterval` (e.g. 30 minutes, or shorter in Development)
> and call `UserManager.UpdateSecurityStampAsync` after a role change so the cookie is refreshed.

**Exit criteria:** unit tests over the handler cover admin, assigned speaker, unassigned speaker,
and anonymous.

---

## 4. Shared contracts (DTOs, service interfaces, validation)

**Goal:** one set of types both `Client` and `Server` compile against.

Everything here goes in `src/UserGroupSiteOpus5.Shared/` — it is referenced by `Client`, and by
`Server` transitively through `Data`.

### 4.1 DTOs (`Shared/Models/`)

Records for reads, classes for form-bound models (Blazor two-way binding needs settable
properties):

- `EventListItem`, `EventDetail`, `EventSpeakerInfo`
- `EventEditModel` (form-bound class)
- `TopicSuggestionListItem` — includes `VoteCount`, `HasCurrentUserVoted`, `VolunteerName`
- `TopicSuggestionCreateModel`
- `UserListItem` (`Id`, `FirstName`, `LastName`, `Email`, `IsAdmin`, `IsSpeaker`)
- `UserRoleUpdateModel`

### 4.2 Validation

Use DataAnnotations plus `IValidatableObject` on `EventEditModel`:

- `[Required]` on `Title` and `Slug` — always.
- `IValidatableObject.Validate` enforces the conditional publish rules: when `IsPublished` is true,
  `Description`, `EventDateTime`, `Location`, and at least one speaker must be present.

Blazor's `DataAnnotationsValidator` runs per-property on field change and the **full** object
(including `IValidatableObject`) on submit — so cross-field publish errors appear on submit, which
is the right moment for them. The same model is re-validated server-side in the event service; the
client check is UX, the server check is the rule.

### 4.3 Service interfaces (`Shared/Services/`)

```csharp
public interface IEventService
{
    Task<IReadOnlyList<EventListItem>> GetPublishedEventsAsync();      // anonymous, date desc
    Task<IReadOnlyList<EventListItem>> GetManageableEventsAsync();     // admin/speaker view
    Task<EventDetail?> GetEventBySlugAsync(string slug);
    Task<EventDetail?> GetEventForEditAsync(int id);
    Task<SaveResult<int>> SaveEventAsync(EventEditModel model);
    Task<string> GenerateUniqueSlugAsync(string title, int? excludeEventId);
    Task<IReadOnlyList<EventSpeakerInfo>> GetAvailableSpeakersAsync();
}

public interface ITopicSuggestionService
{
    Task<IReadOnlyList<TopicSuggestionListItem>> GetSuggestionsAsync();
    Task<SaveResult<int>> CreateSuggestionAsync(TopicSuggestionCreateModel model);
    Task<SaveResult> VoteAsync(int suggestionId);
    Task<SaveResult> VolunteerAsync(int suggestionId);
}

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListItem>> GetUsersAsync();
    Task<SaveResult> UpdateUserRolesAsync(UserRoleUpdateModel model);
}
```

`SaveResult` / `SaveResult<T>` is a small shared result record (`Succeeded`, `Errors`,
`Value`) so both transports report failures the same way, rather than one throwing and the other
returning a status code.

### 4.4 Markdown rendering

- Add **Markdig `1.3.2`** to `Directory.Packages.props` and reference it from `Shared` (both the
  WASM preview tab and server-side detail rendering need it).
- Add `Shared/Services/MarkdownRenderer.cs` with a single shared, static `MarkdownPipeline`
  configured with `.UseAdvancedExtensions().DisableHtml()`.
- **`DisableHtml()` is the security control here.** It escapes raw HTML embedded in Markdown, so
  user-authored descriptions cannot inject `<script>`. Without it, `MarkupString` output is a
  stored-XSS hole. If richer HTML is ever wanted, add `HtmlSanitizer` (`9.0.967`, namespace
  `Ganss.Xss`) server-side instead of relaxing this — it pulls in AngleSharp, which is heavy for a
  WASM payload.
- Render with `@((MarkupString)MarkdownRenderer.ToHtml(model.Description))`.

---

## 5. Server implementation (data services + API)

**Goal:** the server-side half of every shared interface, plus the HTTP surface the WASM client
calls.

### 5.1 Server services (`Server/Services/`)

`ServerEventService`, `ServerTopicSuggestionService`, `ServerUserAdminService` — each implements
the shared interface directly against `ApplicationDbContext` / `UserManager<User>`. These are what
run during pre-rendering.

Implementation notes:

- Read queries use `AsNoTracking()` and project straight into DTOs with `Select` — avoid loading
  entity graphs just to map them.
- `GetPublishedEventsAsync` filters `IsPublished` and orders by `EventDateTime` **descending**.
- Vote counts come from a grouped projection, not `.Count()` per row in a loop (N+1).
- Every mutating method re-checks authorization and re-validates the model. Never trust the client.

### 5.2 Slug generation

`SlugGenerator` helper in `Shared`: lowercase, strip diacritics, non-alphanumeric → `-`, collapse
repeats, trim. `GenerateUniqueSlugAsync` appends `-2`, `-3`, … on collision. The unique index is
the real guard; catch its `DbUpdateException` and surface a friendly error rather than a 500.

### 5.3 Minimal API endpoints (`Server/Endpoints/`)

Grouped extension methods mapped from `Program.cs`, one file per area:

| Method | Route | Authorization |
| --- | --- | --- |
| GET | `/api/events` | Anonymous (published only) |
| GET | `/api/events/manage` | `SpeakerOrAdmin` |
| GET | `/api/events/{slug}` | Anonymous |
| GET | `/api/events/{id:int}/edit` | Event-editor policy |
| POST | `/api/events` | `SpeakerOrAdmin` (admins only for create) |
| PUT | `/api/events/{id:int}` | Event-editor policy |
| GET | `/api/events/slug?title=…` | `SpeakerOrAdmin` |
| GET | `/api/speakers` | `SpeakerOrAdmin` |
| GET/POST | `/api/topics` | Authenticated |
| POST | `/api/topics/{id:int}/vote` | Authenticated |
| POST | `/api/topics/{id:int}/volunteer` | Authenticated |
| GET | `/api/admin/users` | `AdminOnly` |
| PUT | `/api/admin/users/{id:int}/roles` | `AdminOnly` |

Endpoints are thin: authorize → delegate to the server service → map `SaveResult` to
`Results.Ok` / `Results.ValidationProblem` / `Results.Forbid`. Add a `ProblemDetails` handler
(RFC 9457) so failures are shaped consistently.

**CSRF:** these endpoints authenticate with the Identity cookie. `app.UseAntiforgery()` does not
validate JSON requests. Mitigate by (a) keeping the Identity cookie's default `SameSite=Lax`, and
(b) requiring a custom request header on state-changing calls that the `Client` `HttpClient` sets
via a `DelegatingHandler` — cross-site form posts cannot set custom headers. Decide and apply this
once, in Phase 5, rather than per-endpoint.

### 5.4 DI registration in `Server/Program.cs`

```csharp
builder.Services.AddScoped<IEventService, ServerEventService>();
builder.Services.AddScoped<ITopicSuggestionService, ServerTopicSuggestionService>();
builder.Services.AddScoped<IUserAdminService, ServerUserAdminService>();
```

Also register `IHttpContextAccessor` explicitly if it is not already implied — `HttpUserService`
depends on it.

---

## 6. Client implementation

**Goal:** the WASM half of the dual-service pattern.

1. `Client/Services/`: `ClientEventService`, `ClientTopicSuggestionService`,
   `ClientUserAdminService` — each takes `HttpClient` and calls the matching endpoint, using
   `GetFromJsonAsync` / `PostAsJsonAsync`.
2. Register all three against the shared interfaces in `Client/Program.cs`.
3. Add a `DelegatingHandler` that attaches the anti-CSRF header from §5.3 to the shared
   `HttpClient`.
4. Wrap calls in try/catch and surface failures through `IToastService` — the rules require user
   feedback on API errors, and the toast infrastructure is already wired up.

**Exit criteria:** identical behaviour whether a page renders pre-rendered (server service) or
hydrated (client service).

---

## 7. Events UI

### 7.1 Home page — published events (`Server/Components/Pages/Home.razor`)

Replace the placeholder. Static SSR, injects `IEventService` (server implementation), renders
published events **descending by date**, visible to anonymous users. Bootstrap card or list-group
per event: title, date, location, short description, speaker names, link to detail. Include an
empty state.

### 7.2 Event detail (`Server/Components/Pages/EventDetail.razor`, `@page "/events/{Slug}"`)

Static SSR. Loads by slug, 404s via the existing `NotFound` page when missing or when unpublished
and the viewer is not an editor. Renders `Description` through `MarkdownRenderer`. Shows an
**Edit** button when `CanEditEventAsync` is true.

### 7.3 Event list for editors (`Client/Components/Pages/ManageEvents.razor`)

`@page "/events/manage"`, `@attribute [Authorize(Policy = "SpeakerOrAdmin")]`. Admins see all
events; speakers see only events they are assigned to. Table with published/draft badges, plus a
**New Event** button for admins.

### 7.4 Event editor (`Client/Components/Pages/EventEdit.razor`)

`@page "/events/new"` and `@page "/events/{Id:int}/edit"`, `InteractiveWebAssembly`, code-behind in
`EventEdit.razor.cs`. This is the most intricate component in the project:

- `EditForm` + `DataAnnotationsValidator` + `ValidationSummary`, every input in a `form-floating`
  wrapper.
- **Slug-on-blur:** `@onblur` on the Title input calls the service's slug generator *only when the
  Slug field is empty*, then `StateHasChanged()`. Never overwrite a slug the user typed.
- **Markdown edit/preview:** Bootstrap nav-tabs. The *Edit* tab is a `<textarea>` bound to
  `Description`; the *Preview* tab renders `(MarkupString)MarkdownRenderer.ToHtml(Description)`.
  Render the preview lazily (only when the tab is active) so keystrokes don't re-run Markdig.
- **Speaker assignment:** multi-select over `GetAvailableSpeakersAsync()` (users in the Speaker
  role). A checkbox list is easier to use than a multi-select box and shows current assignments at
  a glance.
- **Publish toggle:** a checkbox. When checked with incomplete data, submit fails validation and
  the summary explains exactly which of description / date / location / speaker is missing. Show a
  live "not ready to publish" hint next to the toggle so the user isn't surprised at submit time.
- Save → toast → navigate to the event detail page.

**Exit criteria:** an admin can create a draft with only title+slug; publishing without a speaker
is rejected client- *and* server-side; an assigned speaker can edit their own event but gets a 403
on someone else's.

---

## 8. Topic suggestions UI

`Client/Components/Pages/TopicSuggestions.razor`, `@page "/topics"`, `@attribute [Authorize]`,
`InteractiveWebAssembly`.

- **List** — card or list-group per suggestion: title, description, vote count, who suggested it,
  volunteer name (or "Needs a speaker"). Sort by vote count descending.
- **Suggest** — a `form-floating` form (inline or a Bootstrap modal) posting
  `TopicSuggestionCreateModel`.
- **Vote** — a button showing the count; disabled with a filled icon once the current user has
  voted. `HasCurrentUserVoted` comes down with the DTO so no extra round trip is needed. Server
  rejects duplicates; the unique index is the backstop.
- **Volunteer** — button shown only when `VolunteerUserId is null`. Once claimed, display the
  volunteer's name. Server re-checks that the slot is still open (two users can click at the same
  time) and returns a clear error to the loser of the race.
- Optimistic UI update after each action, reconciled from the response; toast on failure.

---

## 9. Header, navigation, and access wiring

Most of this already works — this phase is verification plus additions.

1. **Verify** `NavMenu.razor` renders "Welcome, {FirstName}" for logged-in users with Log out in
   the dropdown. It does today; confirm the `FirstName` claim populates after a fresh login and
   that the fallback to `Identity.Name` looks acceptable for users who registered without a first
   name.
2. **Extend `Register.razor`** to capture `FirstName` / `LastName` and set `MemberSince` — the
   header requirement depends on `FirstName` being present, and the stock template does not ask for
   it. Verify what the template's register page currently collects before assuming.
3. **Add nav links:** Events (anonymous), Topics (`AuthorizeView`), Manage Events
   (`AuthorizeView Roles="Admin,Speaker"`), Admin → Users (`AuthorizeView Roles="Admin"`).
4. Update the navbar brand from `UserGroupSiteOpus5` to the group's name.

> **Registration friction to decide on:** `Program.cs` sets
> `options.SignIn.RequireConfirmedAccount = true` while `IEmailSender<User>` is
> `IdentityNoOpEmailSender`. Self-registered users therefore land on a confirmation page containing
> the confirmation link instead of receiving an email. For development that is fine; before any
> real deployment either wire a real email sender or relax the flag. Flagging it rather than
> silently changing it — it's a product decision.

---

## 10. Admin user management

`Client/Components/Pages/Admin/Users.razor`, `@page "/admin/users"`,
`@attribute [Authorize(Policy = "AdminOnly")]`, `InteractiveWebAssembly`.

- Table of users: name, email, member since, **Admin** checkbox, **Speaker** checkbox.
- **Self-demotion guard** — the explicit requirement. Enforce in two places:
  1. **UI:** the current user's own Admin checkbox is rendered `disabled` with a tooltip
     ("You cannot remove your own Admin role").
  2. **Server:** `UpdateUserRolesAsync` compares the target user id against `IUserService.UserId`
     and rejects an Admin removal on self with a clear error. The disabled checkbox is cosmetic;
     this check is the rule. It must also survive a hand-crafted request to
     `PUT /api/admin/users/{id}/roles`.
- Consider also blocking removal of the *last* remaining admin — not in the stated requirements,
  but it is the same class of lockout and cheap to add here. Raising it as a suggestion, not
  building it unasked.
- After a successful role change, call `UpdateSecurityStampAsync` on the target user (see §3) so
  their cookie refreshes rather than carrying stale roles.
- Search/filter box, and pagination if the member list is expected to grow.

---

## 11. Tests

No test project exists yet, so this phase includes standing one up.

1. **Create `tests/UserGroupSiteOpus5.Tests/`**, add it to `UserGroupSiteOpus5.slnx`, and add these
   to `Directory.Packages.props` (central package management is enabled — versions live only
   there):

   | Package | Version |
   | --- | --- |
   | `xunit.v3` | `3.2.2` |
   | `bunit` | `2.7.2` |
   | `NSubstitute` | `6.0.0` |
   | `Shouldly` | `4.3.0` |
   | `Microsoft.AspNetCore.Mvc.Testing` | `10.0.10` |
   | `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.10` |

   Use **SQLite in-memory**, not `EntityFrameworkCore.InMemory`, for data-layer tests — the
   in-memory provider does not enforce unique indexes, which is exactly the behaviour the vote and
   slug tests need to verify.

2. **Unit tests**
   - `SlugGenerator`: casing, punctuation, diacritics, collision suffixes.
   - `EventEditModel` validation: title/slug required; each publish precondition rejected
     individually; a valid published event passes.
   - `EventEditorAuthorizationHandler`: admin / assigned speaker / unassigned speaker / anonymous.
   - `MarkdownRenderer`: `<script>` in the source is escaped, not emitted.

3. **Service tests** (SQLite in-memory)
   - Published events return in descending date order and exclude drafts.
   - Duplicate vote is rejected.
   - Volunteering for an already-claimed topic is rejected.
   - **An admin cannot remove their own Admin role** — the requirement deserves a dedicated test.
   - Slug collisions produce unique slugs.

4. **Component tests** (bUnit) for the event editor: slug populates on title blur only when empty;
   preview tab renders markdown; publish toggle with missing data surfaces validation messages.

5. **Integration tests** (`WebApplicationFactory`) for endpoint authorization: anonymous is
   rejected on protected routes; a speaker gets 403 on another speaker's event; an admin succeeds.

Per the C# rules: no `// Arrange` / `// Act` / `// Assert` comments, and match the naming style of
neighbouring tests.

---

## 12. Polish and documentation

1. **Empty and loading states** everywhere — pre-rendered components show data immediately, but
   hydrated fetches need a spinner and a "no events yet" state.
2. **`ErrorBoundary`** around the main content in `MainLayout` so a component fault does not blank
   the page.
3. **Responsive check** at mobile widths — the event editor's tabs and the admin table are the two
   likely offenders.
4. **Accessibility** — labels on every input (`form-floating` requires them anyway), `aria-label`
   on icon-only buttons, keyboard-reachable tabs.
5. **`npm run sass-prod`** before any production build.
6. **Update `README.md`** with the roles, the bootstrap-admin configuration keys, and the seeding
   behaviour.
7. **Consider adding a root `CLAUDE.md`** capturing the render-mode decision rule and the dual
   service pattern, so future work does not have to re-derive them from `.claude/rules/`.

---

## Requirement → phase traceability

| Requirement | Phase |
| --- | --- |
| Users self-register | 9 (template provides; add name capture) |
| Admin manages Admin/Speaker roles | 2.4, 3, 10 |
| Admin cannot remove own Admin role | 10 (UI + server + dedicated test in 11) |
| Event fields (title, slug, descriptions, date, location, speakers, published) | 2.2, 7.4 |
| Slug auto-populates from title on blur | 5.2, 7.4 |
| Markdown description with edit/preview tabs | 4.4, 7.4 |
| Title + slug required to save | 4.2, 7.4 |
| Published requires description, date, location, ≥1 speaker | 4.2, 5.1, 7.4 |
| Editor = any admin or assigned speaker | 3, 5.3, 7.2–7.4 |
| Any user may suggest a topic | 8 |
| One vote per user per topic | 2.2, 5.1, 8 |
| Volunteer for an unclaimed topic | 5.1, 8 |
| Home lists published events, descending, public | 7.1 |
| "Welcome, {FirstName}" in header | 9 (already present — verify) |
| Log out as dropdown item under the welcome element | 9 (already present — verify) |

---

## Suggested execution order

Phases 1 → 2 → 3 → 4 → 5 → 6 are sequential; each builds on the last. After Phase 6 the three
feature slices (7 Events, 8 Topics, 10 Admin) are independent and can be built in any order, with
Phase 9 folded in alongside whichever comes first. Phase 11 is written incrementally as each slice
lands, not saved for the end. Phase 12 closes out.

The highest-risk items — worth tackling early within their phases — are the resource-based event
authorization (Phase 3), the CSRF decision for the JSON API (Phase 5.3), and the markdown
preview's render performance (Phase 7.4).
