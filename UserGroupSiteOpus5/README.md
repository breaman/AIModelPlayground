# User Group Site

A site for managing technical user group meetings: events, speakers, topic suggestions, and member
administration.

## Getting started

```
cd src/UserGroupSiteOpus5.Server && npm install && npm run sass-dev
dotnet tool restore
aspire run
```

`npm install` and a sass build are required before the first `dotnet build`: the site's `site.css`
is generated from `styles/site.scss` and is not committed, and the build copies Bootstrap's JS out
of `node_modules`.

> If a build fails with **RZ1021** ("Markup in a code block must start with a tag…") in files you
> did not touch, run `dotnet build-server shutdown` and build again. This is a known .NET SDK
> 10.0.301 issue, not a defect in the Razor markup.

## Roles

Two roles, defined once in `Shared/Common/RoleNames.cs` and seeded at startup:

| Role | Can do |
| --- | --- |
| `Admin` | Manage members and roles; create, edit, and publish any meeting |
| `Speaker` | Be assigned to a meeting, and edit the meetings they are assigned to |

Anyone who registers can suggest topics, vote on them, and volunteer to present one.

**An administrator cannot remove their own `Admin` role.** The checkbox is disabled in the UI and
the rule is re-checked server-side, so a hand-crafted request to the API cannot bypass it either.

Role claims live in the auth cookie, so a role change takes effect when the cookie is next
revalidated — 30 seconds in Development, 30 minutes otherwise
(`SecurityStampValidatorOptions.ValidationInterval`).

## Bootstrap administrator

Role management requires an administrator, so the first one has to come from configuration. The
seeder (`Server/Data/DatabaseSeeder.cs`) reads these keys at startup and creates or promotes the
account, with its email pre-confirmed:

| Key | Purpose |
| --- | --- |
| `Admin:Email` | Sign-in email. Required. |
| `Admin:Password` | Initial password. Required. |
| `Admin:FirstName` | Optional, defaults to "Site". |
| `Admin:LastName` | Optional, defaults to "Administrator". |

All projects share one `UserSecretsId`, so set them once:

```
cd src/UserGroupSiteOpus5.Server
dotnet user-secrets set "Admin:Email" "admin@usergroup.local"
dotnet user-secrets set "Admin:Password" "Admin123!"
```

With the keys absent the seeder logs a warning and skips — no account is created with a default
password.

## Seeding

At startup the seeder always ensures both roles exist and applies the bootstrap administrator. In
**Development only**, it also adds a few sample meetings, and only when the `Events` table is
empty.

## Registration

`Program.cs` sets `options.SignIn.RequireConfirmedAccount = true`, but `IEmailSender<User>` is the
template's `IdentityNoOpEmailSender`. Self-registered members therefore land on a confirmation page
that shows the confirmation link rather than receiving an email. That is fine for development;
before a real deployment either wire a real email sender or relax the flag.

## Architecture

- **Render modes.** A page that needs no interactivity stays static SSR in the `Server` project
  (better first paint, works for anonymous and search traffic). Anything with buttons, forms, or a
  live preview is an `InteractiveWebAssembly` component in the `Client` project.
  `InteractiveServer` is never used.
- **Dual services.** Each feature interface lives in `Shared`, with a database-backed
  implementation in `Server` (used during pre-rendering and behind the API) and an HTTP-calling one
  in `Client`. Pre-rendered data properties carry `[PersistentState]` so hydration reuses the
  serialised state instead of refetching.
- **Authorization.** `AdminOnly` and `SpeakerOrAdmin` are role policies. Editing a meeting is
  resource-based (`EventEditor`): any admin, or a speaker assigned to *that* meeting. Hiding a
  button is cosmetic; the server check on every mutation is the enforcement point.
- **Markdown.** Event descriptions are stored as Markdown and rendered through
  `Shared/Services/MarkdownRenderer.cs`. Its pipeline calls `DisableHtml()`, which is the control
  that makes the output safe to emit through a `MarkupString` — without it, a description is a
  stored-XSS vector.
- **CSRF.** The JSON API authenticates with the Identity cookie, and `UseAntiforgery` does not
  validate JSON bodies. State-changing endpoints therefore require the `X-Requested-With` header,
  which a cross-site form cannot set; the client attaches it via a `DelegatingHandler`.

## EF migrations

EF is installed as a local tool, so run `dotnet tool restore` before any EF command (the Aspire
dashboard also exposes a command to do this).

```
cd src/UserGroupSiteOpus5.Server
dotnet ef migrations add <Name> -p ../UserGroupSiteOpus5.Data
```

Migrations are applied by Aspire's `ef-migrations` resource when the app starts, so there is no
need to run `database update` by hand.

## Tests

```
dotnet test
```

`tests/UserGroupSiteOpus5.Tests` covers the slug generator, model validation, the event-editor
authorization handler, Markdown escaping, the server services, the event editor component (bUnit),
and endpoint authorization end-to-end (`WebApplicationFactory`).

Data-layer tests run on **SQLite in-memory**, not `EntityFrameworkCore.InMemory`: the in-memory
provider does not enforce unique indexes, and the duplicate-vote and slug-collision behaviour under
test depends on exactly those constraints holding.

## Production builds

Run `npm run sass-prod` in `src/UserGroupSiteOpus5.Server` before publishing so the stylesheet is
minified.
