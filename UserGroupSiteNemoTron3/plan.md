# Implementation Plan: Technical User Group Meeting Management Website

## Context
Building a website to manage technical user group meetings with user management, event management, topic suggestions, and a public-facing home page. The project is an existing Blazor WebAssembly application with ASP.NET Core backend using Entity Framework Core and ASP.NET Core Identity.

**Current Stack:**
- **Server**: ASP.NET Core 10 with Razor Components (InteractiveWebAssembly render mode)
- **Client**: Blazor WebAssembly
- **Shared**: Service interfaces
- **Data**: EF Core with SQL Server, ASP.NET Core Identity (User/Role with int keys)
- **UI**: Bootstrap 5, Bootstrap Icons
- **Auth**: Cookie-based authentication with custom claims (FirstName)
- **Audit**: Automatic audit logging with finger printing (CreatedBy, ModifiedBy, etc.)

---

## Phase 1: Data Models & Database (Foundation)

### 1.1 Extend User Model & Roles
**Files to create/modify:**
- `src/UserGroupSiteNemoTron3.Data/Models/User.cs` - Add `IsAdmin`, `IsSpeaker` properties
- `src/UserGroupSiteNemoTron3.Data/Models/Role.cs` - Keep as-is (using Identity roles for Admin/Speaker)

**Approach:** Use ASP.NET Core Identity roles ("Admin", "Speaker") rather than boolean flags. This aligns with existing `CustomUserClaimsPrincipalFactory` pattern and enables role-based authorization.

### 1.2 Create Event Entity
**File:** `src/UserGroupSiteNemoTron3.Data/Models/Event.cs`
```csharp
public class Event : FingerPrintEntityBase
{
    [Required, MaxLength(200)]
    public string Title { get; set; }
    
    [Required, MaxLength(200)]
    public string Slug { get; set; }
    
    [MaxLength(500)]
    public string? ShortDescription { get; set; }
    
    public string? Description { get; set; } // Markdown content
    
    public DateTime EventDateTime { get; set; }
    
    [MaxLength(300)]
    public string? Location { get; set; }
    
    public bool IsPublished { get; set; }
    
    // Navigation
    public ICollection<EventSpeaker> EventSpeakers { get; set; } = new List<EventSpeaker>();
}
```

### 1.3 Create EventSpeaker Join Entity
**File:** `src/UserGroupSiteNemoTron3.Data/Models/EventSpeaker.cs`
```csharp
public class EventSpeaker : EntityBase
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public bool IsPrimary { get; set; } // For display ordering
}
```

### 1.4 Create TopicSuggestion Entity
**File:** `src/UserGroupSiteNemoTron3.Data/Models/TopicSuggestion.cs`
```csharp
public class TopicSuggestion : FingerPrintEntityBase
{
    [Required, MaxLength(300)]
    public string Title { get; set; }
    
    public string? Description { get; set; }
    
    public int? VolunteerSpeakerId { get; set; }
    public User? VolunteerSpeaker { get; set; }
    
    public ICollection<TopicVote> Votes { get; set; } = new List<TopicVote>();
}
```

### 1.5 Create TopicVote Entity
**File:** `src/UserGroupSiteNemoTron3.Data/Models/TopicVote.cs`
```csharp
public class TopicVote : EntityBase
{
    public int TopicSuggestionId { get; set; }
    public TopicSuggestion TopicSuggestion { get; set; } = null!;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime VotedOn { get; set; } = DateTime.UtcNow;
}
```

### 1.6 Update ApplicationDbContext
**File:** `src/UserGroupSiteNemoTron3.Data/Models/ApplicationDbContext.cs`
- Add DbSets for `Event`, `EventSpeaker`, `TopicSuggestion`, `TopicVote`
- Configure relationships and indexes in `OnModelCreating`
- Add unique constraint on `Event.Slug`
- Add unique constraint on `TopicVote` (TopicSuggestionId, UserId) to prevent duplicate votes

### 1.7 Create & Apply Migration
```bash
dotnet ef migrations add AddEventAndTopicModels --project src/UserGroupSiteNemoTron3.Data --startup-project src/UserGroupSiteNemoTron3.Server
dotnet ef database update --project src/UserGroupSiteNemoTron3.Data --startup-project src/UserGroupSiteNemoTron3.Server
```

---

## Phase 2: Shared Service Contracts

### 2.1 Create IEventService
**File:** `src/UserGroupSiteNemoTron3.Shared/Services/IEventService.cs`
```csharp
public interface IEventService
{
    Task<EventDto[]> GetPublishedEventsAsync();
    Task<EventDto?> GetEventBySlugAsync(string slug);
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<EventDto> CreateEventAsync(CreateEventDto dto);
    Task<EventDto> UpdateEventAsync(int id, UpdateEventDto dto);
    Task DeleteEventAsync(int id);
    Task<bool> CanUserEditEventAsync(int eventId, int userId);
    Task<string> GenerateSlugAsync(string title);
}
```

### 2.2 Create ITopicSuggestionService
**File:** `src/UserGroupSiteNemoTron3.Shared/Services/ITopicSuggestionService.cs`
```csharp
public interface ITopicSuggestionService
{
    Task<TopicSuggestionDto[]> GetAllAsync();
    Task<TopicSuggestionDto?> GetByIdAsync(int id);
    Task<TopicSuggestionDto> CreateAsync(CreateTopicSuggestionDto dto, int userId);
    Task VoteAsync(int topicId, int userId);
    Task RemoveVoteAsync(int topicId, int userId);
    Task VolunteerAsync(int topicId, int userId);
    Task<bool> HasUserVotedAsync(int topicId, int userId);
}
```

### 2.3 Create IUserManagementService
**File:** `src/UserGroupSiteNemoTron3.Shared/Services/IUserManagementService.cs`
```csharp
public interface IUserManagementService
{
    Task<UserDto[]> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> UpdateUserRolesAsync(int userId, UpdateUserRolesDto dto, int currentUserId);
    Task<bool> IsCurrentUserAdminAsync(int currentUserId);
}
```

### 2.4 Create DTOs in Shared Project
**Files:**
- `src/UserGroupSiteNemoTron3.Shared/DTOs/EventDtos.cs`
- `src/UserGroupSiteNemoTron3.Shared/DTOs/TopicSuggestionDtos.cs`
- `src/UserGroupSiteNemoTron3.Shared/DTOs/UserDtos.cs`

---

## Phase 3: Server-Side Implementation

### 3.1 Server Event Service
**File:** `src/UserGroupSiteNemoTron3.Server/Services/EventService.cs`
- Implement `IEventService` using `ApplicationDbContext`
- Auto-generate slug from title (kebab-case) on create/update
- Validation: Title + Slug required for save; Published requires Description, DateTime, Location, ≥1 Speaker
- Authorization: Only Admin or assigned Speaker can edit

### 3.2 Server Topic Suggestion Service
**File:** `src/UserGroupSiteNemoTron3.Server/Services/TopicSuggestionService.cs`
- Implement `ITopicSuggestionService`
- Prevent duplicate votes (unique constraint + check)
- Only allow volunteering if no volunteer exists

### 3.3 Server User Management Service
**File:** `src/UserGroupSiteNemoTron3.Server/Services/UserManagementService.cs`
- Implement `IUserManagementService`
- Admin cannot remove themselves from Admin role
- Use `UserManager<User>` and `RoleManager<Role>` for role assignments

### 3.4 Register Services in Server/Program.cs
```csharp
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITopicSuggestionService, TopicSuggestionService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
```

### 3.5 Create API Endpoints (Minimal APIs)
**Files:**
- `src/UserGroupSiteNemoTron3.Server/Endpoints/EventEndpoints.cs`
- `src/UserGroupSiteNemoTron3.Server/Endpoints/TopicSuggestionEndpoints.cs`
- `src/UserGroupSiteNemoTron3.Server/Endpoints/UserManagementEndpoints.cs`

**Route patterns:**
- `GET /api/events` - Published events (public)
- `GET /api/events/{slug}` - Event by slug (public)
- `GET /api/admin/events` - All events (Admin/Speaker)
- `POST /api/admin/events` - Create event (Admin)
- `PUT /api/admin/events/{id}` - Update event (Admin/Speaker)
- `DELETE /api/admin/events/{id}` - Delete event (Admin)
- `GET /api/topics` - All topic suggestions (authenticated)
- `POST /api/topics` - Create topic (authenticated)
- `POST /api/topics/{id}/vote` - Vote (authenticated)
- `DELETE /api/topics/{id}/vote` - Remove vote (authenticated)
- `POST /api/topics/{id}/volunteer` - Volunteer (authenticated)
- `GET /api/admin/users` - All users (Admin)
- `PUT /api/admin/users/{id}/roles` - Update roles (Admin)

---

## Phase 4: Client-Side Implementation

### 4.1 Client Service Implementations
**Files:**
- `src/UserGroupSiteNemoTron3.Client/Services/EventService.cs` - HttpClient implementation
- `src/UserGroupSiteNemoTron3.Client/Services/TopicSuggestionService.cs` - HttpClient implementation
- `src/UserGroupSiteNemoTron3.Client/Services/UserManagementService.cs` - HttpClient implementation

### 4.2 Register Client Services
**File:** `src/UserGroupSiteNemoTron3.Client/Program.cs`
```csharp
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITopicSuggestionService, TopicSuggestionService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
```

### 4.3 Home Page - Published Events List
**Files:**
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Home.razor`
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Home.razor.cs`

**Features:**
- Display published events in descending order by EventDateTime
- Show Title, ShortDescription, Date/Time, Location, Speakers
- Link to event detail page
- Public access (no auth required)

### 4.4 Event Detail Page
**Files:**
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/EventDetail.razor`
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/EventDetail.razor.cs`

**Route:** `/event/{slug}`

**Features:**
- Render Description as Markdown (use Markdig)
- Show full event details
- "Edit" button visible only to editors (Admin or assigned Speaker)

### 4.5 Event Editor Page (Admin/Speaker)
**Files:**
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Admin/EventEditor.razor`
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Admin/EventEditor.razor.cs`

**Route:** `/admin/events/create` and `/admin/events/edit/{id}`

**Features:**
- **Title** (required) + **Slug** (auto-populated on Title blur, editable)
- **Short Description** (optional)
- **Description** - Markdown editor with **Edit/Preview tabs**
- **Date/Time** picker
- **Location** input
- **Speakers** - multi-select from users with Speaker role
- **Published** checkbox
- Validation per requirements
- Save/Create buttons

### 4.6 Markdown Editor Component (Reusable)
**Files:**
- `src/UserGroupSiteNemoTron3.Client/Components/MarkdownEditor.razor`
- `src/UserGroupSiteNemoTron3.Client/Components/MarkdownEditor.razor.cs`
- `src/UserGroupSiteNemoTron3.Client/Components/MarkdownEditor.razor.css`

**Features:**
- Two tabs: "Edit" and "Preview"
- Edit tab: textarea with monospace font
- Preview tab: rendered HTML using Markdig (server-side) or marked.js (client-side)
- `@bind-Value` support for form integration

### 4.7 Admin Event List Page
**Files:**
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Admin/EventList.razor`
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Admin/EventList.razor.cs`

**Route:** `/admin/events`

**Features:**
- Table of all events (published + drafts)
- Columns: Title, Date, Location, Published status, Speakers, Actions
- Create New button
- Edit/Delete actions

### 4.8 Topic Suggestions Page
**Files:**
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Topics.razor`
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Topics.razor.cs`

**Route:** `/topics` (requires authentication)

**Features:**
- List all topic suggestions with vote counts
- "Suggest Topic" modal/form
- Vote button (toggle, shows count, disabled if already voted)
- "Volunteer to Speak" button (only if no volunteer, assigns current user)
- Show volunteer name if assigned

### 4.9 User Management Page (Admin Only)
**Files:**
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Admin/UserManagement.razor`
- `src/UserGroupSiteNemoTron3.Server/Components/Pages/Admin/UserManagement.razor.cs`

**Route:** `/admin/users` (requires Admin role)

**Features:**
- Table of all users
- Columns: Name, Email, Member Since, Admin (checkbox), Speaker (checkbox), Actions
- Checkbox changes trigger role update API
- **Critical:** Disable Admin checkbox for current user (prevent self-removal)
- Show toast on success/error

### 4.10 Update NavMenu for New Pages
**File:** `src/UserGroupSiteNemoTron3.Server/Components/Layout/NavMenu.razor`

**Add authenticated user links:**
- "Topics" → `/topics`
- Admin dropdown (if Admin): "Manage Events" → `/admin/events`, "Manage Users" → `/admin/users`

---

## Phase 5: Authorization & Policy Configuration

### 5.1 Define Policies in Server/Program.cs
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SpeakerOrAdmin", policy => policy.RequireRole("Speaker", "Admin"));
    options.AddPolicy("EventEditor", policy => policy.RequireAssertion(context =>
        // Custom logic: Admin OR assigned speaker for specific event
    ));
});
```

### 5.2 Apply Authorization to API Endpoints
- Use `RequireAuthorization("AdminOnly")` for admin endpoints
- Use custom logic for event editing (check if user is admin or event speaker)

### 5.3 Apply Authorization to Razor Components
- Use `@attribute [Authorize(Policy = "AdminOnly")]` on admin pages
- Use `@attribute [Authorize]` on authenticated pages (Topics)

---

## Phase 6: UI/UX Polish

### 6.1 Markdown Rendering
- **Server-side**: Use Markdig for rendering in EventDetail page (pre-rendered)
- **Client-side**: Use marked.js for live preview in MarkdownEditor component

### 6.2 Responsive Design
- Bootstrap 5 grid system
- Mobile-friendly tables (card layout on mobile)
- Proper form-floating inputs per Blazor instructions

### 6.3 Toast Notifications
- Use existing `IToastService` for success/error feedback
- Show on create/update/delete operations

### 6.4 Loading States
- Skeleton loaders for event lists
- Disabled buttons during API calls

---

## Phase 7: Testing & Verification

### 7.1 Unit Tests
- EventService: slug generation, validation rules, authorization
- TopicSuggestionService: vote duplicate prevention, volunteer logic
- UserManagementService: self-admin removal prevention

### 7.2 Integration Tests
- API endpoint authorization
- Database constraints (unique slug, unique vote)

### 7.3 Manual Verification Checklist
- [ ] User self-registration works
- [ ] Admin can assign Admin/Speaker roles
- [ ] Admin cannot remove own Admin role
- [ ] Event creation: Title + Slug required
- [ ] Slug auto-generates from Title on blur
- [ ] Published event requires all fields + ≥1 speaker
- [ ] Markdown editor shows Edit/Preview tabs
- [ ] Home page shows published events (public)
- [ ] Event detail renders Markdown
- [ ] Only Admin/Speaker can edit event
- [ ] Topic suggestions: create, vote (once), volunteer
- [ ] NavMenu shows "Welcome, FirstName" + logout dropdown
- [ ] Admin sees management links in NavMenu

---

## Critical Files to Create/Modify Summary

### Data Models (Data project)
- `Models/Event.cs` - New
- `Models/EventSpeaker.cs` - New
- `Models/TopicSuggestion.cs` - New
- `Models/TopicVote.cs` - New
- `Models/ApplicationDbContext.cs` - Modify (add DbSets, configure)
- `Models/User.cs` - Modify (ensure roles work)

### Shared Contracts (Shared project)
- `Services/IEventService.cs` - New
- `Services/ITopicSuggestionService.cs` - New
- `Services/IUserManagementService.cs` - New
- `DTOs/EventDtos.cs` - New
- `DTOs/TopicSuggestionDtos.cs` - New
- `DTOs/UserDtos.cs` - New

### Server Implementation (Server project)
- `Services/EventService.cs` - New
- `Services/TopicSuggestionService.cs` - New
- `Services/UserManagementService.cs` - New
- `Endpoints/EventEndpoints.cs` - New
- `Endpoints/TopicSuggestionEndpoints.cs` - New
- `Endpoints/UserManagementEndpoints.cs` - New
- `Program.cs` - Modify (register services, policies)
- `Components/Pages/Home.razor/.cs` - Modify (event list)
- `Components/Pages/EventDetail.razor/.cs` - New
- `Components/Pages/Admin/EventEditor.razor/.cs` - New
- `Components/Pages/Admin/EventList.razor/.cs` - New
- `Components/Pages/Topics.razor/.cs` - New
- `Components/Pages/Admin/UserManagement.razor/.cs` - New
- `Components/Layout/NavMenu.razor/.cs` - Modify

### Client Implementation (Client project)
- `Services/EventService.cs` - New
- `Services/TopicSuggestionService.cs` - New
- `Services/UserManagementService.cs` - New
- `Components/MarkdownEditor.razor/.cs/.css` - New
- `Program.cs` - Modify (register services)

---

## Dependencies to Add

### Server Project (NuGet)
- `Markdig` - For server-side Markdown rendering

### Client Project (npm)
- `marked` - For client-side Markdown preview in editor
- Add to `package.json` and import in MarkdownEditor component

---

## Implementation Order Recommendation

1. **Data Layer** (Phase 1) - Foundation for everything
2. **Shared Contracts** (Phase 2) - Define boundaries
3. **Server Services + API** (Phase 3) - Business logic
4. **Client Services** (Phase 4) - Connect UI to API
5. **UI Components** (Phase 4) - Pages and reusable components
6. **Authorization** (Phase 5) - Secure everything
7. **Polish & Test** (Phase 6-7) - UX and verification

---

## Verification Strategy

1. **Run the application**: `dotnet run --project src/UserGroupSiteNemoTron3.Server`
2. **Test user flows**:
   - Register new user → verify "Welcome, FirstName" appears
   - Login as admin → verify admin links in nav
   - Create event → verify slug auto-generation
   - Publish event without required fields → verify validation
   - View home page → verify published events show
   - View event detail → verify Markdown renders
   - Create topic suggestion → verify appears in list
   - Vote on topic → verify count increments, can't vote twice
   - Volunteer for topic → verify assignment
   - Manage users → verify role checkboxes work, self-admin disabled
3. **Check database**: Verify tables, constraints, audit logs
4. **Run tests**: `dotnet test` (when test project exists)