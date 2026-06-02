# Technical User Group Meeting Management Site — Implementation Plan

## Overview

This plan implements a full-featured website for managing technical user group meetings on top of the existing .NET 10 Blazor WebAssembly project with Aspire, Identity, EF Core, and Bootstrap.

---

## Phase 1: Domain Model & Database Foundation

**Goal:** Establish the core data models, relationships, and database schema for Events, Speakers, and Topic Suggestions.

### Steps

1. **Create `Event` entity**
   - `Id`, `Title` (required), `Slug` (required, unique), `ShortDescription`, `Description`
   - `EventDate` (DateTime), `Location`
   - `IsPublished` (bool)
   - Audit fields (inherit from `FingerPrintEntityBase` or `EntityBase`)

2. **Create `EventSpeaker` join entity**
   - Links `Event` ↔ `User` (many-to-many)
   - `EventId`, `UserId`

3. **Create `TopicSuggestion` entity**
   - `Id`, `Title`, `Description`, `SuggestedByUserId`, `VolunteerUserId` (nullable)
   - `CreatedAt`

4. **Create `TopicVote` entity**
   - `Id`, `TopicSuggestionId`, `UserId`
   - Unique constraint on `(TopicSuggestionId, UserId)` to enforce one vote per user per topic

5. **Update `ApplicationDbContext`**
   - Add `DbSet<Event>`, `DbSet<EventSpeaker>`, `DbSet<TopicSuggestion>`, `DbSet<TopicVote>`
   - Configure relationships, cascade rules, and unique constraints

6. **Create and apply EF Migration**
   - `dotnet ef migrations add AddEventsTopicsSchema -p ../UserGroupSiteKimiK26.Data`

---

## Phase 2: Authorization & Role Infrastructure

**Goal:** Define and enforce Admin and Speaker roles, plus custom authorization policies.

### Steps

1. **Seed default roles at startup**
   - Ensure "Admin" and "Speaker" roles exist in the database on first run

2. **Create custom authorization policies**
   - `RequireAdmin` — user must be in the "Admin" role
   - `RequireSpeaker` — user must be in the "Speaker" role
   - `CanEditEvent` — custom handler: user is Admin OR user is a speaker for the specific event being accessed

3. **Add `IAuthorizationHandler` implementations**
   - `EventEditorAuthorizationHandler` — checks if the current user is an assigned speaker for the event or an Admin

4. **Register policies in `Program.cs`**
   - `builder.Services.AddAuthorization(...)` with the new policies

---

## Phase 3: User Management (Admin-only)

**Goal:** Allow Admins to manage users and assign roles. Admins cannot remove their own Admin role.

### Steps

1. **Create `UsersService` (server-side)**
   - `GetAllUsersAsync()` — list all users with their roles
   - `UpdateUserRolesAsync(int userId, bool isAdmin, bool isSpeaker)` — idempotent role assignment
   - Enforce: if `userId == currentUserId` and `isAdmin == false`, throw `InvalidOperationException` (or return failure)

2. **Create `IUsersService` interface in `.Shared`**

3. **Create server API endpoints**
   - `GET /api/users` — returns list of users + roles
   - `PUT /api/users/{id}/roles` — updates roles
   - Protect both with `[Authorize(Roles = "Admin")]`

4. **Create Blazor page `/admin/users`**
   - Table of users with checkboxes for Admin and Speaker roles
   - Disable the Admin checkbox for the currently logged-in Admin user (or show a warning tooltip)
   - Save changes via the API
   - Use Toast notifications for success/error feedback

5. **Add "User Management" link to NavMenu** (Admin-only)

---

## Phase 4: Event Management — Backend

**Goal:** Build the server-side API for creating, editing, and retrieving events.

### Steps

1. **Create `EventsService` (server-side)**
   - `GetPublishedEventsAsync()` — all published events, ordered by `EventDate` descending
   - `GetEventBySlugAsync(string slug)` — full event details with speakers
   - `GetEventByIdAsync(int id)` — for editing
   - `CreateEventAsync(EventDto dto, int createdByUserId)`
   - `UpdateEventAsync(int id, EventDto dto, int updatedByUserId)`
   - `DeleteEventAsync(int id)`
   - `GetEditableEventsForUserAsync(int userId)` — events where user is Admin or assigned speaker

2. **Create DTOs in `.Shared`**
   - `EventDto`, `EventListItemDto`, `SpeakerDto`

3. **Create API endpoints (`/api/events`)**
   - `GET /api/events` — published list (public, no auth)
   - `GET /api/events/{slug}` — public event detail (public, no auth)
   - `GET /api/events/manage` — editable events for current user (auth)
   - `GET /api/events/manage/{id}` — event for editing (auth, `CanEditEvent` policy)
   - `POST /api/events` — create (Admin only)
   - `PUT /api/events/{id}` — update (Admin or assigned speaker)
   - `DELETE /api/events/{id}` — delete (Admin only)

4. **Validation logic in service layer**
   - Title and Slug are required for any save (create or update)
   - If `IsPublished == true`, additionally require: Description, EventDate, Location, and at least one speaker
   - Return structured validation errors the UI can display

---

## Phase 5: Event Management — Frontend

**Goal:** Build the Blazor UI for creating and editing events with all specified fields and interactions.

### Steps

1. **Create reusable `MarkdownEditor` component**
   - Two tabs: "Edit" (textarea) and "Preview" (rendered Markdown)
   - Use a simple Markdown-to-HTML library or basic parsing (e.g., `Markdig` on server for preview, or a lightweight WASM alternative)
   - Bind `@bind-Value` for two-way binding

2. **Create `EventEdit.razor` page (`/admin/events/new` and `/admin/events/edit/{id}`)**
   - Form fields:
     - Title (text input)
     - Slug (text input; auto-populate with kebab-case of Title on `onblur` if empty)
     - Short Description (textarea)
     - Description (`MarkdownEditor` component)
     - Date/Time (`InputDate` + time picker or combined datetime-local input)
     - Location (text input)
     - Speakers (multi-select dropdown of users in "Speaker" role)
     - IsPublished (checkbox or toggle)
   - Client-side validation mirroring server rules (Title/Slug required; publish requires additional fields)
   - Show validation summary before save

3. **Slug auto-generation (client-side)**
   - On Title field `onblur`, if Slug is empty, convert Title to kebab-case (lowercase, replace spaces/special chars with hyphens)
   - If user manually edits Slug, do not overwrite on subsequent blurs

4. **Create `EventList.razor` admin page (`/admin/events`)**
   - Table of all events (published and draft) editable by the current user
   - Columns: Title, Date, Location, Published status, Actions (Edit, Delete)
   - Delete with confirmation modal

5. **Add "Event Management" link to NavMenu** (visible to Admins and Speakers)

---

## Phase 6: Public Event Pages

**Goal:** Display published events on the home page and individual event detail pages.

### Steps

1. **Redesign `Home.razor`**
   - Fetch published events from `GET /api/events`
   - Display in descending chronological order (newest first)
   - Each event card shows: Title, Short Description, Date/Time, Location, list of speaker names
   - Card links to detail page
   - Page is fully public (no authentication required)

2. **Create `EventDetail.razor` page (`/events/{slug}`)**
   - Fetch event by slug from `GET /api/events/{slug}`
   - Render full Description as Markdown-to-HTML
   - Show Title, Date/Time, Location, Speakers, and any other metadata
   - Public page (no auth required)

3. **Add loading states and empty states**
   - Skeleton or spinner while loading
   - Friendly message if no published events exist yet

---

## Phase 7: Topic Suggestions — Backend

**Goal:** Build the API for suggesting topics, voting, and volunteering.

### Steps

1. **Create `TopicSuggestionsService` (server-side)**
   - `GetAllSuggestionsAsync(int currentUserId)` — list all suggestions; include `HasVoted` flag for current user and `VoteCount`
   - `CreateSuggestionAsync(CreateSuggestionDto dto, int userId)`
   - `VoteAsync(int topicId, int userId)` — toggle vote (vote if not voted, remove vote if already voted)
   - `VolunteerAsync(int topicId, int userId)` — assign `VolunteerUserId` if currently null; throw if already has a volunteer
   - `DeleteSuggestionAsync(int topicId, int userId)` — only allowed by the user who suggested it or an Admin

2. **Create DTOs in `.Shared`**
   - `TopicSuggestionDto`, `CreateSuggestionDto`, `VoteResultDto`

3. **Create API endpoints (`/api/topics`)**
   - `GET /api/topics` — list (auth required to see `HasVoted`)
   - `POST /api/topics` — create (any authenticated user)
   - `POST /api/topics/{id}/vote` — vote/unvote (any authenticated user)
   - `POST /api/topics/{id}/volunteer` — volunteer (any authenticated user)
   - `DELETE /api/topics/{id}` — delete (suggester or Admin)

---

## Phase 8: Topic Suggestions — Frontend

**Goal:** Build the Blazor UI for viewing, suggesting, voting, and volunteering.

### Steps

1. **Create `TopicSuggestions.razor` page (`/topics`)**
   - List all topic suggestions in a card or table layout
   - Each topic shows: Title, Description, Suggested By, Vote Count, current Volunteer (if any)
   - Actions per topic:
     - **Vote button** — highlighted if current user has voted; clicking toggles vote
     - **Volunteer button** — visible only if no volunteer yet; clicking assigns current user
     - **Delete button** — visible only to the suggester or Admins
   - "Suggest a Topic" button at top opens a modal or navigates to a form

2. **Create `SuggestTopicModal` or `SuggestTopic.razor` form**
   - Title and Description fields
   - Submit creates the suggestion
   - Toast confirmation on success

3. **Client-side vote toggle**
   - Immediate UI update (optimistic) with server confirmation
   - Handle race conditions gracefully

4. **Add "Topic Suggestions" link to NavMenu** (visible to all authenticated users)

---

## Phase 9: Header, Navigation & Polish

**Goal:** Finalize the shared UI shell so all requirements around auth visibility are met.

### Steps

1. **Verify `NavMenu.razor` already shows "Welcome, `<firstName>`"**
   - The existing implementation already has this; confirm it survives the build

2. **Verify dropdown menu**
   - "Manage Account" link (existing)
   - "Log out" button (existing)
   - Ensure the dropdown works with Bootstrap JS

3. **Add conditional nav links based on roles**
   - Admin: User Management, Event Management
   - Speaker: Event Management
   - Authenticated: Topic Suggestions
   - Everyone: Home (already exists)

4. **Create `Unauthorized` / `Forbidden` fallback pages**
   - If a non-Admin tries to access `/admin/*`, show a friendly access-denied message

5. **Add global error handling**
   - Use the existing Toast system for API error messages

---

## Phase 10: Integration, Testing & Seed Data

**Goal:** Wire everything together, verify the full user journey, and prepare the app for demo/use.

### Steps

1. **Create `SeedDataService`**
   - On first run (or in a development-only endpoint), seed:
     - One Admin user (e.g., `admin@example.com`)
     - A few Speaker users
     - A few published events with speakers assigned
     - A few topic suggestions with votes and volunteers

2. **Add integration tests (optional but recommended)**
   - Event CRUD flow
   - Topic suggestion → vote → volunteer flow
   - Admin role self-removal prevention
   - Publish validation rules

3. **Manual end-to-end verification checklist**
   - [ ] Unauthenticated user sees home page with published events
   - [ ] User can self-register (already supported)
   - [ ] Admin can promote users to Speaker/Admin
   - [ ] Admin cannot remove their own Admin role
   - [ ] Admin can create an event; slug auto-generates from title
   - [ ] Markdown preview renders correctly in event editor
   - [ ] Event cannot be published without required fields
   - [ ] Assigned speaker can edit their event
   - [ ] Non-speaker/non-admin cannot edit the event
   - [ ] Authenticated user can suggest a topic
   - [ ] User can vote once per topic; toggling works
   - [ ] User can volunteer only on topics without a volunteer
   - [ ] Logged-in user sees "Welcome, `<firstName>`" and can log out

4. **CSS/SCSS polish**
   - Ensure responsive layouts for event cards, topic list, and admin tables
   - Add any custom overrides to `site.scss`

---

## Tech Stack Summary

| Layer | Technology |
|-------|------------|
| Framework | .NET 10, ASP.NET Core, Blazor WebAssembly |
| Database | SQL Server (via Aspire) |
| ORM | Entity Framework Core 10 |
| Auth | ASP.NET Core Identity with Roles |
| UI | Bootstrap 5 (SCSS), Razor Components |
| Markdown | Markdig (server-side preview rendering) |
| API Style | Minimal API / controller-based endpoints |
| Hosting | .NET Aspire (AppHost + ServiceDefaults) |

---

## Estimated Phase Order & Dependencies

```
Phase 1 ──► Phase 2 ──► Phase 3 ──► Phase 4 ──► Phase 5 ──► Phase 6
   │           │                                         │
   └───────────┴─────────────────────────────────────────┘
                           (shared DTOs, auth infra)

Phase 7 ──► Phase 8
   │
   └────────► Phase 9 ──► Phase 10
```

- **Phases 1–2** can be done in parallel after a quick DTO sketch.
- **Phases 3–6** are sequential because they share the Event domain.
- **Phases 7–8** are sequential and mostly independent of Events.
- **Phase 9** (UI shell) can be refined in parallel with Phases 5–8.
- **Phase 10** must come last.

---

## Open Questions / Decisions to Make

1. **Markdown rendering strategy**
   - Option A: Server-side preview endpoint (`POST /api/markdown/preview`) — simplest, guaranteed consistency
   - Option B: Lightweight WASM Markdown parser — faster UX, adds JS/WASM dependency

2. **Slug uniqueness collision handling**
   - If two events have the same title, append `-2`, `-3`, etc. to the slug on save

3. **Speaker assignment UX**
   - Option A: Multi-select dropdown of Speaker-role users
   - Option B: Searchable tag input with auto-complete

4. **Date/Time input UX**
   - Use HTML5 `datetime-local` input bound to `DateTime?`

5. **Event deletion behavior**
   - Hard delete vs. soft delete (`IsDeleted` flag). Recommend soft delete to preserve history.
