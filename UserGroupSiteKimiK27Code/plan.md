# Implementation Plan: Technical User Group Meeting Management Site

## Project Context

This is a .NET 10 Blazor WebAssembly + Server application using:

- ASP.NET Core Identity with roles (`User`, `Role`)
- Entity Framework Core with SQL Server
- .NET Aspire for orchestration
- Bootstrap 5 + Bootstrap Icons for styling
- WebAssembly-first interactive components (`InteractiveWebAssembly` only; no `InteractiveServer`)
- Shared service contracts with dual client/server implementations

The existing codebase already provides:

- User registration/login with first/last name capture
- A `NavMenu` that shows "Welcome, {FirstName}" and a logout dropdown
- `ToastService`/`ToastContainer` for client notifications
- Audit logging and fingerprinting on `ApplicationDbContext`

This plan adds **user management**, **events**, and **topic suggestions**.

---

## Architecture Decisions

1. **Authorization**: Use ASP.NET Identity roles `Admin` and `Speaker`. Add an `Admin` authorization policy requiring the `Admin` role. Add a custom policy/claim-based check for event editing (`Admin` OR assigned speaker).
2. **Data Access**: Add new EF entities directly to `ApplicationDbContext`. Use EF migrations for schema changes.
3. **Interactive Components**: Follow the existing dual-mode service pattern:
   - Define service interfaces in `UserGroupSiteKimiK27Code.Shared`
   - Implement server-side in `UserGroupSiteKimiK27Code.Server` (direct DB access or minimal APIs)
   - Implement client-side in `UserGroupSiteKimiK27Code.Client` (HTTP calls to backend APIs)
   - Use `[PersistentState]` for pre-rendered data in `InteractiveWebAssembly` components
4. **Markdown Rendering**: Add the `Markdig` package and render markdown to HTML on the server for previews and public display.
5. **Slug Generation**: Use a small custom helper to convert the title to kebab-case; populate on title blur when slug is empty.
6. **Validation**: Use DataAnnotations for simple rules and custom validation logic for "publish" rules.
7. **API Style**: Use Minimal APIs grouped under `/api/...` for client-facing data operations to keep the Server project lightweight.

---

## Assumptions / Open Questions

- The first administrator will be created via a database seed step (or by promoting an existing user from the UI once at least one admin exists). The plan seeds a default admin user/role during migration.
- Speaker assignment for events is performed by admins in the event editor. Topic volunteering is a separate flow where any user can volunteer for an unclaimed suggestion.
- A public event detail page is included so visitors can read the full description.
- Slugs must be unique at the database level.
- Markdown preview uses server-side rendering and is shown without saving.

---

## Phase 1: Foundation & Domain Models

**Goal**: Extend the data model and persistence layer to support events, speakers, topic suggestions, and votes.

1. Add NuGet package references:
   - `Markdig` (Server + Data projects)
   - `Humanizer` (optional, for kebab-case / slug generation)
2. Create new entities in `UserGroupSiteKimiK27Code.Data/Models`:
   - `Event` (`FingerPrintEntityBase`)
     - `Title` (required, max 200)
     - `Slug` (required, max 200, unique index)
     - `ShortDescription` (max 500)
     - `Description` (markdown content)
     - `EventDate` (DateTime, UTC)
     - `Location` (max 200)
     - `IsPublished` (bool)
   - `EventSpeaker` (join entity for many-to-many `Event` <-> `User`)
   - `TopicSuggestion` (`FingerPrintEntityBase`)
     - `Title` (required)
     - `Description`
     - `SuggestedById` (FK to `User`)
     - `VolunteerSpeakerId` (nullable FK to `User`)
   - `TopicVote` (`EntityBase`)
     - `TopicSuggestionId`
     - `UserId`
     - Composite unique index on `(TopicSuggestionId, UserId)`
3. Add `DbSet<T>` properties to `ApplicationDbContext`.
4. Configure relationships and indexes in `OnModelCreating` overrides if needed.
5. Create and apply an EF migration:
   - `dotnet ef migrations add AddEventsAndTopics -p src/UserGroupSiteKimiK27Code.Data`
6. Seed default roles (`Admin`, `Speaker`) and a default admin user in a migration or `ApplicationDbContext` seed method.

---

## Phase 2: Authorization & User Management

**Goal**: Allow admins to manage user roles while preventing self-demotion.

1. Define role-name constants in `UserGroupSiteKimiK27Code.Shared`:
   - `Roles.Admin`
   - `Roles.Speaker`
2. In `Server/Program.cs`:
   - Add `AddAuthorization` with an `Admin` policy (`RequireRole(Roles.Admin)`).
   - Add a custom authorization handler/policy for "event editor" checks.
3. Create `UserGroupSiteKimiK27Code.Server/Components/Pages/Admin/Users.razor` and `.razor.cs`:
   - Requires `Admin` policy.
   - Lists all users with checkboxes for `Admin` and `Speaker`.
   - Disables the `Admin` checkbox for the currently logged-in admin user and shows a tooltip explaining why.
   - Saves changes via `UserManager` and shows toast/status feedback.
4. Add a link to the user management page in `NavMenu` visible only to admins.
5. Add an authorization helper service `IAuthorizationService` / extension method to check whether the current user can edit a specific event.

---

## Phase 3: Event Backend

**Goal**: Provide server-side business logic and HTTP APIs for events.

1. Create shared DTOs in `UserGroupSiteKimiK27Code.Shared`:
   - `EventListItemDto`
   - `EventDetailDto`
   - `EventEditDto`
   - `SpeakerDto`
2. Create shared service interface `IEventService`:
   - `GetPublishedEventsAsync()`
   - `GetEventForEditAsync(int id)`
   - `CreateEventAsync(EventEditDto dto)`
   - `UpdateEventAsync(int id, EventEditDto dto)`
   - `GetSpeakersAsync()`
3. Implement `ServerEventService` in the Server project:
   - Reads/writes `ApplicationDbContext` directly.
   - Enforces publish validation rules before saving a published event.
   - Converts markdown to HTML for storage or returns raw markdown for editing.
4. Register `IEventService` in `Server/Program.cs`.
5. Create Minimal API endpoints under `/api/events`:
   - `GET /api/events/published` - public, returns published event list ordered descending by date.
   - `GET /api/events/{id}` - returns event detail; admin/speaker version returns unpublished data for editors.
   - `POST /api/events` - admin only.
   - `PUT /api/events/{id}` - admin OR assigned speaker.
   - `GET /api/speakers` - returns users in `Speaker` role.
6. Implement `ClientEventService` in the Client project that calls these endpoints via `HttpClient`.
7. Register `IEventService` in `Client/Program.cs`.

---

## Phase 4: Event Frontend

**Goal**: Build public and admin/speaker-facing event pages.

1. **Home page** (`Server/Components/Pages/Home.razor` + `.razor.cs`):
   - Publicly visible (no authorization required).
   - Lists published events in descending order by event date.
   - Each item shows title, short description, date/time, location.
   - Links to public event detail page.
2. **Event detail page** (`Server/Components/Pages/Events/EventDetail.razor`):
   - Publicly visible for published events.
   - Renders markdown description as HTML.
3. **Event admin list** (`Client/Components/Pages/Admin/Events.razor`):
   - `@rendermode InteractiveWebAssembly`
   - Shows all events with published status; links to edit/create.
   - Requires `Admin` or `Speaker` role.
4. **Event editor** (`Client/Components/Pages/Admin/EventEdit.razor` + `.razor.cs`):
   - `@rendermode InteractiveWebAssembly`
   - Title input with `onblur` handler that generates kebab-case slug if slug is empty.
   - Slug input that can be manually overridden.
   - Short description textarea.
   - Markdown editor with **Edit** and **Preview** tabs:
     - Edit tab: textarea for markdown.
     - Preview tab: calls server API to render markdown to HTML (no save).
   - Date/time picker (`InputDate` + time portion).
   - Location input.
   - Speaker multi-select (list of users in `Speaker` role).
   - Published checkbox.
   - Validation:
     - Title and slug required for save.
     - Description, date/time, location, and at least one speaker required when publishing.
     - Slug uniqueness checked server-side.
5. Create a reusable `MarkdownEditor.razor` component in the Client project for the edit/preview tabs.
6. Add Minimal API endpoint `/api/markdown/preview` to render markdown to sanitized HTML.
7. Add navigation links to `NavMenu` for admins and speakers.

---

## Phase 5: Topic Suggestions

**Goal**: Allow authenticated users to suggest, vote on, and volunteer for topics.

1. Create shared DTOs in `UserGroupSiteKimiK27Code.Shared`:
   - `TopicSuggestionDto`
   - `TopicVoteDto`
2. Create shared service interface `ITopicSuggestionService`:
   - `GetSuggestionsAsync()`
   - `CreateSuggestionAsync(...)`
   - `VoteAsync(int topicId)`
   - `VolunteerAsync(int topicId)`
3. Implement `ServerTopicSuggestionService` and Minimal API endpoints under `/api/topics`:
   - `GET /api/topics` - authenticated, includes vote count, current user's vote flag, and volunteer info.
   - `POST /api/topics` - authenticated.
   - `POST /api/topics/{id}/vote` - authenticated, one vote per user per topic.
   - `POST /api/topics/{id}/volunteer` - authenticated, only if no volunteer yet.
4. Implement `ClientTopicSuggestionService` in the Client project.
5. Create `Client/Components/Pages/Topics/TopicSuggestions.razor`:
   - `@rendermode InteractiveWebAssembly`
   - Lists suggestions with vote counts and up-vote button.
   - Shows volunteer button only for unclaimed suggestions.
   - Form to submit a new suggestion.
6. Register `ITopicSuggestionService` in both `Program.cs` files.

---

## Phase 6: UI Polish & Navigation

**Goal**: Tie the features together with consistent navigation and styling.

1. Update `NavMenu.razor`:
   - Keep "Welcome, {FirstName}" dropdown with logout.
   - Add admin-only links: Users, All Events.
   - Add links for speakers/admins: Event Editor / My Events.
   - Add authenticated link: Topic Suggestions.
2. Ensure the home page is reachable without authentication.
3. Add Bootstrap styling and `form-floating` wrappers for all new form inputs per project conventions.
4. Add empty-state messages and loading spinners for interactive WebAssembly pages.
5. Add toast notifications for save/vote/volunteer success and failure.

---

## Phase 7: Testing & Validation

**Goal**: Verify correctness, security, and UX.

1. **Unit tests** (xUnit or NUnit in a new or existing test project):
   - Slug generation helper
   - Publish validation logic
   - Unique vote constraint handling
2. **Integration tests**:
   - Create/update events as admin
   - Edit event as assigned speaker but not as unassigned speaker
   - Voting once per user
   - Volunteering for unclaimed topics
   - Anonymous access to published home page
3. **Manual validation checklist**:
   - Admin cannot remove own admin role
   - Slug auto-generates on title blur
   - Markdown preview works without save
   - Published event requires all mandatory fields
   - Logout dropdown works on all pages
4. Run `dotnet format` and verify builds for Server, Client, Data, and Aspire projects.

---

## Order of Implementation

1. Phase 1: Foundation & Domain Models
2. Phase 2: Authorization & User Management
3. Phase 3: Event Backend
4. Phase 4: Event Frontend
5. Phase 5: Topic Suggestions
6. Phase 6: UI Polish & Navigation
7. Phase 7: Testing & Validation

---

## Deliverables

- `plan.md` (this document)
- EF migration for events, speakers, topic suggestions, and votes
- `Admin` and `Speaker` role management UI
- Public home page with published events
- Event editor with markdown preview and slug generation
- Topic suggestion list with voting and volunteering
- Updated navigation and styling
- Unit and integration tests for critical paths
