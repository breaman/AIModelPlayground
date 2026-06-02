# User Group Site

A .NET 10 + Blazor WebAssembly site for a user group: events, topic
suggestions & voting, and member self-service with role-based
authorization.

## Stack

- **.NET 10** (C# 14) — file-scoped namespaces, primary constructors,
  `TreatWarningsAsErrors=true`.
- **Blazor WebAssembly** with the pre-rendering pattern (see
  `Editor.razor`/`Users.razor`). All interactive components use
  `@rendermode InteractiveWebAssembly`; no SignalR circuits.
- **Aspire 13.3.3** for local orchestration (AppHost wires up
  SQL Server, EF migrations on start, OpenTelemetry, health checks).
- **ASP.NET Identity** with int keys and `Admin` / `Speaker` roles.
- **EF Core 10** + SQL Server (production) / SQLite (tests).
- **Markdig** for markdown rendering with a hand-rolled HTML sanitizer
  allow-list.
- **Serilog** for structured logging.

## Layout

```
src/
  UserGroupSiteMiniMaxM3.Shared/   # cross-platform DTOs and service contracts
  UserGroupSiteMiniMaxM3.Data/     # EF entities + data services
  UserGroupSiteMiniMaxM3.Client/   # Blazor WASM app
  UserGroupSiteMiniMaxM3.Server/   # ASP.NET host, identity, endpoints, server pages
aspire/
  UserGroupSiteMiniMaxM3.AppHost/        # Aspire orchestration
  UserGroupSiteMiniMaxM3.ServiceDefaults/  # shared telemetry / health checks
tests/
  UserGroupSiteMiniMaxM3.Tests/    # xUnit + FluentAssertions
```

`Shared` has no project references (to avoid a circular dep with `Data`).
Anything in `Data` can be consumed by `Server` and `Client`; anything
in `Client` cannot be referenced by `Data` or `Server`.

The same `IXxxService` interface lives in `Shared.Services`; the
server implements it with the EF-aware data service (sometimes via a
thin wrapper that injects the current user), and the client implements
it by calling the HTTP API.

## Features

- **Home** (`/`) — hero card for the next upcoming event plus a list
  of further events. Server-rendered.
- **Events** (`/events`, `/events/{slug}`) — public listings and
  detail page; markdown body rendered server-side and sanitized.
- **Manage events** (`/events/manage`, `/events/manage/new`,
  `/events/manage/{id}`) — auth-gated. Editor uses the dual-mode
  service pattern with `[PersistentState]`.
- **Topics** (`/topics`) — auth-gated. List, suggest, vote, volunteer,
  delete (own only; admins can delete any).
- **Users** (`/admin/users`) — admin only. Toggle `Admin` / `Speaker`
  roles; UI prevents the current user from demoting themselves.

## Bootstrap

The project uses Bootstrap CSS via SCSS. The `package.json` in
`src/UserGroupSiteMiniMaxM3.Server` has scripts to compile the
SCSS in dev (watch) and prod (compressed) modes:

```
cd src/UserGroupSiteMiniMaxM3.Server
npm run sass-dev   # or sass-prod
```

Bootstrap JS (for the navbar collapse and dropdowns) is copied into
`wwwroot/js/` automatically by the `CopyBootstrapJs` MSBuild target in
the Server `.csproj`.

## Database

EF is registered as a local dotnet tool. Restore tools first:

```
dotnet tool restore
```

Create a new migration (from the repo root):

```
dotnet ef migrations add <Name> \
  --project src/UserGroupSiteMiniMaxM3.Data \
  --startup-project src/UserGroupSiteMiniMaxM3.Server
```

Migrations run automatically when the Aspire AppHost starts. SQL
Server is provisioned by the AppHost and seeded with the `Admin` and
`Speaker` roles.

## Aspire

The recommended way to run the project locally:

```
cd aspire/UserGroupSiteMiniMaxM3.AppHost
dotnet watch
```

Aspire brings up SQL Server, runs migrations, and opens the
dashboard.

## Testing

xUnit + FluentAssertions. The test project uses SQLite (file-backed,
per test) so the data services can be exercised end-to-end without
SQL Server.

```
dotnet test tests/UserGroupSiteMiniMaxM3.Tests
```

## Conventions

- All form controls use Bootstrap's `form-floating` wrapper.
- Icons come from the `bootstrap-icons` library.
- Razor pages put all code in a separate `.razor.cs` file.
- Use `is null` / `is not null`; do not use `== null` / `!= null`.
- Use `nameof(...)` for member names in strings.
- Use `PascalCase` for everything public; `camelCase` for private
  fields.
