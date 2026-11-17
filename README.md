# exam-allocation

Web application for allocating exam participants to test centers, rooms and seats under
placement constraints — e.g. groupmates may not share a room or sit at adjacent desks.

- Pick test centers on Google Maps (or by coordinates), define room layouts (rows, desks, seats, aisles)
- Import participants from Excel/CSV
- Configure hard and soft placement rules
- Compute an optimal allocation plus diverse alternatives (OR-Tools CP-SAT), or get an
  explanation of conflicting rules
- Review results on the map and on seating charts, adjust manually, export XLSX / XML / CSV / PDF

**Stack:** .NET 10 · Clean Architecture · MediatR (CQRS) · FluentValidation · LanguageExt ·
EF Core + PostgreSQL · Google OR-Tools · SignalR · React + TypeScript + Vite · MUI ·
TanStack Query · Google Maps JS API

## Documentation

- **User guide (Ukrainian):** [`docs/user-guide.md`](docs/user-guide.md) — the whole workflow for an exam administrator.
- **Developer docs:** [`docs/`](docs/README.md) — overview, architecture, data model, solver design, API,
  client, import/export, testing, benchmarks, implementation plan and coding conventions.
- Agent rules: [`CLAUDE.md`](CLAUDE.md).

## Quick start

### Prerequisites

| Tool | Version | Used for |
|---|---|---|
| .NET SDK | 10.0 | API, solver, tests, DataGen / Bench tools |
| Node.js | 22 LTS or newer (npm included) | client |
| Docker | with Compose v2 | PostgreSQL 17; integration tests (Testcontainers) |

Ports used locally: **5432** (Postgres), **5080** (API), **3000** (client).

### 1. Database

```bash
docker compose up -d db          # postgres:17 on localhost:5432, db exam_allocation, user/password exam/exam
```

The connection string is in `api/src/Api/appsettings.json` (`ConnectionStrings:DefaultConnection`).

### 2. API

```bash
dotnet run --project api/src/Api # http://localhost:5080
```

- EF Core migrations are applied automatically on start; no `dotnet ef` step is needed.
- Swagger UI: <http://localhost:5080/swagger> (Development environment, the default launch profile).
- Health check: <http://localhost:5080/health>.
- Solve runs still `Queued`/`Running` when the API stopped are marked `Failed` (reason `Interrupted`) on the next start.

### 3. Client

```bash
cd client
npm install                      # also enables the repo's git hooks (core.hooksPath = .githooks)
npm run dev                      # http://localhost:3000
```

Client settings live in `client/src/config.json`:

| Key | Default | Meaning |
|---|---|---|
| `apiUrl` | `http://localhost:5080` | API base URL (CORS allows `http://localhost:3000` only — open the client via `localhost`, not `127.0.0.1`) |
| `useMocks` | `false` | `true` serves every API call from in-browser MSW mocks (`src/mocks/handlers`), so the UI runs **without the API and database**; solve progress then uses polling instead of SignalR. Fixtures are simplified and not persisted. |
| `googleMapsApiKey` | `""` | Optional. Empty → the centers page shows a **coordinates form** (latitude/longitude) instead of the map; everything else works. With a key, enable *Maps JavaScript API* and *Places API (New)* for it. |
| `googleMapsMapId` | `""` | Optional Map ID (needed for advanced markers when a key is set). |

### 4. Sample data

`samples/generated/small/participants.xlsx` — 300 participants, 11 groups, 6 needing an accessible seat.
In the UI: create a session → add 2 centers → add rooms (e.g. 8 rooms of 6 rows × 5 desks × 2 seats with
a few accessible seats, via "Кількість копій") → **Учасники → Імпорт з файлу** with this file →
add rules → **Розрахунок → Запустити розрахунок**. The step-by-step walk is in the
[user guide](docs/user-guide.md) and [`docs/smoke-test-phase2.md`](docs/smoke-test-phase2.md).
The import template is `samples/participants-template.xlsx` (also downloadable in the import wizard).

Other sizes (written to `samples/generated/<size>/`: `participants.xlsx`, centers/rooms/rules JSON, `solver-input.json`):

```bash
dotnet run --project api/tools/DataGen -- --size medium --out samples/generated   # small | medium | large
dotnet run --project api/tools/DataGen -- --size custom --participants 1000 --centers 4 --rooms-per-center 10 \
  --group-size 25..32 --needs-ratio 0.02 --seed 1 --out samples/generated        # → samples/generated/custom (git-ignored)
```

## Tests and checks

```bash
dotnet build api/ExamAllocation.sln         # zero warnings (warnings and StyleCop are errors)
dotnet test api/ExamAllocation.sln          # unit + integration; integration tests start Postgres via Testcontainers (Docker must run)
cd client && npm run ci                     # typecheck + lint + Jest + production build
```

End-to-end tests (Playwright, `client/e2e/`, added by the Phase 3 e2e workstream; run
`npx playwright install chromium` once):

```bash
cd client
npm run e2e:mocks                           # UI against MSW mocks — no API or database needed
npm run e2e:api                             # UI against the real API — needs steps 1–2 running
npm run e2e                                 # both suites
```

API contract (after changing DTOs or controllers):

```bash
cd client && npm run export:api && npm run gen:api   # refresh contracts/openapi.json and src/api/generated/schema.ts
```

## Benchmarks

```bash
dotnet run -c Release --project api/tools/Bench -- --size medium            # small | medium | large
dotnet run -c Release --project api/tools/Bench -- --size large --alternatives 5 --repeat 3 --workers 0
dotnet run -c Release --project api/tools/Bench -- --input samples/generated/small/solver-input.json
```

Bench solves a synthetic dataset in-process (no database), validates every result and prints a Markdown
table; with `--alternatives 0` (default) it fails when a run exceeds the target in
[`docs/04-solver.md` §12](docs/04-solver.md#12-performance-targets-release-build-8-cores) by more than 1.5×.
Measured results: [`docs/benchmarks.md`](docs/benchmarks.md).

## Git

History is linear and squash-only, with no AI attribution lines (see `CLAUDE.md` → Git workflow).
`npm install` in `client/` enables the hooks; otherwise run `git config core.hooksPath .githooks` once per clone.

## Status

Phases 0–3 are complete: all features on the real API, the solver within the docs/04 §12 targets
(large: 10 000 participants solved to optimality in seconds, see [`docs/benchmarks.md`](docs/benchmarks.md)),
Playwright e2e for mocks and the real API, infeasibility suggestions with deep links, and the
Ukrainian [user guide](docs/user-guide.md). Plan and reviews: [`docs/09-implementation-plan.md`](docs/09-implementation-plan.md),
[`docs/review-phase2.md`](docs/review-phase2.md), [`docs/review-phase3.md`](docs/review-phase3.md).
