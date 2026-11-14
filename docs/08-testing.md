# 08 — Testing & Benchmarks

## Principles

1. **The validator is the oracle.** Solver tests never assert exact seats (CP-SAT with
   multiple workers is not deterministic); they assert `AllocationValidator` returns valid
   and objective values on small instances solved to optimality.
2. **Each workstream ships its own tests**; a task is not done until its acceptance
   commands (09) pass.
3. **No network in tests.** Google Maps is never called in tests; Postgres via Testcontainers.

## Backend

xUnit + **FluentAssertions 7.x** (pal-suite; 8.x is commercially licensed). Test method
names `Should<Behaviour>`; Arrange/Act/Assert comments as in pal-suite.

| Project | Scope | Notes |
|---|---|---|
| `Domain.Tests` | Guards, value objects (`SeatKey`, `RoomLayout`, `GeoPoint`, `AttributeKey`), entity behaviour methods | plain xUnit |
| `Solver.Tests` | Seat generation, adjacency per kind, α computation, classes, stage 1, stage 2, feedback, precheck codes, conflict core, alternatives diversity, expander, validator | Uses `TestInputBuilder` fluent helper (Phase 0) |
| `Infrastructure.Tests` | Reader (xlsx/csv/encodings), mapping suggester, row validation codes, XLSX export structure, XML against XSD, CSV encoding, PDF non-empty | Fixtures in `tests/.../Fixtures/` |
| `Api.Tests.Integration` | Controller tests per feature folder: CRUD, 404/409/400 `ProblemDetails` with `ErrorCode`, import flow, solve run lifecycle (small input), SignalR `RunCompleted`, export content types | pal-suite style: `BaseIntegrationTest` + `IntegrationTestWebFactory` (Testcontainers PostgreSQL), arrange in `InitializeAsync`, cleanup in `DisposeAsync`, data from `Tests.Data` factories, `ToResponseModel<T>()` |

Must-have solver test cases:

- Adjacency: 2×2 desks, 2 seats/desk, aisle after column 0 → exact edge sets per kind.
- Classes merge participants that differ only in attributes not used by rules.
- Hard R2 `maxPerScope=1` with 3 groups × 4 people, 4 rooms × 3 seats → valid; with 3 rooms → `SeparationImpossible`.
- Hard R3 on a 1-row room of 4 single-seat desks, 1 group of 3 → infeasible (α=2) → precheck `NoAdjacencyImpossible` if only room; with 2 rooms → valid.
- Accessibility: needs-accessible participants only on accessible seats.
- Pins: pinned seat respected; pin conflicting with hard rule → `PinRuleConflict`.
- Conflict core: total capacity is sufficient, but hard R2 `group`/Room (max 2) combined with hard R2 `group`/Center (max 3) makes a large group unplaceable → status `Infeasible`, `ConflictCore` contains both rule ids.
- Alternatives: N=3 on medium instance → 3 distinct allocations, each valid, pairwise moved ≥ D participants (or seat-level distinct).
- Cost-first objective: with spare capacity, rooms used = ⌈participants / roomCap⌉ when no other rules.
- Cancellation: token cancelled during stage 1 → status `Cancelled` within 2 s.

## Frontend

- Unit/component: Jest + ts-jest (jsdom) + Testing Library + MSW (pal-suite uses Jest).
- `npm run typecheck`, `npm run lint` must be clean.
- e2e (Phase 3): Playwright against MSW mocks (`VITE_USE_MOCKS=true`), happy path in 06.

## Synthetic data — `api/tools/DataGen`

CLI: `--size small|medium|large|custom --participants N --centers C --rooms-per-center R --group-size 25..32 --needs-ratio 0.02 --seed 1 --out <dir>`

Outputs to `<dir>/<size>/`:
- `participants.xlsx` (template format, Ukrainian names from embedded lists),
- `participants.json`, `centers.json` (Kyiv-area coordinates), `rooms.json` (mixed layouts:
  6×5×2, 5×4×2, 8×6×1 with aisles), `rules.json` (default rule set: R2 `group`/Room hard
  with `maxPerScope` = ⌈groupSize / roomsAvailable⌉ + 1; R3 `group` hard with SameDesk +
  Side + FrontBack; R4; R5 soft 0.5),
- `solver-input.json` (direct `SolverInput` for the benchmark and solver tests).

Committed: `samples/generated/small/*` (used by MSW fixtures and tests). Medium/large are
generated on demand (gitignored).

Implementation notes (WS-V):
- Generator code lives in `api/tools/Shared/` and is compiled into both DataGen and Bench
  (`<Compile Include="..\Shared\**\*.cs" />`), so Bench can generate the same input in memory.
- Deterministic per `--seed` (ids are seed-derived GUIDs; xlsx document timestamps are fixed).
- Presets: small 300/2 centers/6 rooms per center, medium 2 000/10/8, large 10 000/50/8; layouts
  cycle 6×5×2 (aisle after column 1), 5×4×2 (aisle after 1), 8×6×1 (aisles after 1 and 3); two
  accessible seats per room (front row, outer desks, seat 0); every 3rd room has its last seat disabled;
  center costs 1000 + 100·(i mod 5).
- JSON files are camelCase with enums as strings and mirror the API DTO shapes (`TestCenterDto`,
  `RoomDto`, `ParticipantDto`, `RuleDto`). Rule parameters use the `"type": "<RuleType>"`
  discriminator (same as Domain/DTOs) via `RuleParametersJsonConverter`; `IReadOnlySet<string>` is
  handled by a dedicated converter. DataGen verifies that `solver-input.json` round-trips.
- `solver-input.json` participants carry both `group` and `school` attributes (only `group` is
  referenced by the default rules — exercises class merging). `Options.RandomSeed` = `--seed`.

## Benchmarks — `api/tools/Bench`

CLI: `--size small|medium|large --alternatives 0 --repeat 3 --workers 0`.
Prints a Markdown table: stage timings (precheck, stage 1, stage 2, feedback iterations,
alternatives), model sizes (classes, vars, constraints), objective, validator result.
Exit code ≠ 0 if validation fails or time exceeds the target in 04 §12 by > 50 %.

Extra options: `--input <solver-input.json>` (load instead of generate; no time target unless
`--size` is also given), `--seed N`. The time target applies only to best-only runs
(`--alternatives 0`). Exit codes: `0` success, `1` validation failed, `2` pipeline not implemented
(`NotImplementedException` stubs), `3` time over target × 1.5, `4` not solved (status not
Optimal/Feasible or no allocation), `5` unexpected solver exception, `64` usage error.
The validator column also flags a mismatch between the validator's soft penalty and
`Objective.SoftPenalty` (warning only).
Bench does not reference `Microsoft.Extensions.DependencyInjection`; it resolves the real
`AddSolver()` registrations with a minimal constructor-injecting resolver (`MinimalServiceProvider`).

Results of each benchmark run worth keeping go to `docs/benchmarks.md` (append).
