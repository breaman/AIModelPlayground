# User Group Site — Implementation Plan

## Technology Stack (already established)

- **Framework**: ASP.NET Core 10, Blazor WebAssembly (InteractiveWebAssembly render mode only)
- **Data**: Entity Framework Core, SQL Server, ASP.NET Identity (User/Role with `int` keys)
- **UI**: Bootstrap 5, Bootstrap Icons
- **Logging**: Serilog
- **Orchestration**: .NET Aspire
- **Architecture**: Dual-service pattern — interface in Shared, Server implementation hits DB directly, Client implementation calls HTTP API. Components with `@rendermode InteractiveWebAssembly` live in the Client project and use `[PersistentState]` for pre-rendering.

---

## Phase 1: Data Layer — Event and Topic Suggestion Models

### 1.1 Event Entity
Add `Event` to `UserGroupSiteSonnet46.Data/Models/`:

| Property | Type | Notes |
|---|---|---|
| `Id` | `int` | PK, via `EntityBase` |
| `Title` | `string` (200) | Required |
| `Slug` | `string` (200) | Required, unique index |
| `ShortDescription` | `string?` (500) | |
| `Description` | `string?` | Markdown content |
| `EventDateTime` | `DateTimeOffset?` | Required when published |
| `Location` | `string?` (300) | Required when published |
| `IsPublished` | `bool` | Default false |

Inherit from `FingerPrintEntityBase` so audit fingerprinting is automatic.

### 1.2 EventSpeaker Join Entity
Many-to-many between `Event` and `User` (speakers):

| Property | Type |
|---|---|
| `EventId` | `int` (FK → Event) |
| `UserId` | `int` (FK → User) |

Composite PK `(EventId, UserId)`. Navigation collections on `Event` (`ICollection<EventSpeaker> Speakers`).

### 1.3 TopicSuggestion Entity
Inherit from `FingerPrintEntityBase`:

| Property | Type | Notes |
|---|---|---|
| `Id` | `int` | PK |
| `Title` | `string` (200) | Required |
| `Description` | `string?` | |
| `SuggestedById` | `int` | FK → User |
| `VolunteerUserId` | `int?` | FK → User, null if no volunteer |

### 1.4 TopicVote Entity
Tracks one vote per user per topic:

| Property | Type |
|---|---|
| `TopicSuggestionId` | `int` (FK) |
| `UserId` | `int` (FK) |

Composite PK `(TopicSuggestionId, UserId)`.

### 1.5 ApplicationDbContext Updates
- Add `DbSet<Event> Events`
- Add `DbSet<EventSpeaker> EventSpeakers`
- Add `DbSet<TopicSuggestion> TopicSuggestions`
- Add `DbSet<TopicVote> TopicVotes`
- Configure relationships, unique slug index, and cascade behaviors in `OnModelCreating`

### 1.6 EF Core Migration
Generate and apply migration:
```
dotnet ef migrations add AddEventsAndTopics --project src/UserGroupSiteSonnet46.Data --startup-project src/UserGroupSiteSonnet46.Server
dotnet ef database update --project src/UserGroupSiteSonnet46.Data --startup-project src/UserGroupSiteSonnet46.Server
```

---

## Phase 2: User Management (Admin)

### 2.1 Shared — DTOs and Service Interface
In `UserGroupSiteSonnet46.Shared`:
- `UserDto` record: `Id`, `FirstName`, `LastName`, `Email`, `IsAdmin`, `IsSpeaker`
- `IUserManagementService` interface:
  - `Task<List<UserDto>> GetAllUsersAsync()`
  - `Task SetRoleAsync(int userId, string role, bool enabled)`

### 2.2 Server — Implementation and API Endpoint
In `UserGroupSiteSonnet46.Server`:
- `ServerUserManagementService` uses `UserManager<User>` and `RoleManager<Role>` directly
- Seed `"Admin"` and `"Speaker"` roles on startup if they don't exist
- Minimal API group at `/api/users` (require `"Admin"` policy):
  - `GET /api/users` → returns `List<UserDto>`
  - `POST /api/users/{userId}/roles` → body `{ role, enabled }` — validates caller is not removing their own Admin role

### 2.3 Client — HTTP Implementation
In `UserGroupSiteSonnet46.Client`:
- `ClientUserManagementService` calls `/api/users` via `HttpClient`

### 2.4 Blazor Component — Admin User List
In `UserGroupSiteSonnet46.Client/Components/Pages/Admin/`:
- `UserList.razor` + `UserList.razor.cs` with `@rendermode InteractiveWebAssembly`
- Route: `/admin/users`
- Table of all users showing name, email, and toggle checkboxes for Admin/Speaker roles
- Disable the "Admin" checkbox for the currently logged-in user (prevent self-demotion)
- Require `[Authorize(Roles = "Admin")]`

### 2.5 Navigation Update
Add "Admin" dropdown or nav section visible only to users in the `"Admin"` role, linking to `/admin/users`.

---

## Phase 3: Event Management

### 3.1 Shared — DTOs and Service Interface
In `UserGroupSiteSonnet46.Shared`:
- `EventDto` record: all Event fields + `List<UserDto> Speakers`
- `EventSaveDto` record: all editable fields + `List<int> SpeakerIds`
- `IEventService` interface:
  - `Task<List<EventDto>> GetPublishedEventsAsync()` — for home page
  - `Task<List<EventDto>> GetAllEventsAsync()` — for admin/speaker list
  - `Task<EventDto?> GetEventBySlugAsync(string slug)`
  - `Task<EventDto?> GetEventByIdAsync(int id)`
  - `Task<EventDto> SaveEventAsync(EventSaveDto dto)` — create or update
  - `Task<List<UserDto>> GetSpeakersAsync()` — users with Speaker role, for assignment dropdown

### 3.2 Server — Implementation and API Endpoints
In `UserGroupSiteSonnet46.Server`:
- `ServerEventService` using `ApplicationDbContext` directly
- Authorization: `GetAllEventsAsync` and `SaveEventAsync` require caller to be Admin **or** an assigned speaker on the event
- Minimal API group at `/api/events`:
  - `GET /api/events/published` → public
  - `GET /api/events` → require Auth
  - `GET /api/events/{id:int}` → require Auth
  - `GET /api/events/by-slug/{slug}` → public
  - `POST /api/events` → require Auth (Admin or speaker)
  - `PUT /api/events/{id:int}` → require Auth (Admin or speaker)
  - `GET /api/events/speakers` → require Auth (Admin)

### 3.3 Client — HTTP Implementation
In `UserGroupSiteSonnet46.Client`:
- `ClientEventService` calling the above endpoints

### 3.4 Blazor Components — Event Create/Edit
In `UserGroupSiteSonnet46.Client/Components/Pages/Events/`:

**`EventEdit.razor` + `EventEdit.razor.cs`**
- Route: `/events/new` and `/events/{id:int}/edit`
- `@rendermode InteractiveWebAssembly`
- Form fields (all wrapped in Bootstrap `form-floating`):
  - Title input — on `@onblur` if Slug is empty, populate Slug with kebab-cased Title
  - Slug input — editable, auto-populated from Title blur if blank
  - Short Description textarea
  - Description — tabbed component with **Edit** and **Preview** tabs; Edit shows `<InputTextArea>`, Preview renders the Markdown as HTML using a Markdown library (e.g., Markdig)
  - Date/Time picker (separate date and time inputs, combine to `DateTimeOffset`)
  - Location input
  - Speaker multi-select (checkboxes from `GetSpeakersAsync()`)
  - Published checkbox
- Validation rules enforced client-side with `DataAnnotationsValidator`:
  - Title and Slug required to save at all
  - When IsPublished: Description, EventDateTime, Location, and at least one Speaker required
- Require `[Authorize]`; server enforces Admin-or-speaker authorization

**`EventList.razor` + `EventList.razor.cs`** (Admin/Speaker dashboard)
- Route: `/events`
- Shows all events (published and draft) to authorized users
- Table with Title, Date, Published status, Edit link
- Require `[Authorize]`

### 3.5 Markdown Rendering
Add `Markdig` NuGet package to `UserGroupSiteSonnet46.Client`. Create a `MarkdownRenderer` helper that converts Markdown string to sanitized HTML and renders via `MarkupString`.

### 3.6 Navigation Update
Add "Events" link (visible to all authenticated users) and "New Event" link (visible to Admins) in the nav.

---

## Phase 4: Home Page — Published Events

### 4.1 Home Page Component
In `UserGroupSiteSonnet46.Server/Components/Pages/Home.razor` + `Home.razor.cs`:
- Use `IEventService.GetPublishedEventsAsync()` with server pre-rendering (no InteractiveWebAssembly — this page is public, SSR is fine)
- Display events sorted descending by `EventDateTime`
- Each event card shows: Title, Short Description, Date, Location, Speaker names
- "Read more" link to event detail page at `/events/{slug}`

### 4.2 Event Detail Page (Public)
In `UserGroupSiteSonnet46.Server/Components/Pages/Events/EventDetail.razor`:
- Route: `/events/{slug}`
- SSR, publicly accessible
- Renders full Description as Markdown → HTML (Markdig on server side too)
- Shows all speakers, date/time, location
- If logged-in user is an Admin or assigned speaker, show an "Edit" button

---

## Phase 5: Topic Suggestions

### 5.1 Shared — DTOs and Service Interface
In `UserGroupSiteSonnet46.Shared`:
- `TopicSuggestionDto` record: `Id`, `Title`, `Description`, `SuggestedBy` (UserDto), `VoteCount`, `HasCurrentUserVoted`, `Volunteer` (UserDto?)
- `ITopicService` interface:
  - `Task<List<TopicSuggestionDto>> GetTopicsAsync()`
  - `Task<TopicSuggestionDto> SuggestTopicAsync(string title, string? description)`
  - `Task VoteAsync(int topicId)`
  - `Task VolunteerAsync(int topicId)`

### 5.2 Server — Implementation and API Endpoints
In `UserGroupSiteSonnet46.Server`:
- `ServerTopicService` using `ApplicationDbContext`
- `GetTopicsAsync` projects `HasCurrentUserVoted` based on current user from `IUserService`
- Minimal API group at `/api/topics` (all require Auth):
  - `GET /api/topics`
  - `POST /api/topics` → body `{ title, description }`
  - `POST /api/topics/{id}/vote` — idempotent, no-op if already voted
  - `POST /api/topics/{id}/volunteer` — 400 if topic already has a volunteer

### 5.3 Client — HTTP Implementation
In `UserGroupSiteSonnet46.Client`:
- `ClientTopicService` calling the above endpoints

### 5.4 Blazor Components — Topic Suggestions
In `UserGroupSiteSonnet46.Client/Components/Pages/Topics/`:

**`TopicList.razor` + `TopicList.razor.cs`**
- Route: `/topics`
- `@rendermode InteractiveWebAssembly`
- Require `[Authorize]`
- Lists all topic suggestions with: Title, Description, Suggested by, Vote count, Volunteer name (or "No volunteer yet")
- Vote button: highlighted if current user already voted, disabled after voting
- Volunteer button: shown only if `Volunteer` is null; clicking calls `VolunteerAsync`
- "Suggest a Topic" button opens inline form or navigates to `/topics/new`

**`TopicNew.razor` + `TopicNew.razor.cs`**
- Route: `/topics/new`
- `@rendermode InteractiveWebAssembly`
- Require `[Authorize]`
- Form: Title (required), Description (optional)
- On submit, calls `SuggestTopicAsync`, then redirects to `/topics`

### 5.5 Navigation Update
Add "Topics" nav link visible to authenticated users.

---

## Phase 6: Polish and Cross-Cutting Concerns

### 6.1 Role Seeding
On application startup in `Program.cs`, ensure `"Admin"` and `"Speaker"` roles exist in the database (call `RoleManager<Role>.CreateAsync` if not found).

### 6.2 Authorization Policies
Register named policies in `Program.cs`:
- `"AdminOnly"` → requires `"Admin"` role
- `"AuthenticatedUser"` → requires authenticated user

### 6.3 Toast Notifications
Use the existing `ToastService` to show success/error toasts after:
- User role changes
- Event saves
- Topic suggestions submitted
- Votes and volunteer sign-ups

### 6.4 Form Validation Summary
Ensure all edit forms display a `<ValidationSummary>` component and individual `<ValidationMessage>` per field.

### 6.5 Not Found / Unauthorized Handling
- Redirect to `/not-found` for unknown event slugs
- Show friendly message for unauthorized access attempts (use existing `RedirectToLogin` component pattern)

---

## Step Execution Order

```
Phase 1 → Phase 2 → Phase 4 (home page, no auth needed) →
Phase 3 (events) → Phase 5 (topics) → Phase 6 (polish)
```

Phase 2 (user management) should be done early so speaker role assignment is available when building event speaker selection in Phase 3. The home page (Phase 4) can be built as soon as Phase 1 data models and event service are ready, since it only reads published events.
