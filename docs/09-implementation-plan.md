# 09 — Implementation Plan (multi-agent)

Written to be executed by an orchestrating agent that delegates to parallel subagents.
Every task has **owned paths**, **dependencies** and **acceptance checks**. A task is done
only when its checks pass. Patterns for every file: [10-conventions.md](10-conventions.md).

## Goal statement

> Implement the Exam Allocation MVP described in `docs/01`–`docs/10`, following the
> pal-suite architecture (Clean Architecture, MediatR CQRS, FluentValidation, LanguageExt,
> repositories + queries, controllers + controller services, error factories): a .NET 10 API
> with a CP-SAT solver and a React/MUI client that lets an organizer pick test centers on
> Google Maps, define room layouts, import participants from Excel, configure placement
> rules, compute an optimal seating plus alternatives (or a conflict explanation), review and
> adjust it visually, and export XLSX/XML/CSV/PDF. Done when every task below is complete,
> `dotnet test api/ExamAllocation.sln` and `cd client && npm run ci` pass on `main`, no
> `NotImplementedException` remains in `api/src`, and the medium benchmark meets 04 §12.

## Execution model

```
Phase 0  Foundation ............ 1 agent, sequential      creates EVERY file skeleton + DI + contracts
Phase 1  Parallel workstreams .. 7 agents in parallel     fill in owned files only
Phase 2  Integration ........... 1 agent + 1 reviewer     wires solve pipeline, removes mocks
Phase 3  Hardening ............. up to 4 agents           small independent tasks
```

### Why Phase 0 is big
pal-suite has several **shared registration points** (`Application/ConfigureServices.cs`,
`Infrastructure/Persistence/ConfigurePersistenceServices.cs`, `Api/Modules/SetupModule.cs`,
`ApplicationDbContext`, `Routes.tsx`, `ErrorCode`). If parallel agents edit them, they
conflict. So Phase 0 creates **all** classes/interfaces as stubs and registers all of them;
Phase 1 agents replace stub bodies in their own files and never touch registration files.

### Rules for parallel agents

1. One branch/worktree per workstream: `ws/<id>-<slug>` from tag `foundation`.
2. **Write only inside owned paths** (tables below). Reading anything is fine. Need a change
   elsewhere (new `ErrorCode` member, DTO field, DI registration)? Append to
   `docs/change-requests.md` (`- [WS-X] file — change — reason`) and stub around it.
   Exception: **adding members to enums you own** is allowed.
3. No new packages (all pinned in Phase 0).
4. No EF migrations except WS-C.
5. DTOs (`Api/Dtos/*`), `Solver/Model/*`, `ErrorCode`, `ApiRoutes` are frozen after Phase 0 —
   additive changes only via change requests, applied by the integrator.
6. Before finishing: acceptance checks, `dotnet build` with zero warnings (StyleCop errors
   are build errors), docs updated if implementation deviates, commit `WS-X: …`.
7. Orchestrator integrates branches one at a time with **squash merges** (`git merge --squash`
   + one commit; linear history, no merge commits — see CLAUDE.md → Git workflow); after each:
   `dotnet build`, `dotnet test`, `npm run ci`. A whole phase may also land as a single commit.

### Estimated effort (agent-assisted)

| Phase / WS | Hours |
|---|---|
| Phase 0 | 9–13 |
| WS-A Solver allocation | 10–16 |
| WS-B Layout & seating | 6–10 |
| WS-V Validator, DataGen, Bench | 5–8 |
| WS-C Domain behaviour, persistence, CRUD slices | 8–12 |
| WS-D Import/Export slices | 6–10 |
| WS-E Client: sessions, centers, participants, imports | 9–13 |
| WS-F Client: rooms, rules, solve, allocations | 11–17 |
| Phase 2 Integration | 8–14 |
| Phase 3 Hardening | 6–12 |
| **Total (sum)** | **78–125** (wall-clock lower thanks to parallelism) |

---

## Phase 0 — Foundation (single agent)

| Task | Deliverable | Acceptance |
|---|---|---|
| P0.1 Repo scaffold | Layout from 02; `docker-compose.yml` (postgres:17, db `exam_allocation`, user/pass `exam`); root `.editorconfig` for client; `api/.editorconfig` **copied from `../pal-suite/api/.editorconfig`**; `client/.prettierrc` copied from pal-suite | `docker compose config` OK |
| P0.2 Solution | `api/ExamAllocation.sln`; projects `Domain`, `Application`, `Infrastructure`, `Api`, `Solver`, tests `Api.Tests.Integration`, `Tests.Common`, `Tests.Data`, `Domain.Tests`, `Solver.Tests`, `Infrastructure.Tests`, tools `DataGen`, `Bench`; `Directory.Build.props` (net10.0, nullable, implicit usings, TreatWarningsAsErrors, StyleCop analyzers); `Directory.Packages.props` with all packages from 10 §9 + `Microsoft.EntityFrameworkCore.Design/Tools`, `Microsoft.AspNetCore.Mvc.Testing`, `NetArchTest.Rules`, xunit, `coverlet.collector`; `.config/dotnet-tools.json` with `dotnet-ef`; `api/.globalconfig` (SA0001 off, like pal-suite which generates no XML docs) | `dotnet build` OK |
| P0.3 Domain | `Common/{Guard,ValueObject,IEntity}`; every aggregate from 03 with `<Entity>Id`, `<Entity>Constraints`, entity class with private ctor + `New` + behaviour method **signatures** (bodies may throw `NotImplementedException` except trivial assignments); all enums; `Rules/Parameters/*` typed parameter records (polymorphic JSON); value objects `GeoPoint`, `PersonName`, `AttributeKey` (+ `AttributeKeys` consts), `SeatKey`, `RoomLayout` | builds; `SeatKey` round-trip test |
| P0.4 Solver seams | `Solver/Model/*` records & enums from 04 §1; `RuleParameters` typed records; interfaces `ISeatGenerator`, `IAdjacencyBuilder`, `IIndependentSetCalculator`, `IPrecheck`, `IStage1Solver`, `ISeatingSolver`, `IExpander`, `IAlternativesGenerator`, `IAllocationValidator`, `ISolverPipeline` + stub classes; `Solver/DependencyInjection.cs` `AddSolver()` registering all; `TestInputBuilder` in `Solver.Tests` | builds |
| P0.5 Application skeleton | `ConfigureServices` (validators, MediatR, behaviours, `AddSolver()`), `Common/Behaviours` (pal-suite), `Common/Errors/ErrorCode` (all codes known from 05/07), `Common/Settings/ApplicationSettings`, **all** repository/queries/service interfaces, **all** commands from the slice table below as records + handler stubs + validator stubs, all exception hierarchies (`<Feature>Exception` base + subclasses) | builds; MediatR resolves every handler (test) |
| P0.6 Infrastructure skeleton | `ApplicationDbContext` with all DbSets; every `<Entity>Configuration` (complete — ids, lengths from Constraints, enums as strings, jsonb, FKs, indexes from 03); `ApplicationDbContextInitialiser`; `RepositoryBase` (pal-suite); every `<Entity>Repository` class implementing repository+queries interfaces with stub bodies; `ConfigurePersistenceServices` with **all** registrations (pal-suite triple registration); stubs for `ImportExport/*` services, `Solving/{SolveQueue,SolveWorker}`, `Caching/InMemoryImportCache`, `DateTimeProvider`; **initial migration `Initial`** | migration applies to docker DB |
| P0.7 Api skeleton | `Program.cs` (pal-suite order, `--export-openapi <path>` switch via `OpenApiExportModule`), modules (`SetupModule` with JSON string enums + SignalR + all controller services registered, `DbModule`, `ApiEndpointsModule`, `ExceptionHandlingModule`); `Constants/ApiRoutes`, `HubRoutes`, `SolveProgressHubMethods`; **all DTOs** from 05 with `FromDomain`/`To<Command>` signatures; **all controllers** with every action from 05 (bodies return `StatusCode(501)`); all controller service interfaces + stub implementations; all `<Feature>ErrorFactory` with complete switch; `SolveProgressHub` stub; Swagger; exported `contracts/openapi.json` | `dotnet test` green (smoke + architecture test); `contracts/openapi.json` contains all routes & enums |
| P0.8 Test infrastructure | `Tests.Common` (`IntegrationTestWebFactory` with Testcontainers, `BaseIntegrationTest`, `TestsExtensions`) following pal-suite; `Tests.Data` empty factories per aggregate; one smoke integration test (`GET /health`) | test passes with Docker |
| P0.9 Client scaffold | Vite + React + TS strict; deps: MUI (+icons, x-data-grid, x-date-pickers), Emotion, TanStack Query, axios, notistack, react-router-dom, @microsoft/signalr, @vis.gl/react-google-maps, react-i18next/i18next, openapi-typescript, MSW, Jest, ts-jest, jest-environment-jsdom, Testing Library, Playwright, ESLint flat config + rules from 10 §7, Prettier; scripts `dev`, `build`, `export:api`, `gen:api` (`openapi-typescript ../contracts/openapi.json --enum --enum-values -o src/api/generated/schema.ts`), `typecheck`, `lint`, `test`, `ci`; `api/{axiosInstance,dtos}.ts`; **all** `constants/*` files (complete `RoutePath`, `routes`, `apiRoutes` for every endpoint in 05, `QueryKey`/`queryKeys`, `MutationKey`, `StorageKey`, `queryParams`, `ui`, hub enums); `components/{App,Layout,Common}` (`CustomStepper`, `ReadOnlyBlock`, `CustomBackdrop`, `ConfirmDialog`), `Routes.tsx` with a placeholder view per `RoutePath`; empty feature folders; `mocks/handlers/<feature>.ts` (empty arrays) + browser/test bootstrap; `i18n/uk/<feature>.json` (empty objects) + `common.json`; `enums/` folder | `npm run ci` green; app shell renders |
| P0.10 Planning files | `docs/change-requests.md`; `.gitignore` additions (`samples/generated/medium`, `samples/generated/large`, `client/test-results`) | committed, tag `foundation` |

### Feature slices (created as skeletons in Phase 0, implemented in Phase 1)

| Feature | Commands (Application) | Repository / Queries | Controller / Service / DTOs / ErrorFactory | Owner |
|---|---|---|---|---|
| Sessions | Create/Update/DeleteSession | `IExamSessionRepository`, `IExamSessionQueries` | `SessionsController`, `ISessionsControllerService`, `SessionsDtos`, `SessionErrorFactory` | C |
| Centers | Create/Update/DeleteTestCenter | `ITestCenterRepository`, `ITestCenterQueries` | `TestCentersController`, … | C |
| Rooms | Create/Update/DeleteRoom, CreateRoomsBulk | `IRoomRepository`, `IRoomQueries` | `RoomsController` (+ `preview-layout` via controller service → `ISeatGenerator`/`IAdjacencyBuilder`) | C |
| Participants | Create/Update/DeleteParticipant, DeleteAllParticipants | `IParticipantRepository`, `IParticipantQueries` | `ParticipantsController` | C |
| Rules | Create/Update/DeleteRule | `IRuleRepository`, `IRuleQueries` | `RulesController` | C |
| Pins | UpsertPin, DeletePin | `IPinRepository`, `IPinQueries` | `PinsController` (`pin-all` → Phase 2) | C |
| Imports | UploadImport, ValidateImport, CommitImport | uses `IParticipantRepository` + `IImportCache` | `ImportsController` | D |
| Exports | — (read side) | `IAllocationQueries` (export read model) | `ExportsController`, `IExportsControllerService` | D (+ Phase 2 for real data query) |
| SolveRuns | StartSolveRun, CancelSolveRun, RunPrecheck | `ISolveRunRepository`, `ISolveRunQueries` | `SolveRunsController`, `SolveProgressHub` | Phase 2 |
| Allocations | ApplyMoves, PinAll | `IAllocationRepository`, `IAllocationQueries` | `AllocationsController` | Phase 2 |

---

## Phase 1 — Parallel workstreams

| WS | Name | Owned paths | Depends on |
|---|---|---|---|
| **A** | Solver: classes, stage 1, precheck, conflicts, pipeline, alternatives | `api/src/Solver/{Allocation,Precheck,Pipeline,Alternatives,Resources}/`, `api/tests/Solver.Tests/{Allocation,Precheck,Pipeline,Alternatives}/` | P0 (B's services via stubs until merged) |
| **B** | Solver: layout, adjacency, α, stage 2, expansion | `api/src/Solver/{Layout,Seating,Expansion}/`, `api/tests/Solver.Tests/{Layout,Seating,Expansion}/` | P0 |
| **V** | Validator, DataGen, Bench | `api/src/Solver/Validation/`, `api/tests/Solver.Tests/Validation/`, `api/tools/**`, `samples/generated/small/` | P0 |
| **C** | Domain behaviour + persistence + CRUD slices | `api/src/Domain/**` (bodies; no new public types without CR), `api/src/Infrastructure/Persistence/**` (incl. migrations), slices owned by C in the table above across `Application/<Feature>/**`, `Api/Controllers/<Feature>Controller.cs`, `Api/Services/*/<Feature>ControllerService.cs`, `Api/Dtos/<Feature>Dtos.cs` (mapping bodies), `Api/Modules/Errors/<Feature>ErrorFactory.cs`, `Application/Solving/Services/SessionFingerprint.cs`, `api/tests/Api.Tests.Integration/{Sessions,Centers,Rooms,Participants,Rules,Pins}/`, `api/tests/Tests.Data/**`, `api/tests/Domain.Tests/**` | P0 |
| **D** | Import/Export | `api/src/Infrastructure/ImportExport/**`, `api/src/Infrastructure/Caching/**`, `Application/Imports/**`, `Application/Allocations/Services/AllocationExportModelBuilder.cs`, `Api/Controllers/{Imports,Exports}Controller.cs`, matching services/DTOs/error factories, `api/tests/Infrastructure.Tests/**`, `api/tests/Api.Tests.Integration/{Imports,Exports}/`, `samples/{participants-template.xlsx,allocation.xsd,allocation.sample.xml}` | P0 |
| **E** | Client: sessions, centers/map, participants, imports | `client/src/{views,components/Global}/{Sessions,Centers,Participants,Imports}/`, `client/src/components/Global/Queries/{sessions,centers,participants,imports}.ts`, `client/src/mocks/handlers/{sessions,centers,participants,imports}.ts`, `client/src/i18n/uk/{sessions,centers,participants,imports}.json` | P0 |
| **F** | Client: rooms, rules, solve, allocations | `client/src/{views,components/Global}/{Rooms,Rules,SolveRuns,Allocations}/`, `client/src/components/Common/RoomGrid/`, `client/src/hooks/useSolveProgress.ts`, `client/src/components/Global/Queries/{rooms,rules,solveRuns,allocations}.ts`, `client/src/mocks/handlers/{rooms,rules,solveRuns,allocations}.ts`, `client/src/i18n/uk/{rooms,rules,solveRuns,allocations}.json` | P0 |

UI-only enums go into `client/src/enums/<Feature>.ts`, owned by the feature's workstream.

### WS-A — Solver allocation (04 §3–4, §6–8)
| Task | Acceptance |
|---|---|
| A1 `ClassBuilder` | merge/split by referenced attributes, missing values, pins excluded |
| A2 Precheck (all `DiagnosticCode`s in 04 §7.1, messages from resx keyed by enum) | test per code |
| A3 Stage 1 model (hard + soft + objective modes + symmetry + size guardrail) | small instances to optimum; cost-first test |
| A4 Conflict core (assumptions + deletion minimization) | conflict test from 08 |
| A5 Pipeline (feedback loop, progress, cancellation, post-validation) | pipeline test on DataGen small; cancellation test |
| A6 Alternatives (stage-1 diversity, seat-level fallback, dedupe, budget) | alternatives test from 08 |

### WS-B — Layout & seating (03 adjacency, 04 §3.2, §5, §9)
| B1 `SeatGenerator` (`SeatKey`, labels, disabled/accessible) | tests |
| B2 `AdjacencyBuilder` (all `AdjacencyKind`s, aisles, seat-level side) | exact-edge tests |
| B3 `IndependentSetCalculator` (CP-SAT, cache) | known α values |
| B4 Stage 2 `SeatingSolver` (hard/soft R3, R8, R9, greedy path, pins, parallel rooms) | no hard violations; greedy deterministic |
| B5 `Expander` | deterministic output |

### WS-V — Validator & tooling (04 §10, 08)
| V1 `AllocationValidator` — independent brute-force implementation | valid/invalid cases per rule |
| V2 DataGen CLI | outputs listed in 08; committed small set |
| V3 Bench CLI | runs on small; reports stubs gracefully |

### WS-C — Domain, persistence, CRUD slices (03, 05, 10 §2–5)
| C1 Domain behaviour bodies (Guards, value objects, `Room.ChangeLayout` with seats produced by `ISeatGenerator` in the handler, capacity recompute) | `Domain.Tests` unit tests for guards, value objects and behaviour methods |
| C2 Repositories + queries for C-owned aggregates (paging, filters, `AsNoTracking`, `Option`) | integration tests |
| C3 Commands/handlers/validators for Sessions, Centers, Rooms (incl. bulk, pinned-seat conflict), Participants, Rules (typed parameters validation per `RuleType`), Pins | per command: success, not-found, conflict, validation (400 with `ValidationFailed`) |
| C4 Controllers + controller services + DTO mapping for those slices; `preview-layout`; `attributes`; `rule-types`; session `totals` | integration tests per endpoint |
| C5 `SessionFingerprint` (input hash) | changes only on relevant data |
| C6 Migrations for any schema adjustments (only owner) | `dotnet ef migrations list` clean; applies |

### WS-D — Import/Export (07)
| D1 Readers xlsx/csv (encodings, delimiters, header row) | fixture tests incl. cp1251 |
| D2 Mapping suggester (`ParticipantField` synonyms constants) + row validator (`ImportErrorCode`) | tests per code |
| D3 Import commands (upload → cache → validate → transactional commit) + controller | integration test with template |
| D4 Exporters Xlsx/Xml(+XSD)/Csv/Pdf from `AllocationExportModel` | XML validates vs XSD; XLSX sheets; PDF bytes > 0 |
| D5 Exports controller with test data source (real query in Phase 2) | content types & file names |

### WS-E — Client part 1 (06)
| E1 Sessions List/Create/Edit/View (stepper, dashboard) | Jest tests on MSW |
| E2 Centers: `CentersMap` + fallback, Places → Create, List/Edit/Delete | tests in fallback mode |
| E3 Participants DataGrid (server mode) + inline edit | tests |
| E4 Import wizard | full-wizard test on MSW |
| E5 MSW handlers + fixtures from `samples/generated/small` | pages usable on mocks |

### WS-F — Client part 2 (06)
| F1 `RoomGrid` (SVG, `SeatState`, aisles, adjacency overlay) | component tests |
| F2 Rooms Create/Edit stepper + preview + bulk | tests |
| F3 Rules list + per-`RuleType` parameter forms + precheck panel | tests |
| F4 Solve: run dialog, `useSolveProgress` (SignalR + polling fallback), diagnostics, history | tests with mocked hub |
| F5 Allocations: tabs, overview map (imports `CentersMap` from E; placeholder until merged), rooms list, DataGrid, seating view, manual moves, pins, export menu | tests |
| F6 MSW handlers | usable on mocks |

---

## Phase 2 — Integration (single agent, then a separate reviewer)

Owned: `Application/{Solving,Allocations}/**` (except D's export builder), `Infrastructure/Solving/**`,
`Api/Controllers/{SolveRuns,Allocations}Controller.cs` + services/DTOs/error factories,
`Api/Hubs/**`, `api/tests/Api.Tests.Integration/{SolveRuns,Allocations}/`, `contracts/openapi.json`,
`client/src/api/generated/**`, registration files, `docs/change-requests.md`.

| Task | Acceptance |
|---|---|
| I1 Squash-merge Phase 1 branches (if any remain); apply change requests; re-export OpenAPI; regenerate client enums/types | full build + tests green |
| I2 `SolverInputBuilder` (Domain → SolverInput, SolverOutput → Allocation entities) | mapping tests |
| I3 Solve commands, `SolveQueue`/`SolveWorker`, `SolveRunExecutor`, lifecycle, restart recovery, single-active-run rule, cancel | integration: small session → `Succeeded`, allocations persisted |
| I4 `SolveProgressHub` + `SignalRSolveProgressNotifier` (groups, last-progress replay) | test with `HubConnection` (pal-suite `GetHubConnection` helper) receives `RunCompleted` |
| I5 Allocation read endpoints, `validate-moves`, `apply-moves`, `pin-all`, real export query | tests |
| I6 Client switched to real API (`use_mocks=false`), fix drift | manual smoke of the happy path |
| I7 **Independent review** by a fresh agent: read 03–05 & 10, run DataGen medium + Bench, compare solver to 04, check pal-suite conventions (10) across a sample of files | `docs/review-phase2.md`; defects fixed or listed |

## Phase 3 — Hardening (parallel)

| Task | Owned | Acceptance |
|---|---|---|
| H1 Performance tuning; `docs/benchmarks.md` | `api/src/Solver/**` | medium & large within 04 §12 |
| H2 Playwright e2e (mocks + real API) | `client/e2e/**` | green |
| H3 Infeasibility UX (suggestions per `DiagnosticCode`, deep links) | `client/src/components/Global/SolveRuns/**` | tests |
| H4 README quick start, `docs/user-guide.md` (Ukrainian) | docs | reviewed |

## Global definition of done

- `dotnet build api/ExamAllocation.sln` with zero warnings; `dotnet test` green.
- `cd client && npm run ci` green (typecheck, lint incl. no literal unions / magic numbers, Jest, build).
- No `NotImplementedException` in `api/src`; no `StatusCode(501)` in controllers.
- No string-literal union types and no hard-coded routes/query keys in `client/src` (lint + grep).
- Docs updated where implementation deviates; `docs/change-requests.md` resolved.
