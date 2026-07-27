# Project conventions

Repo-specific decisions that are easy to get wrong and expensive to re-derive. The full coding
rules live in `.claude/rules/csharp.instructions.md` and `.claude/rules/blazor.instructions.md`.

## Render-mode decision rule

**Never** `@rendermode InteractiveServer`. Interactive components use
`@rendermode InteractiveWebAssembly` **and must live in the `Client` project**.

Decide per page:

- **No interactivity** → static SSR, in the `Server` project. Better first paint, works for
  anonymous and search traffic. The public meeting list and meeting detail pages are these.
- **Buttons, forms, or live preview** → `InteractiveWebAssembly`, in the `Client` project. The
  event editor, manage list, topic suggestions, and member administration are these.

## Dual-service pattern

Every feature service is implemented twice against one shared interface:

| Where | What it does |
| --- | --- |
| `Shared/Services/IThingService.cs` | The contract both sides compile against |
| `Server/Services/ServerThingService.cs` | Talks to `ApplicationDbContext` directly. Runs during pre-rendering and behind the API endpoints |
| `Client/Services/ClientThingService.cs` | Calls the matching HTTP endpoint |

Both are registered against the interface in their own `Program.cs`. Pre-rendered data properties
in a component carry `[PersistentState]` and are fetched with `??=` in `OnInitializedAsync`, so
hydration reuses the serialised state rather than refetching.

Mutations return `SaveResult` / `SaveResult<T>` so both transports report failures identically —
one throwing while the other returned a status code would force every call site to handle two
shapes.

## Authorization

- `PolicyNames.AdminOnly` / `PolicyNames.SpeakerOrAdmin` are role policies.
- `PolicyNames.EventEditor` is **resource-based**: any admin, or a speaker assigned to *that*
  event. Pass the event id as the resource. A blanket `[Authorize(Roles="Speaker")]` would wrongly
  let any speaker edit any event.
- The client-side check (`CanEditEventAsync`) exists to hide buttons. **The server check on every
  mutation is the enforcement point.** Server services re-validate the model and re-check
  authorization on every write; the client is never trusted.

## Things that will bite

- **`MarkdownRenderer` must keep `DisableHtml()`.** Its output goes through `MarkupString`, so
  removing it turns every user-authored description into stored XSS. If richer HTML is ever
  needed, sanitise server-side instead of relaxing the pipeline.
- **Status-code pages are scoped away from `/api`.** Re-executing an API 401 into the Blazor
  not-found page trips its antiforgery check and rewrites the response as an empty 400.
- **Never assign speakers from the request body unchecked.** `SaveEventAsync` verifies each id
  actually holds the Speaker role.
- **SQL Server column types break SQLite.** `nvarchar(max)` is not valid SQLite DDL, so the test
  harness strips column types and converts `DateTimeOffset` (which SQLite cannot `ORDER BY`) via
  `SqliteModelCustomizer`. Do not "fix" this by changing the production model.
- **RZ1021 build errors are an SDK bug, not your markup.** Run `dotnet build-server shutdown` and
  rebuild. Do not edit the Razor files or clean `obj/`.
- **`TreatWarningsAsErrors` is on solution-wide**, and it promotes NuGet audit warnings too — a
  transitively vulnerable package fails the build until it is pinned in `Directory.Packages.props`.
