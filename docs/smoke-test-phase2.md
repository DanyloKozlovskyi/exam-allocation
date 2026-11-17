# Phase 2 — real-API smoke test (I6)

Date: 2026-11-14. Branch `phase-2`. Client `useMocks=false`.

## Setup

| | |
|---|---|
| Database | `docker compose up -d db` (postgres:17, healthy) |
| API | `dotnet run --project api/src/Api` → `http://localhost:5080` (migrations applied on start) |
| Client | `cd client && npm run dev` → `http://localhost:3000` |
| Driver | Playwright (Chromium, 1440×1000), one script per step, screenshots at each checkpoint |
| Data | `samples/generated/small/participants.xlsx` (300 participants, 11 groups of 25–32, 6 need an accessible seat) |

docs/06 has no separate "happy path" section; the walk follows its feature order:
session → centers → rooms → participant import → rules → precheck / solve with live progress →
results (overview, alternatives, participants, export) → room seating with a manual move →
pin all → re-run → staleness and dashboard.

## Steps and results

| # | Step (UI only, real API) | Result |
|---|---|---|
| 1 | Sessions → "Нова сесія" stepper (Details → Summary) → create | ✅ session created, dashboard opens, drawer shows Огляд / Центри / Учасники / Правила / Розрахунок |
| 2 | Центри → map fallback coordinates form → center wizard (name, address) → create; a second center the same way | ✅ both centers listed (list refreshes ~0.5 s after navigation); drawer badge "Центри 2" |
| 3 | Center row → Аудиторії → room wizard: 6×5×2, two seats marked accessible on the interactive grid, FrontBack adjacency preview → summary → "Створити аудиторії (8)" (`create-bulk`) | ✅ 8 rooms listed; "Аудиторії" drawer entry appears inside the center; API totals 480 seats / 16 accessible |
| 4 | Учасники → Імпорт: upload xlsx → sheet/header row → mapping (suggested) → validation → mode → commit | ✅ validation 300 valid / 0 errors; commit "Додано: 300"; participants grid filled; badge 300 |
| 5 | Правила → wizard ×3: R2 SeparateByAttribute (group, Room, ≤ 4, hard), R3 NoAdjacentByAttribute (group; SameDesk/Side/FrontBack, hard), R4 AccessibleSeating | ✅ 3 rules stored with the expected typed parameters; badge 3; precheck "Проблем не виявлено" |
| 6 | Розрахунок → run dialog (precheck clean) → start → run view | ✅ `Succeeded` in 3.8 s; transport "Оновлення в реальному часі" (SignalR); 43 `ProgressUpdated` + `RunCompleted`; 6 allocation links (best + 5 alternatives) |
| 7 | Best allocation → overview | ✅ centers map section (fallback without a Maps key, fill-coloured markers), 8 room fill bars, 6 alternative tabs, "Бажані правила виконано без штрафів" |
| 8 | Export menu → each format fetched from the API | ✅ XLSX 200 (45.8 kB), XML 200 (102 kB), CSV 200 (54.9 kB), PDF 200 (108.6 kB), correct content types |
| 9 | Учасники tab → server-side search by surname | ✅ "Березовська" → 7 rows |
| 10 | Room → seating chart → "Ручне редагування": move r0c0s0 → free r5c1s0 → validate-moves → apply-moves | ✅ 39 occupied / 21 free seats, violation legend "Порушень немає"; validate "Зміни не порушують обов'язкових правил. Штраф: 0."; manual version saved and opened |
| 11 | Dashboard | ✅ "Останній розрахунок: Успішно, <date>, Відкрити / Результати"; no stale warning |
| 12 | Allocation → "Зафіксувати все" (`pin-all`) → re-run dialog → start | ✅ 300 pins saved; re-run `Succeeded`; hub stage sequence Precheck → Stage1 → Stage2 → Expanding → Alternatives → Done |
| 13 | Розрахунок list | ✅ 2 runs; the first carries the stale badge (input changed by the pins) |

Covered elsewhere by integration tests (not repeated in the browser): cancel, single active run (409),
infeasible run + precheck diagnostics, hub late-subscriber replay, restart recovery.

## Issues found and fixed during the smoke test

| Issue | Fix |
|---|---|
| Room and rule mutations refreshed only their own lists: drawer badges, center room counts/capacity and run staleness stayed stale until reload | `Queries/invalidation.ts` → `invalidateSessionInputs` (totals, centers, solve runs by prefix), called after room, rule, participant, import and pin mutations |
| During alternatives the nested stage-1/stage-2 solves reported `Stage1`/`Stage2` progress after `Alternatives`, so the run view's stage stepper jumped backwards | `AlternativesGenerator` passes `SilentProgress` to nested solves; regression test `ShouldReportOnlyAlternativesProgressOnceAlternativesStart` |

Not a defect: CORS allows `http://localhost:3000` only (configured origin); browsing the dev server via
`127.0.0.1:3000` is rejected by design.

Observation (left for Phase 3 UX): group colours come from a hash into a 12-colour palette, so with 11
groups a few share a near-identical colour on the seating chart.

## Re-run after the review fixes (2026-11-17)

Same steps 1–13 on a fresh session, after the R-1…R-18 fixes and migration `SolveRunActiveIndex`:
all passed. Solve `Succeeded` in 3.9 s with 6 allocations; exports XLSX/XML/CSV/PDF 200; manual move
validated (penalty 0) and saved; `pin-all` → 300 pins in one batch; re-run `Succeeded`; first run
marked stale. `solve-runs/{id}/allocations` after the manual edit returns indexes `0,1,2,3,4,5,-1`
(best first, manual last). Six parallel `solve-runs/create` against the same database → one 200,
five 409.

Re-run after the stage-1 performance change (covering knapsacks + greedy hint, review R-5):
all 13 steps passed again on a fresh session; first solve `Succeeded` in 5.0 s with 6 allocations.
