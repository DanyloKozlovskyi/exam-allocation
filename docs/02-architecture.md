# 02 — Architecture & Tech Stack

The architecture mirrors the team's reference project **pal-suite**. Coding patterns with
examples are in [10-conventions.md](10-conventions.md); this document covers stack, layout
and runtime.

## Stack

| Layer | Choice | Notes |
|-------|--------|-------|
| Runtime | **.NET 10 (LTS)** | pal-suite is on .NET 8; same patterns |
| Architecture | Clean Architecture: Domain / Application / Infrastructure / Api (+ pure Solver) | as pal-suite |
| Write side | **MediatR 12.x** commands + handlers returning `Either<FeatureException, Entity>` | CQRS, pal-suite |
| Read side | Controller services + `I<Entity>Queries` interfaces | pal-suite |
| Validation | **FluentValidation** + `ValidationBehaviour` MediatR pipeline | pal-suite |
| Functional types | **LanguageExt.Core 4.4.x** (`Either`, `Option`, `Unit`) | pal-suite |
| API | ASP.NET Core **controllers** (`[ApiController]`), Swashbuckle | pal-suite |
| Realtime progress | **SignalR** hub (`SolveProgressHub`) | client package `@microsoft/signalr` as in pal-suite |
| DB | **PostgreSQL 17**, EF Core + Npgsql, `EFCore.NamingConventions` (snake_case) | pal-suite |
| Solver | **Google OR-Tools CP-SAT** in a pure `Solver` project | |
| Excel / CSV / PDF | ClosedXML / CsvHelper / QuestPDF (in Infrastructure) | |
| Background jobs | `BackgroundService` + `System.Threading.Channels` | |
| Logging | built-in `ILogger` + `UnhandledExceptionBehaviour` | Sentry optional later (pal-suite uses it) |
| Analyzers | StyleCop.Analyzers + pal-suite `.editorconfig` | |
| Client | **React 19 + TypeScript 5.9 + Vite** (`@vitejs/plugin-react-swc`) | pal-suite; TS pinned to 5.9 (openapi-typescript peer) |
| UI kit | **MUI** (`@mui/material`, `@mui/icons-material`, `@mui/x-data-grid`, `@mui/x-date-pickers`), Emotion | pal-suite |
| Server state | **TanStack Query** + **axios** | pal-suite |
| Notifications | **notistack** | pal-suite |
| Routing | **react-router-dom** | pal-suite |
| Map | Google Maps JS API via `@vis.gl/react-google-maps` | |
| Room layout / seating | SVG components (`components/Common/RoomGrid`) | |
| API typing | `openapi-typescript --enum` → generated TS **enums** + DTO interfaces | ⚠ added for parallel work |
| Mocks | MSW **2.x** | ⚠ added: UI work before endpoints exist; MSW 3 is ESM-only and does not load in Jest on Node 22 |
| i18n | react-i18next, `uk` default, typed keys | ⚠ added (pal-suite is English-only) |
| Client tests | **Jest** + ts-jest + Testing Library (`jest-fixed-jsdom`) | pal-suite uses Jest; ESM-only MSW deps are transpiled by ts-jest (see `jest.config.cjs`) |
| Lint / format | ESLint 9+ flat config (`typescript-eslint`, react-hooks, react-refresh) + rules from 10 §7, Prettier (pal-suite config) | ⚠ pal-suite's `react-app` config does not support ESLint 9+ |
| Local infra | Docker Compose (postgres) | |

Package versions are pinned once in Phase 0 (`api/Directory.Packages.props`,
`client/package.json`); licensing constraints in 10 §9.

## Repository layout

```
exam-allocation/
├─ CLAUDE.md
├─ README.md
├─ docker-compose.yml
├─ docs/
├─ samples/                        sample XLSX/CSV inputs, XSD, sample XML
├─ contracts/openapi.json          exported Swagger document (committed) → client codegen
├─ api/
│  ├─ ExamAllocation.sln
│  ├─ .editorconfig                copied from pal-suite/api/.editorconfig
│  ├─ Directory.Build.props        net10.0, nullable, TreatWarningsAsErrors, StyleCop for all projects
│  ├─ Directory.Packages.props
│  ├─ src/
│  │  ├─ Domain/
│  │  │  ├─ Common/               Guard, ValueObject, IEntity
│  │  │  ├─ Sessions/  Centers/  Rooms/  Participants/  Rules/  Pins/  SolveRuns/  Allocations/
│  │  │  │                        <Entity>.cs, <Entity>Id.cs, <Entity>Constraints.cs, enums, value objects
│  │  │  └─ ValueObjects/          GeoPoint, PersonName, AttributeKey, SeatKey, RoomLayout
│  │  ├─ Application/
│  │  │  ├─ ConfigureServices.cs
│  │  │  ├─ Common/
│  │  │  │  ├─ Behaviours/         UnhandledExceptionBehaviour, ValidationBehaviour
│  │  │  │  ├─ Errors/             ErrorCode
│  │  │  │  ├─ Interfaces/         Repositories/, Queries/, IDateTimeProvider, ISolveQueue,
│  │  │  │  │                      ISolveProgressNotifier, IParticipantFileReader, IAllocationExporter, IImportCache
│  │  │  │  └─ Settings/           ApplicationSettings (Solver, Cors, Import sections)
│  │  │  ├─ Sessions/  Centers/  Rooms/  Participants/  Rules/  Pins/
│  │  │  │                        Commands/ (Command+Handler, Validator), Exceptions/
│  │  │  ├─ Imports/               Commands/ (Upload, Validate, Commit), Exceptions/, Services/
│  │  │  ├─ SolveRuns/             Commands/ (StartSolveRun, CancelSolveRun, RunPrecheck), Exceptions/
│  │  │  ├─ Solving/               Services/ (SolverInputBuilder, SolveRunExecutor, SessionFingerprint), Mapping/
│  │  │  └─ Allocations/           Commands/ (ApplyMoves, PinAll), Services/ (AllocationExportModelBuilder), Exceptions/
│  │  ├─ Infrastructure/
│  │  │  ├─ ConfigureServices.cs
│  │  │  ├─ Persistence/           ApplicationDbContext, Initialiser, Configurations/, Converters/, Repositories/, Migrations/
│  │  │  ├─ ImportExport/          Readers/, Mapping/, Validation/, Exporters/{Xlsx,Xml,Csv,Pdf}/, Constants/
│  │  │  ├─ Solving/               SolveQueue (Channel), SolveWorker (BackgroundService)
│  │  │  ├─ Caching/               InMemoryImportCache
│  │  │  └─ DateTimeProvider/
│  │  ├─ Api/
│  │  │  ├─ Program.cs
│  │  │  ├─ Constants/             ApiRoutes, HubRoutes
│  │  │  ├─ Controllers/           <Feature>Controller.cs
│  │  │  ├─ Dtos/                  <Feature>Dtos.cs
│  │  │  ├─ Hubs/                  SolveProgressHub, SignalRSolveProgressNotifier
│  │  │  ├─ Modules/               SetupModule, DbModule, ApiEndpointsModule, ExceptionHandlingModule, Errors/<Feature>ErrorFactory.cs
│  │  │  └─ Services/              Abstract/I<Feature>ControllerService.cs, Implementation/<Feature>ControllerService.cs
│  │  └─ Solver/
│  │     ├─ Model/  Layout/  Precheck/  Allocation/  Seating/  Alternatives/
│  │     ├─ Expansion/  Validation/  Resources/  Pipeline/
│  ├─ tests/
│  │  ├─ Api.Tests.Integration/    <Feature>/<Feature>ControllerTests.cs
│  │  ├─ Tests.Common/             IntegrationTestWebFactory, BaseIntegrationTest, TestsExtensions, Services/ (fakes)
│  │  ├─ Tests.Data/               <Aggregate>Data.cs
│  │  ├─ Domain.Tests/             entity/value-object unit tests
│  │  ├─ Solver.Tests/
│  │  └─ Infrastructure.Tests/     import/export unit tests
│  └─ tools/
│     ├─ DataGen/
│     └─ Bench/
└─ client/
   ├─ package.json, vite.config.mts, tsconfig.json, jest.config.cjs, eslint.config.js, .prettierrc
   ├─ e2e/   Playwright specs (Phase 3, H2)
   └─ src/   (see 06 and 10 §7)
```

## Dependency rules (architecture test in `Solver.Tests`/`Api.Tests.Integration`, NetArchTest)

```
Api ─────────► Application, Infrastructure
Infrastructure ► Application
Application ──► Domain, Solver
Domain ───────► (LanguageExt only)
Solver ───────► (OR-Tools only)        ← pure: input/output records only
```

The **Solver is pure** and knows nothing about entities. `Application/Solving/Services/SolverInputBuilder`
converts Domain → `SolverInput` and `SolverOutput` → `Allocation` entities.

## Runtime flow (solve)

```
Client ─POST sessions/{id}/solve-runs/create─► SolveRunsController
   └─ sender.Send(StartSolveRunCommand) ─► handler: validate, session fingerprint,
        SolveRun(Queued) via ISolveRunRepository (unique index: one Queued/Running run per session),
        ISolveQueue.Enqueue(runId) ─► 200 SolveRunDto
SolveWorker (BackgroundService, Infrastructure) ◄── Channel ── dequeue runId
   └─ scope → SolveRunExecutor (Application): load run, fingerprint again + Running (xmin-checked),
        snapshot → SolverInput, ISolverPipeline.RunAsync(input, progress, ct)
        progress ─► ISolveProgressNotifier ─► SignalR group "run:{runId}" (SolveProgressHub)
        result   ─► IAllocationRepository.AddRangeAsync, SolveRun → Succeeded|Infeasible|Failed|Cancelled
Client: HubConnection to /hubs/solve-progress, Invoke(SubscribeToRun, runId),
        on(ProgressUpdated) / on(RunCompleted); fallback polling GET solve-runs/{runId}
```

- One worker (runs execute one at a time); CP-SAT uses all cores itself. The stage-1 time limit and
  the number of alternatives are per-run options (`SolveOptionsDto`, defaults in `SolveRunOptions`).
- Cancel: `POST solve-runs/{runId}/cancel` → `CancelSolveRunCommand` → token → `StopSearch()`.
- Startup (`ApplicationDbContextInitialiser`): applies EF migrations, then runs left `Queued` or `Running` →
  `Failed` (the queue is in memory) with `failureReason = Interrupted` and the error message
  "Interrupted by server restart".
- `Failed` runs carry a `SolveRunFailureReason`: `Interrupted` (restart), `NoSolutionInTime` (solver status
  `NoSolutionInTime`, message "No solution found within the time limit") or `SolverError` (unexpected
  exception, message = exception text). `Infeasible` is a separate status with diagnostics, not a failure.

## Key decisions

1. **pal-suite patterns** for every layer (10). Deviations are marked ⚠ there.
2. **Two-stage solver decomposition** with participant-class aggregation (04).
3. **Contract-first client**: committed `contracts/openapi.json` + generated enums/interfaces + MSW mocks.
4. **Phase 0 creates every file skeleton** (interfaces, stub classes, DI registrations,
   controllers with all actions returning `501`) so parallel agents only fill in owned files (09).
5. **No auth in MVP** — add later following pal-suite (`[Authorize(Roles)]`, `PermissionsService`).
6. **Google Maps behind a boundary** — only `components/Global/Centers/Map/` knows about Google.

## Configuration (`api/src/Api/appsettings.json`, bound to `ApplicationSettings`)

```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=localhost;Port=5432;Database=exam_allocation;Username=exam;Password=exam" },
  "Solver": {
    "NumWorkers": 0,
    "Stage2TimeLimitSecondsPerRoom": 5,
    "MaxFeedbackIterations": 5
  },
  "Import": { "MaxFileSizeMb": 20, "MaxRows": 100000, "CacheMinutes": 60 },
  "Cors": { "Origins": [ "http://localhost:3000" ] }
}
```

Client `src/config.json` (pal-suite style, camelCase keys): `apiUrl` (default `http://localhost:5080`),
`googleMapsApiKey` (empty → coordinates-form fallback instead of the map), `googleMapsMapId`,
`useMocks` (default `false` since Phase 2; `true` starts the MSW browser worker and serves every request
from `src/mocks/handlers`, solve progress via polling). Read once in `constants/config.ts` (`appConfig`).

## Commands

| Task | Command |
|------|---------|
| Start DB | `docker compose up -d db` |
| Build / test API | `dotnet build api/ExamAllocation.sln` / `dotnet test api/ExamAllocation.sln` |
| Run API | `dotnet run --project api/src/Api` (http://localhost:5080, Swagger at `/swagger`) |
| Export OpenAPI | `cd client && npm run export:api` (runs `dotnet run --project ../api/src/Api -- --export-openapi ../../../contracts/openapi.json`; the path is relative to `api/src/Api`, no DB needed) |
| Add migration | `dotnet ef migrations add <Name> -p api/src/Infrastructure -s api/src/Api -o Persistence/Migrations` |
| Client dev | `cd client && npm run dev` (http://localhost:3000, pal-suite port) |
| Regenerate API types | `cd client && npm run gen:api` (`openapi-typescript --enum --enum-values`) |
| Client checks | `cd client && npm run ci` (typecheck + lint + test + build) |
| E2E (Phase 3, H2) | `cd client && npm run e2e:mocks` (MSW mocks, no API) · `npm run e2e:api` (real API + DB running) · `npm run e2e` (both); Playwright, specs in `client/e2e/` |
| Synthetic data | `dotnet run --project api/tools/DataGen -- --size small --out samples/generated` |
| Benchmarks | `dotnet run -c Release --project api/tools/Bench -- --size large` |
