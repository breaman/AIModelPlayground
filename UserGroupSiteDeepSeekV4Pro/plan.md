# User Group Meeting Management Site — Implementation Plan

## Architecture Overview

The project is a **Blazor Web App** (.NET 10, C# 14) with four projects:
- **Server** — ASP.NET Core host, Razor pages for pre-rendering, Minimal API endpoints, Identity/auth
- **Client** — Blazor WebAssembly interactive components (`@rendermode InteractiveWebAssembly`)
- **Data** — EF Core models, `ApplicationDbContext`, migrations
- **Shared** — Service interfaces, DTOs, constants shared between Server and Client

All interactive components follow the **dual-mode service pattern**: an interface in Shared, a server implementation (calls DB directly for prerendering), and a client implementation (calls HTTP API). Components go in the Client project.

---

## Phase 1: Data Model & Database

### 1.1 Roles & Seed Data

The project already has `User` and `Role` classes with ASP.NET Identity. Add three roles:
- **Admin** — full site management
- **Speaker** — can be assigned to events, can edit their own events
- **User** — default role for self-registered users

Add a `DataSeeder` class that runs on startup to ensure these roles exist and optionally create an initial Admin user.

### 1.2 New Entity Models (Data project)

#### `Event`
```csharp
public class Event : FingerPrintEntityBase
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [Required, MaxLength(200)] public string Slug { get; set; } = "";
    [MaxLength(500)]  public string? ShortDescription { get; set; }
    public string? Description { get; set; }  // Markdown content
    public DateTimeOffset EventDateTime { get; set; }
    [MaxLength(500)]  public string? Location { get; set; }
    public bool IsPublished { get; set; }

    // Navigation
    public ICollection<EventSpeaker> EventSpeakers { get; set; } = [];
}
```

#### `EventSpeaker` (join table)
```csharp
public class EventSpeaker
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
```

#### `TopicSuggestion`
```csharp
public class TopicSuggestion : FingerPrintEntityBase
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }

    // Navigation
    public int? SuggestedByUserId { get; set; }
    public User? SuggestedByUser { get; set; }
    public int? VolunteerUserId { get; set; }
    public User? VolunteerUser { get; set; }
    public ICollection<TopicVote> Votes { get; set; } = [];
}
```

#### `TopicVote`
```csharp
public class TopicVote
{
    public int TopicSuggestionId { get; set; }
    public TopicSuggestion TopicSuggestion { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
```

### 1.3 Update `ApplicationDbContext`

Add `DbSet<>` properties for `Events`, `EventSpeakers`, `TopicSuggestions`, `TopicVotes`. Configure composite keys for join tables in `OnModelCreating`.

### 1.4 EF Migration

Create and apply the initial migration for these new entities.

---

## Phase 2: Shared Contracts & DTOs (Shared project)

### 2.1 DTOs

Create DTOs that are safe to serialize between Server and Client:

- `EventDto` — mirrors Event model, includes `List<int> SpeakerUserIds`
- `EventListItemDto` — lighter DTO for list displays
- `TopicSuggestionDto`
- `TopicSuggestionListItemDto`
- `UserDto` — for user management (Id, FirstName, LastName, Email, Roles)

### 2.2 Service Interfaces

```csharp
public interface IEventService
{
    Task<List<EventListItemDto>> GetPublishedEventsAsync();
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<EventDto?> GetEventBySlugAsync(string slug);
    Task<EventDto> CreateEventAsync(EventDto dto);
    Task<EventDto> UpdateEventAsync(EventDto dto);
    Task DeleteEventAsync(int id);
    Task<List<UserDto>> GetAvailableSpeakersAsync();
}

public interface ITopicSuggestionService
{
    Task<List<TopicSuggestionListItemDto>> GetAllAsync();
    Task<TopicSuggestionDto> CreateAsync(TopicSuggestionDto dto);
    Task VoteAsync(int topicId);
    Task RemoveVoteAsync(int topicId);
    Task VolunteerAsync(int topicId);
    Task RemoveVolunteerAsync(int topicId);
}

public interface IUserManagementService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task UpdateUserRolesAsync(int userId, List<string> roles);
}
```

### 2.3 Constants

- `RoleNames.Admin`, `RoleNames.Speaker`
- `Policies.CanManageUsers`, `Policies.CanManageEvents`

---

## Phase 3: API Endpoints (Server project)

All endpoints go in `Server/Endpoints/` as extension methods mapped in `Program.cs`.

### 3.1 Event API (`/api/events`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/events/published` | Anonymous | List published events (desc date) |
| GET | `/api/events/{id}` | Anonymous (if published) / Editor | Get event detail |
| GET | `/api/events/slug/{slug}` | Anonymous (if published) / Editor | Get event by slug |
| POST | `/api/events` | Admin | Create event |
| PUT | `/api/events/{id}` | Admin or assigned Speaker | Update event |
| DELETE | `/api/events/{id}` | Admin | Delete event |
| GET | `/api/events/speakers/available` | Admin | List users with Speaker role |

### 3.2 Topic Suggestion API (`/api/topics`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/topics` | Authenticated | List all topic suggestions with vote counts |
| POST | `/api/topics` | Authenticated | Create a new topic suggestion |
| POST | `/api/topics/{id}/vote` | Authenticated | Vote for a topic (toggle) |
| POST | `/api/topics/{id}/volunteer` | Authenticated | Volunteer to speak (toggle) |

### 3.3 User Management API (`/api/users`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/users` | Admin | List all users with roles |
| PUT | `/api/users/{id}/roles` | Admin | Update user roles |

Add `MapApiEndpoints()` extension method on `IEndpointRouteBuilder` and call it from `Program.cs`.

---

## Phase 4: Server-Side Service Implementations (Server project)

Implement each service interface directly against EF Core (used during pre-rendering):

- `ServerEventService` — queries `ApplicationDbContext` directly
- `ServerTopicSuggestionService` — queries `ApplicationDbContext` directly
- `ServerUserManagementService` — uses `UserManager<User>` and `RoleManager<Role>`

Register in `Server/Program.cs`:
```csharp
builder.Services.AddScoped<IEventService, ServerEventService>();
builder.Services.AddScoped<ITopicSuggestionService, ServerTopicSuggestionService>();
builder.Services.AddScoped<IUserManagementService, ServerUserManagementService>();
```

---

## Phase 5: Client-Side Service Implementations (Client project)

Implement each service interface using `HttpClient` to call the API:

- `ClientEventService` — calls `/api/events/*`
- `ClientTopicSuggestionService` — calls `/api/topics/*`
- `ClientUserManagementService` — calls `/api/users/*`

Register in `Client/Program.cs`:
```csharp
builder.Services.AddScoped<IEventService, ClientEventService>();
builder.Services.AddScoped<ITopicSuggestionService, ClientTopicSuggestionService>();
builder.Services.AddScoped<IUserManagementService, ClientUserManagementService>();
```

---

## Phase 6: Authorization Policies (Server project)

Add policies in `Program.cs`:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanManageUsers, policy =>
        policy.RequireRole(RoleNames.Admin));
    options.AddPolicy(Policies.CanManageEvents, policy =>
        policy.RequireRole(RoleNames.Admin, RoleNames.Speaker));
});
```

Apply `[Authorize]` attributes on API endpoints and `AuthorizeView` / `AuthorizeRouteView` on pages.

---

## Phase 7: UI Components & Pages

### 7.1 Server-Side Pages (pre-rendered, static)

**Home Page** (`Server/Components/Pages/Home.razor`):
- Shows list of published events in descending date order
- Each event card shows title, date, location, short description, speakers
- Visible to anonymous users
- Mark the page with `@attribute [AllowAnonymous]` in the code-behind

### 7.2 Client-Side Interactive Components (Client project)

All use `@rendermode InteractiveWebAssembly` and follow the `[PersistentState]` pre-rendering pattern.

#### Event Pages (`Client/Components/Pages/Events/`)
- **EventList.razor** — Admin view: table of all events (published + unpublished) with edit/delete actions
- **EventEdit.razor** — Create/Edit form (Admin or assigned Speaker):
  - Title field with `onblur` auto-generate slug via JS interop (kebab-case)
  - Slug field (editable, required)
  - Short Description
  - Description with **Edit/Preview tabs** for Markdown editing (use a simple textarea + Markdown renderer)
  - Date/Time picker
  - Location field
  - Speaker multi-select (searchable dropdown of Speaker-role users)
  - IsPublished checkbox
  - Validation:
    - Title + Slug always required
    - If IsPublished: Description, Date/Time, Location, and ≥1 speaker required
- **EventDetail.razor** — Public event detail page with rendered Markdown description

#### Topic Suggestion Pages (`Client/Components/Pages/Topics/`)
- **TopicList.razor** — List of all suggestions with:
  - Vote count + vote button (toggle, one vote per user per topic)
  - Volunteer button (if no volunteer assigned) / Volunteer name (if assigned)
  - Suggest new topic button → modal or inline form
- **TopicSuggest.razor** — Form to submit a new topic suggestion (Title, Description)

#### User Management Pages (`Client/Components/Pages/Admin/`)
- **UserManagement.razor** — Admin-only:
  - Table of all users (name, email, roles)
  - Role toggles: Admin / Speaker checkboxes per user
  - **Cannot remove own Admin role** (disable checkbox for current user)

### 7.3 Navigation Updates

Update `Server/Components/Layout/NavMenu.razor`:
- Add "Events" link (visible to all)
- Add "Topic Suggestions" link (visible when authenticated)
- Add "Manage Events" link (visible to Admin/Speaker)
- Add "Manage Users" link (visible to Admin only)

### 7.4 Markdown Support

Include a JavaScript Markdown library (e.g., `marked.js`) via CDN in `App.razor` for client-side preview rendering. The Description field stores raw Markdown; the preview tab renders it via JS interop.

---

## Phase 8: Validation & Business Logic Details

### Event Save Validation
- Title and Slug are **always required**
- If `IsPublished == true`:
  - Description must not be null/empty
  - EventDateTime must be set (not default)
  - Location must not be null/empty
  - At least one speaker must be assigned
- These validations run on both client (form validation) and server (API validation)

### Slug Auto-Generation
- When Title field loses focus and Slug is empty, generate kebab-case slug via JS interop
- Slug must be unique — validate on save, append `-2`, `-3` etc. if duplicate

### Editor Authorization for Events
- **Admin** can edit any event
- **Speaker** can edit events where they are assigned as a speaker
- Server API checks this before allowing PUT

### Topic Suggestion Rules
- Any authenticated user can suggest a topic
- Any authenticated user can vote (toggle — add/remove their vote)
- Any authenticated user can volunteer if no one has volunteered yet
- User can remove their own volunteer
- One vote per user per topic (enforced by unique constraint + API check)

### User Management Guard
- Admin cannot remove their own Admin role via the UI (checkbox disabled)
- Server API also enforces this — if the target user is the current user and Admin role is being removed, return error

---

## Phase 9: Testing

### 9.1 Integration Tests
- Event CRUD API endpoints (auth + validation)
- Topic suggestion + voting + volunteering flows
- User role management (including self-admin guard)

### 9.2 Unit Tests
- Server service implementations
- Slug generation logic
- Event publish validation rules

---

## Phase 10: Final Polish

- Ensure all forms use Bootstrap `form-floating` pattern
- Use Bootstrap Icons consistently
- Add toast notifications for success/error actions (existing `ToastService`)
- Add loading indicators for async operations
- Ensure responsive layout works on mobile
- Verify the site works with the Aspire orchestration (`dotnet watch` from AppHost)
