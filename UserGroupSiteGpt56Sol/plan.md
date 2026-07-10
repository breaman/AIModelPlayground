# Technical User Group Site Implementation Plan

## 1. Confirm the baseline and define acceptance criteria

- Preserve the existing .NET 10 Blazor Web App architecture, ASP.NET Core Identity setup, EF Core/SQL Server persistence, Aspire orchestration, Serilog logging, Bootstrap styling, and audit/fingerprinting infrastructure.
- Treat the existing registration, login, account-management, authenticated welcome text, and logout dropdown as the starting point; verify them instead of rebuilding them.
- Define the application roles as `Admin` and `Speaker`. Role membership controls user administration and event-speaker eligibility; a topic volunteer may be any authenticated user and does not automatically receive the `Speaker` role.
- Use UTC for stored event timestamps and convert to the intended display timezone at the UI boundary.
- Convert Markdown to sanitized HTML so preview and published content cannot execute unsafe markup.
- Record feature-level acceptance criteria from the requirements and use them to drive automated and manual testing.

## 2. Design and migrate the domain model

- Add an `Event` entity with title, unique slug, short description, Markdown description, event date/time, location, published flag, and audit fields.
- Add an explicit `EventSpeaker` join entity between events and users, with a composite uniqueness constraint so the same speaker cannot be assigned twice.
- Add a `TopicSuggestion` entity with title, optional supporting details, creator, creation timestamp, and an optional volunteer user.
- Add a `TopicVote` join entity between topic suggestions and users, with a composite unique index enforcing one vote per user per topic at the database level.
- Configure relationships, maximum lengths, required fields, indexes, delete behavior, and `DbSet` properties in `ApplicationDbContext`.
- Add an EF Core migration and ensure it works with the existing Aspire migration workflow.

## 3. Establish shared contracts and application services

- Create request/response DTOs and service interfaces in the Shared project so pages do not bind EF or Identity entities directly.
- Implement event, topic-suggestion, and user-administration services on the server with asynchronous EF Core queries, projection, cancellation support, and structured logging.
- Implement matching HTTP-backed client services and protected API endpoint groups for interactive WebAssembly pages.
- Follow the repository's prerendering pattern: register server implementations for prerendering, client implementations for WebAssembly hydration, and persist initial component state to avoid duplicate loading.
- Return consistent validation and authorization errors that the UI can display through the existing toast/validation mechanisms.

## 4. Seed roles and secure every operation

- Seed the `Admin` and `Speaker` roles idempotently and document/configure how the first administrator is provisioned.
- Add role- and policy-based authorization for administrative features and an event-editor policy that permits either an Admin or a speaker assigned to that event.
- Require authentication for creating topics, voting, and volunteering; require Admin for user management and event creation.
- Enforce authorization in server services/endpoints as well as hiding unavailable UI actions.
- Prevent an administrator from removing their own `Admin` role in the server-side command, regardless of the submitted payload, and disable that control in the UI for immediate feedback.
- Protect all write paths with antiforgery/authentication, validate resource ownership/assignment, and avoid trusting IDs or role values supplied by the browser.

## 5. Build user administration

- Add an Admin-only user list with name, email, current roles, and actions to edit role membership.
- Add a role-edit form that independently toggles `Admin` and `Speaker`, allowing either or both.
- Disable the current administrator's own Admin checkbox/removal action and show a clear explanation.
- Re-check the self-removal rule and valid role names on the server before applying changes.
- Show success, validation, and concurrency/error feedback, and refresh claims/session behavior where necessary after role changes.

## 6. Build event management and validation

- Add an Admin-only event list with draft/published status and create/edit navigation.
- Add an event form containing title, slug, short description, Markdown description, date/time, location, speaker selection, and published flag, using Bootstrap floating form controls where applicable.
- When the title loses focus, generate a kebab-case slug only if the slug remains empty; never overwrite a slug the editor entered.
- Add Edit and Preview tabs for the Markdown description, using the same sanitized rendering pipeline for preview and published output.
- Limit assignable event speakers to users in the `Speaker` role and support assigning one or more without duplicates.
- Enforce save rules on both client and server: title and slug are always required; published events additionally require description, date/time, location, and at least one speaker.
- Enforce unique, normalized slugs and return a useful validation message for conflicts.
- Permit assigned speakers to edit their events under the event-editor policy, while keeping event creation and speaker assignment/removal Admin-only unless requirements are later expanded.

## 7. Build public event pages and the home page

- Replace the placeholder home content with published events ordered by event date/time descending.
- Display useful event summary information and link each item to a public detail route based on its slug.
- Add a public event-detail page that renders the sanitized Markdown description, date/time, location, and speaker information.
- Ensure drafts never appear in public list/detail queries and return Not Found for an unpublished or unknown public slug.
- Add appropriate empty, loading, and failure states and verify the pages work for anonymous visitors.

## 8. Build topic suggestions, voting, and volunteering

- Add an authenticated topic list showing suggestion details, vote count, current user's vote state, and any volunteer.
- Add a form for any logged-in user to create a suggestion with server-side validation.
- Add an idempotent vote action (and an explicit unvote action only if desired by the final UX), while relying on the unique database constraint to prevent duplicate votes during concurrent requests.
- Allow any authenticated user to claim an unclaimed topic as its volunteer.
- Make volunteer claiming atomic so two users cannot claim the same suggestion; return a friendly conflict message to the loser of a race.
- Hide or disable volunteering after a topic has a volunteer and refresh displayed counts/state after successful actions.

## 9. Complete navigation and UX integration

- Add navigation links based on authentication and authorization: public Events/Home, authenticated Topic Suggestions, and Admin management links.
- Verify the header displays `Welcome, <firstName>` for authenticated users with a sensible fallback when first name is absent.
- Keep Log out as a dropdown item below the welcome element and preserve Manage Account.
- Add accessible labels, validation summaries, keyboard-friendly tabs/dropdowns, responsive layouts, confirmation where an action is consequential, and consistent Bootstrap icon usage.

## 10. Add automated tests for rules and security boundaries

- Add unit tests for slug generation/normalization, event draft and publish validation, Markdown sanitization, and event-editor authorization.
- Add data/service tests for event-speaker uniqueness, one vote per user/topic, atomic volunteer claiming, published-only queries, and descending event ordering.
- Add authorization/integration tests proving anonymous users cannot mutate data, non-admins cannot manage roles or create events, assigned speakers can edit only their assigned events, and admins cannot remove their own Admin role.
- Add Blazor component tests for slug-on-blur behavior, Markdown Edit/Preview tabs, conditional publish validation, role controls, vote state, and the authenticated header.
- Include regression tests for missing names, duplicate slugs, concurrent votes/volunteers, unpublished event URLs, and stale edit submissions.

## 11. Validate the complete application and document operation

- Run restore, build, formatting/analyzer checks, and the full automated test suite.
- Generate production CSS with the existing Sass script and verify static assets are included.
- Run the application through Aspire and manually exercise anonymous, regular-user, speaker, and admin journeys at desktop and mobile sizes.
- Verify migrations on a clean database, role/initial-admin provisioning, audit records, UTC/date display behavior, and production-safe Markdown output.
- Update the README with setup, migration, role bootstrap, first-admin provisioning, test, Sass, and Aspire run instructions.
- Consider the feature complete only when every acceptance criterion is covered by an automated test or a recorded manual verification step.
