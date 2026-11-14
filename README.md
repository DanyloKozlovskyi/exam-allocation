# exam-allocation

Web application for allocating exam participants to test centers, rooms and seats under
placement constraints — e.g. groupmates may not share a room or sit at adjacent desks.

- Pick test centers on Google Maps, define room layouts (rows, desks, seats, aisles)
- Import participants from Excel/CSV
- Configure hard and soft placement rules
- Compute an optimal allocation plus diverse alternatives (OR-Tools CP-SAT), or get an
  explanation of conflicting rules
- Review results on the map and on seating charts, adjust manually, export XLSX / XML / CSV / PDF

**Stack:** .NET 10 · Clean Architecture · MediatR (CQRS) · FluentValidation · LanguageExt ·
EF Core + PostgreSQL · Google OR-Tools · SignalR · React + TypeScript + Vite · MUI ·
TanStack Query · Google Maps JS API

## Documentation

See [`docs/`](docs/README.md) — overview, architecture, data model, solver design, API,
client, import/export, testing, implementation plan and coding conventions.
Agent rules: [`CLAUDE.md`](CLAUDE.md).

## Quick start

Prerequisites: .NET 10 SDK, Node 22+, Docker (for Postgres and integration tests).

```bash
docker compose up -d db                     # Postgres 17 on localhost:5432 (exam / exam)
dotnet run --project api/src/Api            # http://localhost:5080 — applies migrations on start, Swagger at /swagger
cd client && npm install && npm run dev     # http://localhost:3000 (MSW mocks on by default: src/config.json → useMocks)
```

Git: history is linear and squash-only (see `CLAUDE.md` → Git workflow). `npm install` in `client/`
enables the repo's git hooks; or run `git config core.hooksPath .githooks` once per clone.

Checks:

```bash
dotnet build api/ExamAllocation.sln         # zero warnings (StyleCop as errors)
dotnet test api/ExamAllocation.sln          # unit + integration (Testcontainers needs Docker)
cd client && npm run ci                     # typecheck + lint + jest + build
cd client && npm run export:api && npm run gen:api   # refresh contracts/openapi.json and generated TS types
```

Status: **Phase 0 (foundation) complete** — all layers, contracts and DI are in place; feature logic is stubbed
(`501` / `NotImplementedException`) for the Phase 1 workstreams in `docs/09-implementation-plan.md`.
