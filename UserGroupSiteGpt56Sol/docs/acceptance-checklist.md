# Acceptance checklist

This matrix is the operational acceptance record for the technical user-group features.

## Automated verification

- [x] Draft events require title and normalized slug.
- [x] Publishing additionally requires description, UTC date/time, location, and a speaker.
- [x] Slug generation produces stable lowercase ASCII kebab-case values.
- [x] Unsafe raw HTML is encoded by the Markdown renderer.
- [x] Supported headings, emphasis, code, and lists render as safe HTML.
- [x] Normalized event slugs have a unique database index.
- [x] Event-speaker assignments use a unique composite primary key.
- [x] Topic votes use a unique composite key/index.
- [x] Volunteer assignment is optional and protected by row-version concurrency.
- [x] Published event service queries exclude drafts and order event dates descending.
- [x] Assigned-speaker event authorization permits the assignee and rejects another user.
- [x] The server-side self-Admin-removal guard rejects a malicious role payload.
- [x] Repeat voting is idempotent and an existing topic volunteer wins.
- [x] All application write routes carry authorization and antiforgery metadata.
- [x] Blazor tests cover slug-on-blur, safe Markdown preview, publish validation, self-role controls, vote state, and the authenticated header.
- [x] The complete solution builds, including Razor/WebAssembly output.
- [x] Production Sass generation completes and emits `wwwroot/css/site.css`.

## Code-enforced security and behavior

- [x] Anonymous users can only query published event list/detail endpoints.
- [x] Topic creation, voting, unvoting, and volunteering require authentication.
- [x] Event creation and user-role management require `Admin`.
- [x] Event editing uses a resource policy allowing `Admin` or an assigned speaker.
- [x] Only administrators can assign speakers, and selected users must hold `Speaker`.
- [x] An administrator cannot remove their own `Admin` role in either UI or server command.
- [x] Every write endpoint requires antiforgery validation.
- [x] Duplicate votes are idempotent and protected by the database constraint.
- [x] Volunteer claiming uses a serializable transaction and row-version conflict handling.
- [x] Public event queries filter drafts and sort newest event date first.
- [x] Stale event edits return a reload-and-retry concurrency message.

## Manual browser matrix

Run this matrix after `aspire run` with a healthy container runtime:

- [ ] Anonymous desktop/mobile: home empty/list states, published detail, draft/unknown detail not found, register, and login.
- [ ] Regular user desktop/mobile: welcome fallback, account menu, logout, create topic, idempotent vote/unvote, and volunteer claim.
- [ ] Speaker: only assigned events listed; assigned edit succeeds; speaker assignment controls are disabled; unassigned edit is forbidden.
- [ ] Admin: user list, independent Admin/Speaker toggles, own Admin toggle disabled, create/edit/preview/publish event, duplicate slug feedback, and speaker selection.
- [ ] Concurrency: race two votes, volunteer claims, and stale event saves; verify friendly outcomes and one persisted winner where applicable.
- [ ] Persistence: inspect SQL rows, UTC values, audit entries, and apply the migration to a clean database.
- [ ] Accessibility: keyboard navigation for menu/tabs/forms, visible validation summaries, accessible labels, and responsive layouts.

The browser matrix could not be completed in the implementation environment on 2026-07-10 because the Aspire dashboard reported the Docker container runtime as unhealthy. The dashboard and resource graph were successfully opened with a real browser; SQL Server, migrations, and the server correctly remained waiting for that unavailable dependency.
