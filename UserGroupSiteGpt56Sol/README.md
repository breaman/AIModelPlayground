# Technical User Group Site

A .NET 10 Blazor Web App for publishing user-group events and coordinating community topic suggestions. The application uses ASP.NET Core Identity, EF Core with SQL Server, Aspire orchestration, Interactive WebAssembly with prerendering, Serilog, Bootstrap, and the existing audit/fingerprinting pipeline.

## Prerequisites

- .NET SDK version pinned by `global.json`
- .NET Aspire CLI and a healthy Docker-compatible container runtime
- Node.js/npm
- A trusted ASP.NET Core development HTTPS certificate

Restore both toolchains from the repository root:

```bash
dotnet tool restore
dotnet restore
cd src/UserGroupSiteGpt56Sol.Server
npm ci
```

## Database and migrations

The initial migration includes Identity, audit tables, events and event speakers, topic suggestions, votes, and volunteer assignments. Aspire runs database updates before starting the server.

Create a future migration from the repository root with:

```bash
dotnet ef migrations add MigrationName \
  --project src/UserGroupSiteGpt56Sol.Data \
  --startup-project src/UserGroupSiteGpt56Sol.Data \
  --context ApplicationDbContext
```

The design-time context only supplies tooling configuration. Runtime connections continue to come from Aspire/configuration.

## Roles and the first administrator

`Admin` and `Speaker` are created idempotently at application startup. Topic volunteers only need an authenticated account; volunteering does not grant the `Speaker` role.

To provision the first administrator:

1. Register and confirm the account normally.
2. Store its email in user secrets:

```bash
dotnet user-secrets set "InitialAdmin:Email" "admin@example.com" \
  --project src/UserGroupSiteGpt56Sol.Server
```

3. Restart the application. Startup grants that existing account the `Admin` role. Do not store an administrator email or credentials in committed configuration.

Role changes update the user's security stamp, so affected users must sign in again. The server always rejects attempts by an administrator to remove their own `Admin` role.

## Run with Aspire

From the repository root:

```bash
aspire run
```

The Aspire dashboard exposes the server endpoint and shows the SQL Server, migration, and application resources. Stored event timestamps are UTC; pages display them in the visitor's local timezone.

## Build, test, and format

```bash
dotnet build UserGroupSiteGpt56Sol.slnx
dotnet test UserGroupSiteGpt56Sol.slnx
dotnet format UserGroupSiteGpt56Sol.slnx --verify-no-changes
```

The automated suite covers slug generation/normalization, conditional publish validation, safe Markdown rendering, relational constraints, published-event filtering/order, resource authorization, role safety, topic command behavior, endpoint security metadata, and core Blazor interactions. See `docs/acceptance-checklist.md` for the complete automated and manual acceptance matrix.

## Sass and static assets

Development watch mode:

```bash
cd src/UserGroupSiteGpt56Sol.Server
npm run sass-dev
```

Production CSS:

```bash
cd src/UserGroupSiteGpt56Sol.Server
npm run sass-prod
```

The server build copies the pinned Bootstrap bundle from `node_modules`, so run `npm ci` before the first build.

## Security notes

- Public event queries return only published events.
- All commands require authentication; user administration and event creation require `Admin`.
- Assigned speakers may edit their events but cannot alter speaker assignments.
- Every browser write carries an ASP.NET Core antiforgery token.
- Event slugs, speaker role eligibility, ownership/assignment, and role changes are revalidated on the server.
- Markdown is HTML-encoded before a small safe formatting subset is rendered; raw HTML is never trusted.
- Database constraints protect unique normalized slugs, event-speaker assignments, and one vote per user/topic.
