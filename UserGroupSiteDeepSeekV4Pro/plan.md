# Technical User Group Meeting Site — Implementation Plan

## Project Overview
Build a website for managing technical user group meetings using the existing .NET 10 Blazor Web App scaffold (Identity, EF Core, Aspire, Bootstrap).

---

## Phase 1: Data Models & Database

### 1.1 Event Model
Create `Event.cs` in `src/UserGroupSiteDeepSeekV4Pro.Data/Models/`
- Inherits from `FingerPrintEntityBase` (gets Id, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
- `Title` (string, required, max 200)
- `Slug` (string, required, max 200, unique)
- `ShortDescription` (string, max 500)
- `Description` (string, nvarchar(max), stores Markdown)
- `EventDateTime` (DateTime, required when published)
- `Location` (string, max 300, required when published)
- `IsPublished` (bool, default false)
- Navigation property: `ICollection<EventSpeaker> EventSpeakers`

### 1.2 EventSpeaker Join Model
Create `EventSpeaker.cs`
- Inherits from `FingerPrintEntityBase`
- `EventId` (int, FK → Event)
- `UserId` (int, FK → User)
- Navigation properties: `Event`, `User`

### 1.3 TopicSuggestion Model
Create `TopicSuggestion.cs`
- Inherits from `FingerPrintEntityBase`
- `Title` (string, required, max 200)
- `Description` (string, max 1000)
- `SuggestedById` (int, FK → User)
- Navigation: `SuggestedBy` (User)
- Navigation: `ICollection<TopicVote> Votes`
- Navigation: `ICollection<TopicVolunteer> Volunteers`

### 1.4 TopicVote Model
Create `TopicVote.cs`
- Inherits from `FingerPrintEntityBase`
- `TopicSuggestionId` (int, FK → TopicSuggestion)
- `UserId` (int, FK → User)
- Unique constraint on `(TopicSuggestionId, UserId)` — one vote per user per topic

### 1.5 TopicVolunteer Model
Create `TopicVolunteer.cs`
- Inherits from `FingerPrintEntityBase`
- `TopicSuggestionId` (int, FK → TopicSuggestion)
- `UserId` (int, FK → User)
- Unique constraint: at most one volunteer per topic

### 1.6 Update ApplicationDbContext
- Add `DbSet<Event> Events`
- Add `DbSet<EventSpeaker> EventSpeakers`
- Add `DbSet<TopicSuggestion> TopicSuggestions`
- Add `DbSet<TopicVote> TopicVotes`
- Add `DbSet<TopicVolunteer> TopicVolunteers`
- Configure relationships, indexes, and unique constraints in `OnModelCreating`

### 1.7 EF Migration
- Run `dotnet ef migrations add AddEventsAndTopics` to scaffold the database changes

---

## Phase 2: Shared DTOs & Services

### 2.1 Event DTOs
Create `src/UserGroupSiteDeepSeekV4Pro.Shared/Models/`
- `EventDto` — for listing/presentation (Id, Title, Slug, ShortDescription, Description, EventDateTime, Location, IsPublished, SpeakerNames)
- `EventEditDto` — for create/edit form binding (Title, Slug, ShortDescription, Description, EventDateTime, Location, IsPublished, SpeakerUserIds)

### 2.2 TopicSuggestion DTOs
- `TopicSuggestionDto` — (Id, Title, Description, SuggestedByName, VoteCount, HasVolunteer, VolunteerName, CurrentUserHasVoted)
- `TopicSuggestionEditDto` — (Title, Description)

---

## Phase 3: Server-Side Services & API Endpoints

### 3.1 Event Service
Create `src/UserGroupSiteDeepSeekV4Pro.Server/Services/EventService.cs`
- `GetPublishedEventsAsync()` — returns published events ordered by date descending
- `GetEventByIdAsync(int id)` — returns single event with speakers
- `GetEventBySlugAsync(string slug)`
- `CreateEventAsync(EventEditDto dto)`
- `UpdateEventAsync(int id, EventEditDto dto)`
- `DeleteEventAsync(int id)`
- `GetAllEventsAsync()` — for admin listing (published + unpublished)
- Validation: title and slug required; if published, description, date, location, and ≥1 speaker required
- Auto-generate slug (kebab-case from title) when slug is empty

### 3.2 Topic Suggestion Service
Create `TopicSuggestionService.cs`
- `GetSuggestionsAsync(int? currentUserId)` — all suggestions with vote counts, volunteer info, and whether current user voted
- `CreateSuggestionAsync(TopicSuggestionEditDto dto, int userId)`
- `VoteAsync(int topicId, int userId)` — toggle-style or one-time vote; prevent duplicate
- `VolunteerAsync(int topicId, int userId)` — set user as volunteer; prevent if already has volunteer
- `RemoveVolunteerAsync(int topicId, int userId)`

### 3.3 User Management Service
Create `UserManagementService.cs`
- `GetUsersAsync()` — list all users with roles
- `SetUserRolesAsync(int userId, List<string> roles)` — assign Admin/Speaker roles
- Enforce: admin cannot remove own Admin role

### 3.4 Minimal API Endpoints
Create `src/UserGroupSiteDeepSeekV4Pro.Server/Endpoints/`
- `EventEndpoints.cs` — CRUD for events (authorize Admin/Speaker for create/edit, public for read)
- `TopicEndpoints.cs` — CRUD + vote/volunteer actions (authorize authenticated users)
- `UserManagementEndpoints.cs` — user list + role management (authorize Admin only)

---

## Phase 4: Razor UI Pages

### 4.1 Home Page (Public)
Update `Home.razor`
- Query published events via API on component initialization
- Render as cards/listing in descending date order
- Show: Title, Date, Location, ShortDescription, Speakers
- Link to event detail page

### 4.2 Event Detail Page
Create `EventDetail.razor` at `src/UserGroupSiteDeepSeekV4Pro.Server/Components/Pages/Events/`
- Route: `/events/{slug}`
- Render Markdown Description to HTML (use a Markdown library like Markdig)
- Show all event fields: title, date, location, speakers, full description
- If current user is Admin or assigned Speaker, show "Edit" button

### 4.3 Event Create/Edit Page
Create `EventEdit.razor`
- Route: `/admin/events/create` and `/admin/events/{id:int}/edit`
- Authorize: Admin or assigned Speaker
- **Title field** with `@onblur` handler that calls JS interop to auto-fill slug as kebab-case when slug is empty
- **Slug field** — editable but auto-populated on title blur
- **Short Description** — textarea
- **Description** — tabbed editor with "Edit" tab (textarea for Markdown) and "Preview" tab (live-rendered HTML using Markdig in a client-side WASM component)
- **Date/Time picker** — `<input type="datetime-local">`
- **Location** — text input
- **Speakers** — multi-select dropdown (search users, select one or more)
- **Published toggle** — checkbox
- **Save button** — validates: title & slug required always; if published, description/date/location/≥1 speaker required
- Toast notifications on success/error

### 4.4 Admin Event List Page
Create `EventList.razor` at `/admin/events`
- Table showing all events (published + unpublished)
- Columns: Title, Date, Published, Speakers
- Edit and Delete actions per row

### 4.5 Topic Suggestions Page
Create `TopicSuggestions.razor` at `/topics`
- Accessible to authenticated users
- List all suggestions with: title, description, suggested by, vote count
- **Vote button** — increments vote; disabled if user already voted (or toggle)
- **Volunteer button** — "I can present this!"; hidden if topic already has volunteer
- **Suggest a topic** button → opens form/modal
- Create topic form: title + description fields

### 4.6 User Management Page (Admin)
Create `UserManagement.razor` at `/admin/users`
- Authorize: Admin only
- Table of all users: Name, Email, MemberSince, Roles
- Edit roles: checkboxes/multi-select for Admin and Speaker
- **Guard**: if current admin tries to remove their own Admin role, show error toast and prevent
- Save button per user or bulk save

### 4.7 Nav Menu Updates
Update `NavMenu.razor`
- Add conditional nav items:
  - **Admin dropdown** (visible to Admins): "Manage Events", "Manage Users"
  - **Topics** link (visible to authenticated users)
- Existing "Welcome, FirstName" dropdown already works — keep as-is with Manage Account + Logout

---

## Phase 5: Authorization & Role Seeding

### 5.1 Role Constants
Create `src/UserGroupSiteDeepSeekV4Pro.Shared/Constants/Roles.cs`
```csharp
public static class Roles
{
    public const string Admin = "Admin";
    public const string Speaker = "Speaker";
}
```

### 5.2 Authorization Policies
In `Program.cs`:
- Add policy `"AdminOnly"` → requires Admin role
- Add policy `"EventEditor"` → requires Admin OR Speaker role

### 5.3 Role Seeding
Create a database seed mechanism (or migration seed) that creates the Admin and Speaker roles if they don't exist.

---

## Phase 6: Markdown Rendering (Client-Side WASM)

### 6.1 Add Markdig Package
Add `Markdig` NuGet package to the Client project for WASM-side rendering.

### 6.2 MarkdownPreview Component
Create a WASM interactive component at `src/UserGroupSiteDeepSeekV4Pro.Client/Components/MarkdownPreview.razor`
- Takes `Markdown` string parameter
- Renders as sanitized HTML
- Used in the Description preview tab (live preview without save)

---

## Phase 7: Polish & Testing

### 7.1 Validation & Error Handling
- Client-side validation on forms (DataAnnotations + Blazor form validation)
- Server-side validation in services (return errors, not exceptions)
- Toast notifications for all user actions (success, error, warning)

### 7.2 Responsive Layout
- Ensure all pages work on mobile (Bootstrap responsive classes)
- Event cards stack vertically on small screens

### 7.3 Edge Cases
- Slug uniqueness — check on save, append suffix if duplicate
- Published event unpublish — relax validation (allow saving unpublished without full required fields)
- Concurrent vote handling — unique constraint at DB level prevents duplicates

---

## Execution Order

| Step | Phase | Description |
|------|-------|-------------|
| 1 | 1.1–1.5 | Create all data models |
| 2 | 1.6 | Update ApplicationDbContext |
| 3 | 1.7 | Create & apply EF migration |
| 4 | 5.1 | Add role constants |
| 5 | 5.2 | Add authorization policies |
| 6 | 5.3 | Seed roles |
| 7 | 2.1–2.2 | Create shared DTOs |
| 8 | 3.1 | Build EventService |
| 9 | 3.4 | Build Event API endpoints |
| 10 | 3.2 | Build TopicSuggestionService |
| 11 | 3.4 | Build Topic API endpoints |
| 12 | 3.3 | Build UserManagementService |
| 13 | 3.4 | Build User Management API endpoints |
| 14 | 6.1–6.2 | Add Markdig & MarkdownPreview component |
| 15 | 4.1 | Update Home page |
| 16 | 4.2 | Build Event Detail page |
| 17 | 4.3 | Build Event Create/Edit page |
| 18 | 4.4 | Build Admin Event List page |
| 19 | 4.5 | Build Topic Suggestions page |
| 20 | 4.6 | Build User Management page |
| 21 | 4.7 | Update NavMenu |
| 22 | 7.1–7.3 | Validation, polish, edge cases |
