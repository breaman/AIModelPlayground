# User Group Meeting Site

A website for managing technical user group meetings, built with .NET 10 Blazor
(WebAssembly interactivity), ASP.NET Identity, EF Core + SQL Server, and Aspire.

## Features

- **Self-registration & login** — visitors register themselves via ASP.NET Identity;
  the header shows a "Welcome, *FirstName*" dropdown with account management and log out.
- **Role management** (Admin) — `/admin/users` lists every member with Admin/Speaker
  toggles. Admins cannot remove their own Admin role (enforced client- and server-side).
- **Event management** (Admin + assigned Speakers) — `/events` lists editable events;
  `/events/new` and `/events/{id}/edit` provide the editor with:
  - Markdown description editor with Edit/Preview tabs (Markdig, raw HTML disabled),
  - slug auto-fill from the title (kebab-case) with server-side uniqueness validation,
  - a speaker picker (Admins only) fed by users in the Speaker role,
  - publish rules: a published event requires a description, date/time, location, and
    at least one speaker — drafts only need a title and slug. Validation runs identically
    in the WASM form and the server endpoint via a shared `EventEditDto`.
  - Only Admins create events; Speakers can edit events they are assigned to.
- **Topic suggestions** (any member) — `/topics` lets members suggest topics, vote
  (one vote per member, enforced by a composite primary key), and volunteer to present
  (a single volunteer slot, race-safe). Topics sort by vote count.
- **Public home page** — anonymous, statically rendered list of published events
  (newest first) with Markdig-rendered descriptions.

A development-only admin account is seeded on startup: `admin@usergroup.local` /
`Admin123!`.

## Architecture

- `src/UserGroupSiteFable5.Client` — interactive (WASM) pages and components. Pages use
  the dual-mode service pattern with `[PersistentState]` so server pre-rendering works.
- `src/UserGroupSiteFable5.Server` — static SSR pages (Home, Identity account UI),
  minimal API endpoints under `/api/...`, and the server implementations of the shared
  data services. Event editing uses resource-based authorization
  (`IEventAuthorizationService`: Admin ∪ assigned speakers).
- `src/UserGroupSiteFable5.Shared` — DTOs (with DataAnnotations shared by client and
  server validation), service interfaces, and `SlugHelper`.
- `src/UserGroupSiteFable5.Data` — entities, `ApplicationDbContext` (with audit
  fingerprinting), and EF migrations. Roles `Admin`/`Speaker` are seeded via `HasData`.
- `tests/UserGroupSiteFable5.Tests` — xUnit tests (SQLite in-memory) for slug
  generation, publish validation, editor authorization, the self-lockout guard, and
  single-vote/single-volunteer enforcement: `dotnet test`.

## Bootstrap
Since this template utilizes bootstrap scss, the initial css file needs to be generated. There are two scripts included for doing this, one is sass-dev that will run the process in watch mode and the other is sass-prod that will compress the css file for production use. In order to do this perform the following steps in your terminal:

```
cd src/UserGroupSiteFable5.Server
npm install
npm run sass-dev (or sass-prod depending on which one you want)
```

## EF Migrations
This project adds EF as a dotnet tool, so before running any EF commands, one needs to run the following command from the project folder (there is also a command in the Aspire dashboard to run this restore command if the app is started before the restore command is run manually):

```
dotnet tool restore
```

The initial migration (`InitialDatabase`) already exists and covers Identity plus the
event/topic schema. To add further migrations:

```
cd src/UserGroupSiteFable5.Server
dotnet ef migrations add <MigrationName> -p ../UserGroupSiteFable5.Data
```

Migrations will run when the aspire project is started, so no need to run the migration manually after it is created.

## Aspire
This project is configured with aspire, so the recommended way to kick off project execution is the following:

```
cd aspire/UserGroupSiteFable5.AppHost
dotnet watch
```
