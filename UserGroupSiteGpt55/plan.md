# Technical User Group Site Plan

## 1. Confirm Baseline Architecture

- Review the existing Blazor WebAssembly-first structure, Identity setup, EF Core context, Aspire AppHost, Bootstrap styling pipeline, and current navigation/authentication components.
- Keep interactive UI in the Client project with `@rendermode InteractiveWebAssembly` and use the dual service implementation pattern for pre-rendered pages that load data.
- Preserve the existing Identity user model and role support, extending it only where the user group features require additional fields or relationships.
- Define the first working milestone as an end-to-end vertical slice: database model, service/API access, admin UI, and public display for published events.

## 2. Domain Model and Persistence

- Add event domain entities:
  - `Event` with title, slug, short description, markdown description, event date/time, location, published flag, and audit fields.
  - `EventSpeaker` join entity linking events to users in the Speaker role.
- Add topic suggestion entities:
  - `TopicSuggestion` with title, description, suggested-by user, optional volunteer speaker, created date, and status if needed.
  - `TopicVote` linking a user to a topic suggestion with a unique constraint on topic/user.
- Configure EF Core relationships, delete behavior, indexes, and uniqueness rules:
  - Unique event slug.
  - Unique topic vote per user per topic.
  - Event-speaker many-to-many relationship.
- Add database migrations after the model is complete.
- Seed or document initial Admin role assignment so the first administrator can manage the site.

## 3. Roles, Authorization, and User Management

- Standardize role names as constants for `Admin` and `Speaker`.
- Add authorization policies:
  - Admin-only user management.
  - Admin-only event creation.
  - Event editor access for Admins or assigned speakers.
  - Logged-in user access for topic suggestions, voting, and volunteering.
- Build admin user management:
  - List registered users with first name, last name, email, and current Admin/Speaker role state.
  - Allow Admins to add or remove Admin and Speaker roles from other users.
  - Prevent the current Admin from removing their own Admin role in both UI and server-side logic.
  - Show clear validation feedback when a role change is rejected.
- Ensure role changes refresh user claims/session state where needed.

## 4. Event Services and Validation

- Create shared DTOs/view models for event list, event detail, event edit, speaker options, and validation results.
- Implement event service contracts in the Shared project and platform-specific implementations:
  - Client implementation calls HTTP endpoints.
  - Server implementation reads/writes through `ApplicationDbContext` for pre-rendering.
- Add server endpoints or controller/minimal API handlers for event operations.
- Implement event validation rules:
  - Title is required to save.
  - Slug is required to save.
  - Slug is unique.
  - When published, description, date/time, location, and at least one speaker are required.
- Add slug generation from title using kebab case when slug is empty and the title field loses focus.
- Enforce event editor rules in server-side write operations, not only in the UI.

## 5. Event UI

- Update the home page to list published events in descending event date/time order and keep it visible to anonymous users.
- Add an event detail page by slug for published events and authorized editors.
- Add admin event create/edit pages:
  - Use Bootstrap form controls wrapped in `form-floating` containers where applicable.
  - Keep Razor markup separate from code-behind.
  - Include title, slug, short description, description, date/time, location, speakers, and published flag.
  - Include edit and preview tabs for the markdown description.
  - Render markdown preview without saving the form.
  - Show validation messages for draft and publish-specific requirements.
- Add editor navigation/actions so Admins and assigned speakers can find events they are allowed to edit.

## 6. Markdown Support

- Select a markdown rendering approach, preferably a well-supported .NET library such as Markdig.
- Render markdown safely:
  - Sanitize or restrict unsafe HTML if raw HTML input is allowed by the renderer.
  - Use the same rendering behavior for preview and saved event display.
- Add tests around markdown rendering for common formatting and unsafe input behavior.

## 7. Topic Suggestions

- Build authenticated topic suggestion workflows:
  - Logged-in users can create topic suggestions.
  - Logged-in users can browse suggestions.
  - Logged-in users can vote once per suggestion.
  - Logged-in users can remove their vote if that behavior is desired.
  - Logged-in users can volunteer to speak on a suggestion only when no volunteer exists.
- Enforce voting and volunteering constraints in the database and server logic.
- Add UI states for already voted, volunteer already assigned, and current user as volunteer.
- Consider an Admin action to convert a topic suggestion into an event draft in a later milestone.

## 8. Header and Navigation

- Verify the existing authenticated header behavior:
  - Logged-in users see `Welcome, <firstName>`.
  - Logout appears as a dropdown menu item below the welcome element.
- Add role-aware navigation items:
  - Admin links for users and event management.
  - Speaker/editor links for editable assigned events.
  - Authenticated link for topic suggestions.
- Keep anonymous navigation focused on home, register, and login.

## 9. Testing and Verification

- Add focused unit tests for:
  - Slug generation.
  - Event save and publish validation.
  - Event editor authorization logic.
  - Self-admin role removal prevention.
  - Topic vote uniqueness.
  - Topic volunteer assignment rules.
- Add integration tests for protected endpoints and role-based behavior where practical.
- Add component or browser tests for the highest-risk flows:
  - Event create/edit with markdown preview.
  - Published event appears on the home page.
  - User role management cannot remove the current Admin's own Admin role.
  - Topic vote and volunteer behavior.
- Run `dotnet build` and `dotnet test` before considering each implementation milestone complete.

## 10. Delivery Milestones

1. Data model, migrations, role constants, and authorization policies.
2. Admin user management with self-admin removal protection.
3. Event CRUD services/endpoints with validation and editor authorization.
4. Event create/edit UI with slug generation and markdown preview.
5. Public home/event detail pages for published events.
6. Topic suggestion, voting, and volunteer workflows.
7. Navigation polish, tests, and Aspire run-through.

## 11. Open Decisions

- Decide whether topic votes can be removed after voting.
- Decide whether topic suggestions need moderation, status, or archival behavior.
- Decide whether event date/time should store timezone information or assume a site-wide timezone.
- Decide whether only Admins can assign speakers to events or whether assigned speakers can manage co-speakers.
- Decide whether unpublished events should be visible to assigned speakers before publication.
