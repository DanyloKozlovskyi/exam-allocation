# CLAUDE.md — rules for agents working in this repo

## Read first
1. `docs/README.md` → `docs/10-conventions.md` (how code is written) → `docs/09-implementation-plan.md`
   (find your workstream and owned paths).
2. The docs your task references. `docs/04-solver.md` is the source of truth for solver semantics.
3. Architecture reference: `../pal-suite` (if available). Copy **patterns**, never business code.

## Architecture in one screen (pal-suite)
- Layers: `Domain` → `Application` → `Infrastructure` → `Api`; pure `Solver` referenced by `Application`.
- **Writes**: controller → `ISender.Send(command)` → handler → repository; handler returns
  `Either<<Feature>Exception, Entity>`; one `AbstractValidator<TCommand>` per command
  (runs in `ValidationBehaviour`). Command + handler in one file; validator in `<Command>Validator.cs`.
- **Reads**: controller → `I<Feature>ControllerService` → `I<Entity>Queries` (no MediatR),
  returning `IReadOnlyList<Dto>` / `Option<Dto>`.
- Repositories extend `RepositoryBase<T,TKey>` and implement `I<Entity>Repository` **and**
  `I<Entity>Queries`.
- Entities: strongly-typed id records, private setters/ctor, `static New(...)`, behaviour
  methods, `Guard`; limits in `<Entity>Constraints`.
- Errors: `<Feature>Exception` hierarchy with `ErrorCode`; `<Feature>ErrorFactory.ToObjectResult()`
  maps to status codes + `ProblemDetails`.
- Routes: `[Route(ApiRoutes.…)]`, `create` / `edit` action segments (pal-suite style).

## Hard rules
- **Write only inside the owned paths of your workstream** (09). Need something elsewhere
  (DI registration, new `ErrorCode`, DTO field)? Append to `docs/change-requests.md` and stub around it.
- **Do not edit registration files** (`ConfigureServices.cs`, `ConfigurePersistenceServices.cs`,
  `SetupModule.cs`, `ApplicationDbContext.cs`, `Routes.tsx`) — Phase 0 registered everything.
- **No new packages.** Versions live in `api/Directory.Packages.props` and `client/package.json`.
  MediatR stays on 12.x, FluentAssertions on 7.x, LanguageExt on 4.4.x (licensing / API — see 10 §9).
- **No EF migrations** unless you are WS-C or the integrator.
- DTOs (`Api/Dtos`), `Solver/Model`, `ErrorCode`, `ApiRoutes` are frozen after Phase 0 (additive via change request).
- `client/src/api/generated/*` is generated (`npm run gen:api`) — never edit by hand.
- The Solver must not reference other repo projects (architecture test).
- Solver tests assert properties via `AllocationValidator`, never exact seat assignments.
- Write the math model in `docs/04-solver.md` before changing solver code that alters it.
- No network in tests (no Google Maps, no external HTTP). Postgres via Testcontainers.

## No magic strings / numbers
- Backend: routes in `ApiRoutes`/`HubRoutes`; limits in `<Entity>Constraints`; codes as enums
  (`ErrorCode`, `DiagnosticCode`, `ImportErrorCode`); config sections via `nameof`; seat keys via
  `SeatKey`; attribute keys via `AttributeKey`/`AttributeKeys`; import/export headers and names in
  `Infrastructure/ImportExport/Constants`; user-facing solver texts in `.resx` keyed by enum name.
- Client: **enums over types** — no string-literal union types (lint-enforced); API enums come
  from the generated schema (`--enum`); routes via `RoutePath`/`routes.*`, endpoints via
  `apiRoutes.*`, query keys via `queryKeys.*`, mutation keys via `MutationKey`, timings and sizes
  in `constants/queryParams.ts` / `constants/ui.ts`, test ids via `TestId` enums, texts via i18n.

## Conventions
- C#: file-scoped namespaces, primary constructors for DI (pal-suite), `record` DTOs/commands,
  nullable enabled, warnings and StyleCop as errors (`api/.editorconfig` from pal-suite),
  async with `CancellationToken` everywhere.
- Client: TypeScript strict, function components, MUI, TanStack Query + axios, notistack,
  pal-suite folders (`views/` thin pages, `components/Global/<Feature>/{Common,Create,Edit,List,View}`),
  reducer actions as enums, Prettier config from pal-suite.
- User-facing text is Ukrainian; code, comments, commits and docs are English.
- Commits: `WS-<id>: <imperative summary>` (`P0: …`, `INT: …`).

## Commands
| | |
|---|---|
| DB | `docker compose up -d db` |
| API build / test | `dotnet build api/ExamAllocation.sln` / `dotnet test api/ExamAllocation.sln` |
| Run API | `dotnet run --project api/src/Api` |
| Export OpenAPI | `cd client && npm run export:api` (then `npm run gen:api`) |
| Migration (WS-C / integrator only) | `dotnet ef migrations add <Name> -p api/src/Infrastructure -s api/src/Api -o Persistence/Migrations` |
| Client | `cd client && npm run dev` · `npm run gen:api` · `npm run ci` |
| Synthetic data | `dotnet run --project api/tools/DataGen -- --size small --out samples/generated` |
| Benchmark | `dotnet run -c Release --project api/tools/Bench -- --size medium` |

## Before you finish a task
1. Run your task's acceptance checks (09) plus build/test/lint for your side.
2. Update docs if your implementation deviates from them.
3. Commit on your workstream branch.
