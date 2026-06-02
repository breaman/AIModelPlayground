# User Group Site — Implementation Plan

## Overview

This plan covers building a technical user group meeting management website on the existing Blazor WebAssembly project. The project already has ASP.NET Core Identity (with roles), EF Core with SQL Server, Aspire orchestration, audit logging, toast notifications, and a `CustomUserClaimsPrincipalFactory` that adds a `FirstName` claim.

---

## Phase 1: Data Models & Database

### 1.1 Create Event Model

**File:** `src/UserGroupSiteGlm51.Data/Models/Event.cs`

- Inherit from `FingerPrintEntityBase` (gets `Id`, `CreatedOn`, `CreatedBy`, `ModifiedOn`, `ModifiedBy` for audit).
- Fields:
  - `string Title` (required, max 200)
  - `string Slug` (required, max 200) — unique index
  - `string? ShortDescription` (max 500)
  - `string? Description` — Markdown content
  - `DateTimeOffset? EventDate` — date/time of the event (stored as `DateTimeOffset` for timezone awareness)
  - `string? Location` (max 500)
  - `bool IsPublished` — defaults to `false`

### 1.2 Create EventSpeaker Join Model

**File:** `src/UserGroupSiteGlm51.Data/Models/EventSpeaker.cs`

- Inherit from `EntityBase` (just `Id` — no fingerprint needed for a join table).
- Fields:
  - `int EventId` — FK to `Event`
  - `int UserId` — FK to `User` (speakers are `User` entities with the Speaker role)
  - Unique composite index on `(EventId, UserId)`

### 1.3 Create TopicSuggestion Model

**File:** `src/UserGroupSiteGlm51.Data/Models/TopicSuggestion.cs`

- Inherit from `FingerPrintEntityBase`.
- Fields:
  - `string Title` (required, max 200)
  - `string? Description` — Markdown content explaining the topic
  - `int SuggestedById` — FK to `User` (who suggested it)
  - `int? VolunteerId` — FK to `User` (nullable; the user who volunteered to present, null means no volunteer yet)
  - Navigation: `User SuggestedBy`, `User? Volunteer`

### 1.4 Create TopicVote Model

**File:** `src/UserGroupSiteGlm51.Data/Models/TopicVote.cs`

- Inherit from `EntityBase`.
- Fields:
  - `int TopicSuggestionId` — FK to `TopicSuggestion`
  - `int UserId` — FK to `User`
  - Unique composite index on `(TopicSuggestionId, UserId)` — one vote per user per topic

### 1.5 Update ApplicationDbContext

**File:** `src/UserGroupSiteGlm51.Data/Models/ApplicationDbContext.cs`

- Add `DbSet<Event> Events`
- Add `DbSet<EventSpeaker> EventSpeakers`
- Add `DbSet<TopicSuggestion> TopicSuggestions`
- Add `DbSet<TopicVote> TopicVotes`
- Override `OnModelCreating` to configure:
  - `Event.Slug` unique index
  - `EventSpeaker` composite key on `(EventId, UserId)`
  - `TopicVote` composite key on `(TopicSuggestionId, UserId)`
  - `TopicSuggestion.VolunteerId` optional relationship to `User`
  - Relationships and cascade delete rules

### 1.6 Create & Run EF Migration

- Run `dotnet ef migrations add AddEventsAndTopics -p ../UserGroupSiteGlm51.Data` from the Server project directory
- Verify the migration creates the expected tables and indexes
- Aspire will auto-apply on startup

---

## Phase 2: Server-Side Services & API Endpoints

### 2.1 Event Service Interface & Implementation

**File:** `src/UserGroupSiteGlm51.Data/Interfaces/IEventService.cs`

```csharp
public interface IEventService
{
    Task<IEnumerable<Event>> GetPublishedEventsAsync();           // Home page
    Task<Event?> GetEventBySlugAsync(string slug);               // Event detail
    Task<Event> CreateEventAsync(Event eventEntity);              // Admin create
    Task<Event> UpdateEventAsync(Event eventEntity);              // Admin/Speaker edit
    Task DeleteEventAsync(int eventId);                           // Admin delete
    Task AddSpeakerToEventAsync(int eventId, int userId);         // Assign speaker
    Task RemoveSpeakerFromEventAsync(int eventId, int userId);   // Remove speaker
    Task<bool> IsEditorAsync(int eventId, int userId);           // Admin or assigned speaker
    Task<bool> CanPublishAsync(Event eventEntity);                // Validation check
}
```

**File:** `src/UserGroupSiteGlm51.Server/Services/EventService.cs`

- Inject `ApplicationDbContext`, `IUserService`
- Implement all methods using EF Core
- `IsEditorAsync`: returns true if the user is in the Admin role OR is an assigned speaker for the event
- `CanPublishAsync`: returns true if Title, Slug, Description, EventDate, Location, and at least one speaker are all populated
- Auto-generate Slug from Title (kebab-case) when Slug is empty on create

### 2.2 Topic Suggestion Service Interface & Implementation

**File:** `src/UserGroupSiteGlm51.Data/Interfaces/ITopicSuggestionService.cs`

```csharp
public interface ITopicSuggestionService
{
    Task<IEnumerable<TopicSuggestion>> GetAllTopicsAsync();
    Task<TopicSuggestion> CreateTopicAsync(TopicSuggestion topic);
    Task VoteAsync(int topicSuggestionId, int userId);
    Task UnvoteAsync(int topicSuggestionId, int userId);
    Task VolunteerAsync(int topicSuggestionId, int userId);
    Task UnvolunteerAsync(int topicSuggestionId, int userId);
    Task<bool> HasVotedAsync(int topicSuggestionId, int userId);
    Task<TopicSuggestion?> GetTopicByIdAsync(int id);
}
```

**File:** `src/UserGroupSiteGlm51.Server/Services/TopicSuggestionService.cs`

- Inject `ApplicationDbContext`, `IUserService`
- `VoteAsync`: add a `TopicVote` record (enforce one-vote-per-user constraint)
- `UnvoteAsync`: remove the `TopicVote` record
- `VolunteerAsync`: set `VolunteerId` only if it's currently null (first-come-first-served)
- `UnvolunteerAsync`: clear `VolunteerId` only if the current user is the volunteer

### 2.3 User Management Service Interface & Implementation

**File:** `src/UserGroupSiteGlm51.Data/Interfaces/IUserManagementService.cs`

```csharp
public interface IUserManagementService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int userId);
    Task<IList<string>> GetUserRolesAsync(int userId);
    Task AddToRoleAsync(int userId, string role);
    Task RemoveFromRoleAsync(int userId, string role);
    Task<bool> IsLastAdminAsync(int userId);          // True if removing this user from Admin would leave no admins
}
```

**File:** `src/UserGroupSiteGlm51.Server/Services/UserManagementService.cs`

- Inject `UserManager<User>`, `RoleManager<Role>`
- `IsLastAdminAsync`: query the Admin role members; return true if this user is the only one
- Enforce: cannot remove yourself from Admin role (checked at the service level)

### 2.4 Minimal API Endpoints

**File:** `src/UserGroupSiteGlm51.Server/Endpoints/EventEndpoints.cs`

Register in `Program.cs` with `MapGroup("/api/events")`:
- `GET /api/events` → GetPublishedEvents (public, no auth required)
- `GET /api/events/{slug}` → GetEventBySlug (public)
- `POST /api/events` → CreateEvent (Admin only)
- `PUT /api/events/{id}` → UpdateEvent (Admin or assigned Speaker for the event)
- `DELETE /api/events/{id}` → DeleteEvent (Admin only)
- `POST /api/events/{id}/speakers` → AddSpeakerToEvent (Admin only)
- `DELETE /api/events/{id}/speakers/{userId}` → RemoveSpeakerFromEvent (Admin only)

**File:** `src/UserGroupSiteGlm51.Server/Endpoints/TopicEndpoints.cs`

Register in `MapGroup("/api/topics")`:
- `GET /api/topics` → GetAllTopics (authenticated)
- `POST /api/topics` → CreateTopic (authenticated)
- `POST /api/topics/{id}/vote` → Vote (authenticated)
- `DELETE /api/topics/{id}/vote` → Unvote (authenticated)
- `POST /api/topics/{id}/volunteer` → Volunteer (authenticated, only if no volunteer yet)
- `DELETE /api/topics/{id}/volunteer` → Unvolunteer (authenticated, only if current user is the volunteer)

**File:** `src/UserGroupSiteGlm51.Server/Endpoints/UserManagementEndpoints.cs`

Register in `MapGroup("/api/users")`:
- `GET /api/users` → GetAllUsers (Admin only)
- `GET /api/users/{id}` → GetUserById (Admin only)
- `GET /api/users/{id}/roles` → GetUserRoles (Admin only)
- `POST /api/users/{id}/roles/{role}` → AddToRole (Admin only)
- `DELETE /api/users/{id}/roles/{role}` → RemoveFromRole (Admin only; prevent self-removal from Admin; prevent removing last Admin)

### 2.5 Register Services in Program.cs

**File:** `src/UserGroupSiteGlm51.Server/Program.cs`

- Register `IEventService` → `EventService`
- Register `ITopicSuggestionService` → `TopicSuggestionService`
- Register `IUserManagementService` → `UserManagementService`
- Call `MapEventEndpoints()`, `MapTopicEndpoints()`, `MapUserManagementEndpoints()` on `app`
- Seed the `Admin` and `Speaker` roles if they don't exist (in an `Initialize` method or similar)

### 2.6 Shared DTOs

**File:** `src/UserGroupSiteGlm51.Shared/Models/` (new directory)

Create request/response DTOs for the API endpoints so the client project can reference them:
- `EventDto`, `CreateEventRequest`, `UpdateEventRequest`
- `TopicSuggestionDto`, `CreateTopicRequest`
- `UserDto`, `UserRoleDto`

---

## Phase 3: Client-Side Services

### 3.1 HttpClient Service Implementations

**File:** `src/UserGroupSiteGlm51.Client/Services/EventService.cs`

- Implements `IEventService` by calling the minimal API endpoints via `HttpClient`
- Handles auth tokens automatically (Blazor WASM `HttpClient` with `BaseAddress`)

**File:** `src/UserGroupSiteGlm51.Client/Services/TopicSuggestionService.cs`

- Implements `ITopicSuggestionService` via `HttpClient`

**File:** `src/UserGroupSiteGlm51.Client/Services/UserManagementService.cs`

- Implements `IUserManagementService` via `HttpClient`

### 3.2 Client Service Registration

**File:** `src/UserGroupSiteGlm51.Client/Program.cs`

- Register `IEventService` → `EventService`
- Register `ITopicSuggestionService` → `TopicSuggestionService`
- Register `IUserManagementService` → `UserManagementService`
- Register `HttpClient` (already registered, confirm auth handler setup)

---

## Phase 4: UI — Home Page (Public Event Listing)

### 4.1 Home Page Redesign

**File:** `src/UserGroupSiteGlm51.Server/Components/Pages/Home.razor` + `Home.razor.cs`

- Replace the current placeholder content
- On `OnInitializedAsync`, call `IEventService.GetPublishedEventsAsync()`
- Display events in descending order by `EventDate` (newest first)
- Each event card shows: Title, ShortDescription, EventDate (formatted nicely), Location
- Title links to `/events/{slug}` for detail view
- This page is visible to anonymous users (no `[Authorize]` attribute)

### 4.2 Event Detail Page

**File:** `src/UserGroupSiteGlm51.Server/Components/Pages/EventDetail.razor` + `EventDetail.razor.cs`

- Route: `/events/{slug}`
- Publicly visible (no auth required)
- Shows: Title, Description (rendered from Markdown to HTML), EventDate, Location, Speaker(s) names
- If the current user is an editor (Admin or assigned speaker), show an "Edit" link

---

## Phase 5: UI — Header / Navigation Updates

### 5.1 Update NavMenu

**File:** `src/UserGroupSiteGlm51.Server/Components/Layout/NavMenu.razor` + `NavMenu.razor.cs`

- Already has "Welcome, {FirstName}" dropdown for authenticated users — verify this is working
- Add navigation items:
  - **Home** (always visible)
  - **Topic Suggestions** (visible to authenticated users)
  - **Admin** (visible to Admin role only):
    - Manage Events
    - Manage Users
  - Dropdown menu items for logged-in users:
    - Manage Account (existing)
    - Logout (existing)

---

## Phase 6: UI — Event Management (Admin)

### 6.1 Event List Page (Admin)

**File:** `src/UserGroupSiteGlm51.Server/Components/Pages/Admin/EventList.razor` + `EventList.razor.cs`

- Route: `/admin/events`
- `[Authorize(Roles = "Admin")]`
- Table listing all events (published and unpublished)
- Columns: Title, Slug, EventDate, Location, Published status, Actions (Edit/Delete)
- "Create New Event" button at the top

### 6.2 Event Create/Edit Page (Admin / Assigned Speaker)

**File:** `src/UserGroupSiteGlm51.Server/Components/Pages/EventEdit.razor` + `EventEdit.razor.cs`

- Route: `/admin/events/new` and `/admin/events/{id}/edit`
- Authorization: Admin or assigned Speaker for the event
- Form fields:
  - **Title** — text input, required
  - **Slug** — text input, auto-populated from Title on blur (if Slug is empty). Implement kebab-case conversion in JavaScript interop or Blazor
  - **Short Description** — text input, max 500 chars
  - **Description** — **Markdown editor with Edit/Preview tabs**:
    - Edit tab: `<textarea>` with monospace font
    - Preview tab: renders Markdown to HTML using a client-side Markdown library (e.g., Markdig on the server or a JS library on the client)
  - **Date/Time** — `<input type="datetime-local">`
  - **Location** — text input
  - **Speakers** — multi-select or tag picker showing users in the Speaker role
  - **Is Published** — checkbox
- Validation:
  - Title and Slug are required to save (any state)
  - If "Is Published" is checked, validate that Description, Date/Time, Location, and at least one Speaker are populated before saving — show validation errors inline
- On save: call `IEventService.CreateEventAsync` or `IEventSvice.UpdateEventAsync`
- Redirect to event list on success

### 6.3 Markdown Preview Component

**File:** `src/UserGroupSiteGlm51.Client/Components/MarkdownEditor.razor` + `MarkdownEditor.razor.cs`

- A reusable Blazor component with two tabs: "Edit" and "Preview"
- Edit tab: `<textarea>` bound to the Markdown string value
- Preview tab: renders the Markdown as HTML
- Implementation approach:
  - Use **Markdig** (already a .NET library) for server-side Markdown → HTML conversion
  - Create a minimal API endpoint `POST /api/markdown/preview` that accepts Markdown text and returns rendered HTML
  - The preview tab calls this endpoint to render the Markdown
  - Alternative: embed a lightweight JS-based Markdown renderer (like `marked`) for client-side preview without a server round-trip — evaluate trade-offs and pick one

---

## Phase 7: UI — Topic Suggestions

### 7.1 Topic Suggestions List Page

**File:** `src/UserGroupSiteGlm51.Server/Components/Pages/Topics.razor` + `Topics.razor.cs`

- Route: `/topics`
- `[Authorize]` — must be logged in
- List all topic suggestions
- Each topic shows: Title, Description (rendered Markdown), SuggestedBy name, Vote count, Current volunteer name (or "No volunteer yet")
- Actions per topic (if logged in):
  - **Vote** / **Unvote** toggle button (highlight if current user has voted)
  - **Volunteer** button (disabled/hidden if someone already volunteered)
  - **Unvolunteer** button (only visible if current user is the volunteer)

### 7.2 Create Topic Suggestion Page

**File:** `src/UserGroupSiteGlm51.Server/Components/Pages/TopicCreate.razor` + `TopicCreate.razor.cs`

- Route: `/topics/new`
- `[Authorize]`
- Form with: Title (required), Description (optional, Markdown editor)
- On save: call `ITopicSuggestionService.CreateTopicAsync`
- Redirect to topics list on success

---

## Phase 8: UI — User Management (Admin)

### 8.1 User Management Page

**File:** `src/UserGroupSiteGlm51.Server/Components/Pages/Admin/UserList.razor` + `UserList.razor.cs`

- Route: `/admin/users`
- `[Authorize(Roles = "Admin")]`
- Table listing all registered users
- Columns: Name, Email, Roles (badges), Actions
- Actions per user:
  - **Add to Admin role** button (if not already Admin)
  - **Remove from Admin role** button (disabled with tooltip "Cannot remove yourself from Admin" if the target is the current user, or "Cannot remove the last Admin" if they're the only Admin)
  - **Add to Speaker role** button (if not already a Speaker)
  - **Remove from Speaker role** button (if currently a Speaker)

---

## Phase 9: Seed Data & Role Initialization

### 9.1 Role Seeding

**File:** `src/UserGroupSiteGlm51.Server/Data/SeedData.cs` (new)

- On application startup (or via a migration/endpoint), ensure the `Admin` and `Speaker` roles exist in the database
- Optionally seed a default Admin user for development/testing

### 9.2 Update Program.cs for Seeding

**File:** `src/UserGroupSiteGlm51.Server/Program.cs`

- After Identity setup, call a seeding method that creates `Admin` and `Speaker` roles if they don't exist
- Create a default admin user if in Development mode (via `appsettings.Development.json` config)

---

## Phase 10: Integration, Testing & Polish

### 10.1 End-to-End Testing

- Verify anonymous users can see the home page with published events
- Verify authenticated users can suggest topics, vote, and volunteer
- Verify admins can create/edit/delete events and manage users
- Verify admins cannot remove themselves from the Admin role
- Verify published events must have all required fields
- Verify slug auto-generation from title

### 10.2 UI Polish

- Consistent Bootstrap styling across all pages
- Responsive layout (mobile-friendly)
- Loading states for async operations
- Error handling and toast notifications for success/failure
- Empty states (no events, no topics, etc.)

### 10.3 Validation & Error Handling

- Server-side validation on all API endpoints (DataAnnotations + manual checks)
- Client-side validation in Blazor forms
- Authorization checks on all protected endpoints and pages
- Graceful error messages via toast notifications

---

## Implementation Order

The recommended order of implementation, accounting for dependencies:

1. **Phase 1** — Data models & migration (foundation for everything)
2. **Phase 2.5–2.6** — Shared DTOs & service interfaces (contracts first)
3. **Phase 2.1–2.4** — Server-side service implementations & API endpoints
4. **Phase 2.7 + 3** — Service registration in Program.cs & client service implementations
5. **Phase 9** — Role seeding (needed before testing role-based features)
6. **Phase 4** — Home page (public, foundational UI)
7. **Phase 5** — Nav menu updates (navigation to new pages)
8. **Phase 6** — Event management (core feature)
9. **Phase 7** — Topic suggestions (secondary feature)
10. **Phase 8** — User management (admin feature)
11. **Phase 10** — Testing & polish

---

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Render mode | Interactive WebAssembly only | Per project `.claude/rules/blazor.instructions.md` |
| Code-behind | `.razor.cs` files, no `@code` blocks | Per project rules |
| CSS framework | Bootstrap with SCSS | Already in the project |
| Markdown rendering | Markdig (server-side) + API endpoint for preview | .NET-native, no additional JS dependencies; preview tab calls a small API endpoint |
| Auth model | ASP.NET Core Identity with roles (already in place) | Leverages existing setup with `User`, `Role`, `ClaimsPrincipalFactory` |
| API style | Minimal API endpoints | Clean, lightweight, follows .NET modern patterns |
| Audit trail | Automatic via `FingerPrintEntityBase` | Already built into the `AuthDbContext` save-changes pipeline |
| Slug generation | Kebab-case from Title, generated on blur | UX-friendly; user can override if desired |
| Topic volunteering | First-come-first-served (one volunteer per topic) | Per requirements — "doesn't already have a volunteer" |