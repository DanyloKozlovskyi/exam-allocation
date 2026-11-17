# 06 — Client (frontend)

React + TypeScript (strict) + Vite, structured like **pal-suite/client**: MUI, axios,
TanStack Query, notistack, react-router-dom, Jest. Located in `client/`.
Typing and constants rules: [10 §7](10-conventions.md#7-client-pal-suite-layout--stricter-typing).

## Structure recap

- `views/<Feature>/<Action>/index.tsx` — thin route pages: read params via `useParams`,
  render the matching `components/Global/<Feature>/<Action>` component, navigate on finish
  with `routes.*` builders.
- `components/Global/<Feature>/{Common,Create,Edit,List,View}` — feature components.
  `Common/index.ts` holds the form state interface, `ValidationErrors`, the reducer and the
  `<Feature>ReducerAction` enum; `Common/components/{Steps,StepDetails,StepReview}` implement
  the stepper flow (pal-suite pattern) using our `components/Common/CustomStepper`.
- `components/Global/Queries/<feature>.ts` — `use<Entity>Query` / `use<Entity>ListQuery` hooks.
- `<Action>/mutations.ts` — axios mutation functions (`createTestCenter(sessionId, dto)`),
  using `apiRoutes.*` and DTO interfaces from `api/dtos.ts`.

## Routes (`constants/routes.ts`)

```ts
export enum RoutePath {
  Home = '/',
  Sessions = '/sessions',
  SessionCreate = '/sessions/create',
  SessionView = '/sessions/:sessionId',
  SessionEdit = '/sessions/:sessionId/edit',
  Centers = '/sessions/:sessionId/centers',
  CenterCreate = '/sessions/:sessionId/centers/create',
  CenterEdit = '/sessions/:sessionId/centers/:centerId/edit',
  Rooms = '/sessions/:sessionId/centers/:centerId/rooms',
  RoomCreate = '/sessions/:sessionId/centers/:centerId/rooms/create',
  RoomEdit = '/sessions/:sessionId/centers/:centerId/rooms/:roomId/edit',
  Participants = '/sessions/:sessionId/participants',
  ParticipantsImport = '/sessions/:sessionId/participants/import',
  Rules = '/sessions/:sessionId/rules',
  RuleCreate = '/sessions/:sessionId/rules/create',
  RuleEdit = '/sessions/:sessionId/rules/:ruleId/edit',
  SolveRuns = '/sessions/:sessionId/solve-runs',
  SolveRunView = '/sessions/:sessionId/solve-runs/:runId',
  AllocationView = '/sessions/:sessionId/allocations/:allocationId',
  AllocationRoom = '/sessions/:sessionId/allocations/:allocationId/rooms/:roomId',
}
export const routes = { centers: { list: (sessionId: string) => generatePath(RoutePath.Centers, { sessionId }), … }, … };
```

`Routes.tsx` (pal-suite style, one `<Route>` per `RoutePath`) is written in Phase 0 with
placeholder views; each workstream replaces only its own views.

## App shell (`components/App`, `components/Layout`)

- Providers as in pal-suite: `QueryClientProvider` (`refetchOnWindowFocus: false`),
  `SnackbarProvider` (`maxSnack` etc. from `constants/ui.ts`), MUI `ThemeProvider` +
  `CssBaseline`, `BrowserRouter`. No MSAL in MVP.
- `api/axiosInstance.ts`: `baseURL` from config; response interceptor normalizes
  `ProblemDetails`. `helpers/processError(error, enqueueSnackbar)` switches on `ErrorCode`
  to pick the i18n message.
- Layout: left drawer with session steps (`components/Layout/navigation.ts`, badges from
  `sessions/{id}/totals` and the run count): Сесії → Огляд → Центри → Аудиторії (only while a center is
  open — rooms belong to a center) → Учасники → Правила → Розрахунок → Результати (only once a run
  succeeded; links to its best allocation).
- Mock mode: `config.useMocks = true` starts the MSW browser worker (`mocks/browser.ts`) before rendering;
  default `false` (real API) since Phase 2.

## Features

### Sessions
List (DataGrid), Create/Edit (stepper: Details → Summary), View = dashboard with totals,
step checklist, last run status.

### Centers (map) — `components/Global/Centers`
- `Map/CentersMap.tsx` — the only place using `@vis.gl/react-google-maps` (`APIProvider`,
  `Map`, `AdvancedMarker`), props `{ centers, selectedCenterId, onSelect, onAddAtPoint, onPlacePicked }`.
  Reused by allocation overview.
- Places Autocomplete (new Places API) → opens Create with name/address/lat/lng/placeId prefilled.
- Without API key (`config.googleMapsApiKey` empty): `Map/CentersMapFallback.tsx` with a
  coordinates form — used in tests.
- List with room count/capacity, active toggle, edit/delete.

### Rooms — `components/Global/Rooms`
- Create/Edit stepper: Details (name, rows, columns, seats per desk, aisles, cost) →
  Layout (interactive grid) → Summary.
- `components/Common/RoomGrid` — presentational SVG: desks, seats, aisles; `renderSeat`
  render prop; `SeatState` enum (`Normal`, `Accessible`, `Disabled`, `Occupied`,
  `Violation`, `Selected`); optional adjacency overlay per `AdjacencyKind`.
- Live preview via `rooms/preview-layout`; "Кількість копій" on the summary step (1–50) → `create-bulk`
  with names from the `bulk.nameTemplate` (`<name>-<n>`).

### Participants — `components/Global/Participants`
- List: MUI X DataGrid in server mode (paging/sorting/filter → `ParticipantSortField`,
  `SortDirection`), dynamic attribute columns, row actions.
- Import wizard (`components/Global/Imports`): stepper Upload → Sheet & header row →
  Column mapping (`ParticipantField` → header selects, suggested mapping prefilled) →
  Validation report (errors grid by `ImportErrorCode`) → Mode (`ImportMode`, `DedupeStrategy`) → Commit.
- Template download.

### Rules — `components/Global/Rules`
- List with enabled switch, hard/soft chip, weight.
- Create/Edit: type select (`RuleType`, from `rule-types`), then a **per-type parameters
  form** component chosen by a `Record<RuleType, React.FC<…>>` map; attribute selects from
  `sessions/{id}/attributes`; adjacency kinds as checkboxes with small illustrations.
- Precheck button ("Перевірити здійсненність", rules list) → `DiagnosticsPanel` (shared with Solve).

### Solve — `components/Global/SolveRuns`
- Run dialog with `SolveOptionsDto` fields (advanced section collapsed).
- `hooks/useSolveProgress(runId)` — `@microsoft/signalr` `HubConnection` to
  `HubRoute.SolveProgress`, invokes `HubMethod.SubscribeToRun`, listens to
  `HubEvent.ProgressUpdated` / `HubEvent.RunCompleted` (enums mirroring
  `SolveProgressHubMethods`); falls back to polling `solve-runs/{id}` every
  `SOLVE_POLL_INTERVAL_MS`.
- Progress view: stage stepper (`SolveStage`), objective/bound mini chart, cancel.
- `DiagnosticsPanel`: message, link to offending rule/center, suggestion text per
  `DiagnosticCode` (i18n). Shown on the run view when the run has diagnostics or is `Infeasible`.
- Infeasibility UX (Phase 3, H3 — `components/Global/SolveRuns/**`): a suggestion for every
  `DiagnosticCode` and deep links from each diagnostic to the rule / center / room it names;
  `Failed` runs explained by `failureReason` (`Interrupted`, `NoSolutionInTime`, `SolverError`) next
  to the free-text `errorMessage`. Until H3 lands the run view shows `errorMessage` only.
- Runs history DataGrid with status chip (`SolveRunStatus` → colour map constant), stale badge.

### Allocations — `components/Global/Allocations`
- Alternative tabs (index 0..N, objective summary), export menu (`ExportFormat`).
- Overview: `CentersMap` with fill-ratio colouring, rooms list with fill bars and penalties.
- Participants DataGrid (who sits where).
- Room seating: `RoomGrid` with surname + group colour (stable hash → palette constant),
  tooltip with details, violations highlighted by `ViolationKind`.
- Manual edit mode: select seat → select target → `validate-moves` → show report →
  "Зберегти як ручну версію" → `apply-moves` (a hard violation asks for confirmation and sends `force`);
  selecting an occupied target swaps the two participants. Pin room / pin all → pins API → re-run dialog.
- Pins page (`/sessions/:sessionId/pins`, drawer «Фіксації»): participant, room (link to its plan),
  seat, remove one / remove all (`pins/delete-all`); pin diagnostics link here.

## UX rules
- All user-facing text via i18n (`i18n/uk/<feature>.json`, typed keys); no inline literals.
- Numbers/dates via `Intl` with `LOCALE = 'uk-UA'` constant.
- Every mutation → notistack message; destructive actions → `ConfirmDialog`.
- `CustomBackdrop` while saving (pal-suite pattern); skeletons for lists.
- `data-qa` attributes from per-feature `TestId` enums (pal-suite uses `data-qa`).

## Tests
- Jest + ts-jest (jsdom) + Testing Library; MSW for network.
- `npm run typecheck`, `npm run lint` clean.
- Playwright e2e (Phase 3, H2 — `client/e2e/`): the administrator happy path in two modes —
  `npm run e2e:mocks` (MSW mocks, no backend) and `npm run e2e:api` (real API + Postgres);
  `npm run e2e` runs both. Selectors use the `data-qa` `TestId` enums.
