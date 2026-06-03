# Technical User Group Meeting Management Website - Implementation Plan

## Project Overview

A Blazor WebAssembly application for managing technical user group meetings with features for user management, event management, and topic suggestions.

---

## Phase 1: Database Models and Entities

### 1.1 Create Domain Models

**Files to Create:**
- `src/UserGroupSiteQwen35.Data/Models/Event.cs`
- `src/UserGroupSiteQwen35.Data/Models/Speaker.cs`
- `src/UserGroupSiteQwen35.Data/Models/TopicSuggestion.cs`
- `src/UserGroupSiteQwen35.Data/Models/TopicVote.cs`
- `src/UserGroupSiteQwen35.Data/Models/EventSpeaker.cs` (join table)

**Event Model:**
```csharp
- Id (int, PK)
- Title (string, required, max 200)
- Slug (string, required, unique, max 200)
- ShortDescription (string, max 500)
- Description (string, markdown support)
- DateTime (DateTime)
- Location (string)
- IsPublished (bool, default false)
- CreatedBy, ModifiedBy, CreatedOn, ModifiedOn (from FingerPrintEntityBase)
```

**Speaker Model:**
```csharp
- Id (int, PK)
- UserId (int, FK to User)
- Bio (string, markdown support)
- IsApproved (bool, default false)
```

**TopicSuggestion Model:**
```csharp
- Id (int, PK)
- Title (string, required)
- Description (string, markdown)
- SuggestedByUserId (int, FK to User)
- VolunteerSpeakerId (int?, FK to Speaker, nullable)
- CreatedOn, CreatedBy
```

**TopicVote Model:**
```csharp
- Id (int, PK)
- TopicSuggestionId (int, FK)
- UserId (int, FK)
- VotedOn (DateTime)
- Unique constraint on (TopicSuggestionId, UserId)
```

**EventSpeaker Join Model:**
```csharp
- EventId (int, FK)
- SpeakerId (int, FK)
- Composite PK (EventId, SpeakerId)
```

### 1.2 Update ApplicationDbContext

**File to Modify:**
- `src/UserGroupSiteQwen35.Data/Models/ApplicationDbContext.cs`

**Add DbSets:**
```csharp
public DbSet<Event> Events => Set<Event>();
public DbSet<Speaker> Speakers => Set<Speaker>();
public DbSet<TopicSuggestion> TopicSuggestions => Set<TopicSuggestion>();
public DbSet<TopicVote> TopicVotes => Set<TopicVote>();
public DbSet<EventSpeaker> EventSpeakers => Set<EventSpeaker>();
```

### 1.3 Create Repository Interfaces and Implementations

**Files to Create:**
- `src/UserGroupSiteQwen35.Data/Interfaces/IEventRepository.cs`
- `src/UserGroupSiteQwen35.Data/Interfaces/ISpeakerRepository.cs`
- `src/UserGroupSiteQwen35.Data/Interfaces/ITopicSuggestionRepository.cs`
- `src/UserGroupSiteQwen35.Data/Repositories/EventRepository.cs`
- `src/UserGroupSiteQwen35.Data/Repositories/SpeakerRepository.cs`
- `src/UserGroupSiteQwen35.Data/Repositories/TopicSuggestionRepository.cs`

---

## Phase 2: User Management

### 2.1 Role Configuration

**File to Create:**
- `src/UserGroupSiteQwen35.Data/Models/RoleNames.cs` (constants)

```csharp
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Speaker = "Speaker";
}
```

### 2.2 User Service Extensions

**Files to Create/Modify:**
- `src/UserGroupSiteQwen35.Server/Services/UserManagementService.cs`
- `src/UserGroupSiteQwen35.Client/Services/IUserManagementClientService.cs`
- `src/UserGroupSiteQwen35.Client/Services/UserManagementClientService.cs`

**Features:**
- Get all users with their roles
- Add/Remove Admin role (with self-prevention check)
- Add/Remove Speaker role
- Check if user is Admin/Speaker/Both

### 2.3 User Management API Endpoints

**File to Create:**
- `src/UserGroupSiteQwen35.Server/Endpoints/UserManagementEndpoints.cs`

**Endpoints:**
- `GET /api/users` - List all users with roles (Admin only)
- `PUT /api/users/{id}/role` - Add/remove role (Admin only)
- `GET /api/users/current` - Get current user info with roles

### 2.4 User Management UI

**Files to Create:**
- `src/UserGroupSiteQwen35.Server/Components/Pages/Admin/UserManagement.razor`
- `src/UserGroupSiteQwen35.Server/Components/Pages/Admin/UserManagement.razor.cs`
- `src/UserGroupSiteQwen35.Server/Components/Pages/Admin/UserManagement.razor.css`

**UI Components:**
- User list table with current roles
- Role assignment dropdowns per user
- Warning when trying to remove own Admin role
- Search/filter functionality

---

## Phase 3: Event Management

### 3.1 Event Service

**Files to Create:**
- `src/UserGroupSiteQwen35.Server/Services/EventService.cs`
- `src/UserGroupSiteQwen35.Client/Services/IEventClientService.cs`
- `src/UserGroupSiteQwen35.Client/Services/EventClientService.cs`

**Features:**
- Get all published events (public)
- Get all events (Admin/Speaker)
- Get event by ID/slug
- Create/Update/Delete events
- Publish/Unpublish events
- Validate event before publishing

### 3.2 Event API Endpoints

**File to Create:**
- `src/UserGroupSiteQwen35.Server/Endpoints/EventEndpoints.cs`

**Endpoints:**
- `GET /api/events` - List published events (public)
- `GET /api/events/all` - List all events (authenticated)
- `GET /api/events/{id}` - Get single event
- `GET /api/events/slug/{slug}` - Get event by slug
- `POST /api/events` - Create event (Admin/Speaker)
- `PUT /api/events/{id}` - Update event (Admin/Speaker, or assigned speaker)
- `DELETE /api/events/{id}` - Delete event (Admin only)
- `POST /api/events/{id}/publish` - Publish event (Admin/Speaker)
- `POST /api/events/{id}/speakers` - Add speaker to event

### 3.3 Event Editor Component

**Files to Create:**
- `src/UserGroupSiteQwen35.Server/Components/Pages/Admin/EventEditor.razor`
- `src/UserGroupSiteQwen35.Server/Components/Pages/Admin/EventEditor.razor.cs`
- `src/UserGroupSiteQwen35.Server/Components/Pages/Admin/EventEditor.razor.css`

**Features:**
- Form with all event fields
- Auto-generate slug from title (kebab-case) on blur
- Markdown editor with preview tabs
- Speaker selection (multi-select)
- Validation:
  - Title and Slug required for save
  - Description, Date/Time, Location, and 1+ speakers required for publish
- Draft mode (unpublished) vs Published state

### 3.4 Markdown Editor Component

**Files to Create:**
- `src/UserGroupSiteQwen35.Server/Components/Shared/MarkdownEditor.razor`
- `src/UserGroupSiteQwen35.Server/Components/Shared/MarkdownEditor.razor.cs`
- `src/UserGroupSiteQwen35.Server/Components/Shared/MarkdownEditor.razor.css`

**Features:**
- Edit mode with textarea
- Preview mode with rendered markdown
- Tab switching between edit/preview
- Use a markdown parsing library (Markdig)

---

## Phase 4: Topic Suggestions

### 4.1 Topic Suggestion Service

**Files to Create:**
- `src/UserGroupSiteQwen35.Server/Services/TopicSuggestionService.cs`
- `src/UserGroupSiteQwen35.Client/Services/ITopicSuggestionClientService.cs`
- `src/UserGroupSiteQwen35.Client/Services/TopicSuggestionClientService.cs`

**Features:**
- Get all topic suggestions with vote counts
- Create new suggestion
- Vote for a suggestion (one vote per user per topic)
- Remove vote
- Volunteer to speak on a topic
- Remove volunteer status

### 4.2 Topic Suggestion API Endpoints

**File to Create:**
- `src/UserGroupSiteQwen35.Server/Endpoints/TopicSuggestionEndpoints.cs`

**Endpoints:**
- `GET /api/topics` - List all suggestions with vote counts (authenticated)
- `POST /api/topics` - Create new suggestion (authenticated)
- `POST /api/topics/{id}/vote` - Vote for topic (authenticated)
- `DELETE /api/topics/{id}/vote` - Remove vote (authenticated)
- `POST /api/topics/{id}/volunteer` - Volunteer to speak (authenticated, no existing volunteer)
- `DELETE /api/topics/{id}/volunteer` - Remove volunteer (authenticated)

### 4.3 Topic Suggestions UI

**Files to Create:**
- `src/UserGroupSiteQwen35.Server/Components/Pages/TopicSuggestions/TopicList.razor`
- `src/UserGroupSiteQwen35.Server/Components/Pages/TopicSuggestions/TopicList.razor.cs`
- `src/UserGroupSiteQwen35.Server/Components/Pages/TopicSuggestions/TopicList.razor.css`
- `src/UserGroupSiteQwen35.Server/Components/Pages/TopicSuggestions/SuggestTopic.razor`
- `src/UserGroupSiteQwen35.Server/Components/Pages/TopicSuggestions/SuggestTopic.razor.cs`

**UI Components:**
- List of suggestions sorted by vote count
- Vote button (disabled if already voted)
- Vote count display
- Volunteer button (disabled if already has volunteer or user already volunteered)
- "Suggest Topic" form
- Filter by: voted, not voted, has volunteer, no volunteer

---

## Phase 5: UI and Navigation

### 5.1 Update Home Page

**File to Modify:**
- `src/UserGroupSiteQwen35.Server/Components/Pages/Home.razor`

**Features:**
- List all published events in descending date order
- Public access (no login required)
- Event cards with:
  - Title
  - Date/Time
  - Location
  - Short description
  - "View Details" link

### 5.2 Update Header/Navigation

**Files to Modify:**
- `src/UserGroupSiteQwen35.Server/Components/Layout/MainLayout.razor`
- `src/UserGroupSiteQwen35.Server/Components/Layout/MainLayout.razor.cs`

**Features:**
- Show "Welcome, {FirstName}" when logged in
- Dropdown menu with Logout option
- Navigation links based on user role:
  - Public: Home, Topic Suggestions
  - Logged in: Home, Topic Suggestions, (navigation items)
  - Admin/Speaker: Event Management, User Management (Admin only)

### 5.3 Event Detail Page

**Files to Create:**
- `src/UserGroupSiteQwen35.Server/Components/Pages/Events/EventDetail.razor`
- `src/UserGroupSiteQwen35.Server/Components/Pages/Events/EventDetail.razor.cs`
- `src/UserGroupSiteQwen35.Server/Components/Pages/Events/EventDetail.razor.css`

**Features:**
- Full event details with rendered markdown
- Speaker information
- Edit button (for editors)
- Public access for published events

### 5.4 AuthorizeView Helpers

**Files to Create:**
- `src/UserGroupSiteQwen35.Shared/Helpers/RoleHelper.cs`

**Features:**
- Extension methods for checking roles
- Helper methods for determining "editor" status

---

## Phase 6: Authorization and Security

### 6.1 Policy-Based Authorization

**File to Modify:**
- `src/UserGroupSiteQwen35.Server/Program.cs`

**Add Policies:**
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SpeakerOrAdmin", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || context.User.IsInRole("Speaker")));
    options.AddPolicy("EventEditor", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || IsAssignedSpeaker(context)));
});
```

### 6.2 Event Editor Authorization Handler

**File to Create:**
- `src/UserGroupSiteQwen35.Server/Authorization/EventEditorHandler.cs`
- `src/UserGroupSiteQwen35.Server/Authorization/EventEditorRequirement.cs`

### 6.3 Self-Admin Removal Prevention

**Implementation in:**
- `src/UserGroupSiteQwen35.Server/Services/UserManagementService.cs`

**Logic:**
- Check if current user is attempting to remove their own Admin role
- Return error/exception if so
- UI should disable/hide the option for self

---

## Phase 7: Testing and Validation

### 7.1 Unit Tests

**Project:**
- `src/UserGroupSiteQwen35.Data.Tests/` (if not exists, create test project)

**Test Coverage:**
- Event validation logic
- Role assignment logic
- Vote uniqueness
- Volunteer assignment rules

### 7.2 Integration Tests

**Test Coverage:**
- API endpoints
- Authorization policies
- Database operations

### 7.3 UI Testing

**Manual Test Scenarios:**
- User registration flow
- Admin role assignment
- Event creation and publishing
- Topic suggestion and voting
- Volunteer flow

---

## Phase 8: Additional Considerations

### 8.1 Markdown Rendering

**Package to Add:**
- Markdig (for server-side markdown parsing)

**Implementation:**
- Create a markdown pipe/service
- Sanitize HTML output to prevent XSS
- Apply Bootstrap styling to rendered HTML

### 8.2 Slug Generation

**Utility Class:**
- `src/UserGroupSiteQwen35.Shared/Utilities/SlugHelper.cs`

```csharp
public static string GenerateSlug(string title)
{
    // Convert to lowercase, replace spaces with hyphens
    // Remove special characters
    // Ensure uniqueness if needed
}
```

### 8.3 Email Notifications (Future Enhancement)

**Consider:**
- Welcome emails for new users
- Event reminders
- Topic suggestion notifications

### 8.4 Calendar Integration (Future Enhancement)

**Consider:**
- iCal export for events
- Add to Google Calendar/Outlook links

---

## File Structure Summary

```
src/
├── UserGroupSiteQwen35.Data/
│   ├── Models/
│   │   ├── Event.cs
│   │   ├── Speaker.cs
│   │   ├── TopicSuggestion.cs
│   │   ├── TopicVote.cs
│   │   ├── EventSpeaker.cs
│   │   └── RoleNames.cs
│   ├── Interfaces/
│   │   ├── IEventRepository.cs
│   │   ├── ISpeakerRepository.cs
│   │   └── ITopicSuggestionRepository.cs
│   └── Repositories/
│       ├── EventRepository.cs
│       ├── SpeakerRepository.cs
│       └── TopicSuggestionRepository.cs
├── UserGroupSiteQwen35.Server/
│   ├── Components/
│   │   ├── Pages/
│   │   │   ├── Admin/
│   │   │   │   ├── UserManagement.razor*
│   │   │   │   └── EventEditor.razor*
│   │   │   ├── Events/
│   │   │   │   └── EventDetail.razor*
│   │   │   └── TopicSuggestions/
│   │   │       ├── TopicList.razor*
│   │   │       └── SuggestTopic.razor*
│   │   └── Shared/
│   │       └── MarkdownEditor.razor*
│   ├── Services/
│   │   ├── EventService.cs
│   │   ├── UserManagementService.cs
│   │   └── TopicSuggestionService.cs
│   ├── Endpoints/
│   │   ├── EventEndpoints.cs
│   │   ├── UserManagementEndpoints.cs
│   │   └── TopicSuggestionEndpoints.cs
│   └── Authorization/
│       ├── EventEditorHandler.cs
│       └── EventEditorRequirement.cs
└── UserGroupSiteQwen35.Client/
    └── Services/
        ├── IEventClientService.cs
        ├── EventClientService.cs
        ├── IUserManagementClientService.cs
        ├── UserManagementClientService.cs
        ├── ITopicSuggestionClientService.cs
        └── TopicSuggestionClientService.cs
```

*Files marked with * also have .razor.cs and .razor.css companions

---

## Migration Steps

1. Create all new model files
2. Update ApplicationDbContext with new DbSets
3. Run: `dotnet ef migrations add AddUserGroupFeatures -p ../UserGroupSiteQwen35.Data`
4. Run: `dotnet ef database update`
5. Implement services and repositories
6. Create API endpoints
7. Build UI components
8. Test authorization flows

---

## Priority Order

1. **Phase 1** - Database Models (foundation)
2. **Phase 2** - User Management (needed for Admin features)
3. **Phase 3** - Event Management (core feature)
4. **Phase 5** - UI Updates (make features accessible)
5. **Phase 4** - Topic Suggestions (community feature)
6. **Phase 6** - Authorization hardening
7. **Phase 7** - Testing

---

## Notes

- All dates/times should be stored in UTC and converted to local time for display
- Consider timezone handling for events
- Implement proper error handling and user feedback (toasts)
- Use Bootstrap components for consistent styling
- Ensure responsive design for mobile access
