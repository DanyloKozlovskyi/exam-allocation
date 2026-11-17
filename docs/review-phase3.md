# Phase 3 (Hardening) — independent review

Reviewed: branch `phase-3` (`d454df0`), i.e. `git diff main..phase-3` (7 commits, 110 files). Date: 2026-11-17.
Machine: 2 cores, 7 GB RAM, .NET 10.0.403, Node 22.23.4, Docker; the docs/04 §12 targets assume 8 cores.
The reviewer did not write this code. No source, test, config or doc file was changed except this one;
adversarial solver tests ran in a throw-away copy of `api/` in the scratchpad (not committed).

## Summary verdict

**Accept with fixes (no blocker).** Every gate is green: build with 0 warnings, 519 .NET tests, client `npm run ci`,
and e2e 2 of 2 on two runs. Medium and large are well within docs/04 §12 even on 2 cores. Every allocation
was valid, and large is proven optimal at the room-selection lower bound. The two-step stage 1 is sound:
- the room selection uses only necessary conditions, so its bound is a valid lower bound;
- the step E "accept if cost = LB and penalty 0" shortcut is a correct optimality proof;
- the fallbacks keep correctness, which adversarial inputs with soft R5/R2/R6, Center-scoped R2, accessible α
  and pins confirm.

Two Medium findings:
- The greedy assignment breaks hard **Center-scoped R2** across the rooms of one center, which docs/04 §4.1 says
  it respects. Correctness is kept only because step E re-checks the assignment. The cost is wasted
  steps E and A, which is a performance risk on large inputs.
- With alternatives on large, the loop stops after one 60 s stage-1 failure with
  `AlternativesLimited(TimeBudgetExhausted)` while half of the budget is left. It never tries the seat-level
  fallback, so 0 alternatives are returned.

The rest is Low or Info: contract and doc drift, stale comments, convention nits and e2e coverage gaps.

Finding counts: High 0 · Medium 2 · Low 10 · Info 4.

## 1. Benchmarks

Commands (Release, `--repeat 3` unless stated, run one at a time, nothing else running):
`dotnet run --project api/tools/DataGen -- --size medium --out samples/generated`, then
`dotnet run -c Release --project api/tools/Bench -- --size medium|large [--alternatives 3 --repeat 1]`.
DataGen medium: 2 000 participants, 70 groups, 10 centers, 80 rooms (3 922 seats), 4 rules (R2 group/Room hard,
R3 group hard, R4, R5 soft 0.5). Large: 10 000 participants, 400 rooms (19 611 seats).

### Best allocation only (`--alternatives 0`)

| Size | Run | Status | Total ms | Precheck | Stage 1 ms | Stage 2 ms | Rooms | Cost | Soft | Validator |
|---|---|---|---|---|---|---|---|---|---|---|
| Medium | 1 | Optimal | 9 406 | 440 | 666 | 8 094 | 36 | 41 100 | 0 | valid |
| Medium | 2 | Optimal | 2 757 | 6 | 185 | 2 530 | 36 | 41 100 | 0 | valid |
| Medium | 3 | Optimal | 2 232 | 9 | 56 | 2 104 | 36 | 41 100 | 0 | valid |
| Large | 1 | Optimal | 4 075 | 148 | 1 351 | 2 345 | 176 | 204 100 | 0 | valid |
| Large | 2 | Optimal | 2 586 | 9 | 653 | 1 850 | 176 | 204 100 | 0 | valid |
| Large | 3 | Optimal | 2 324 | 7 | 664 | 1 606 | 176 | 204 100 | 0 | valid |

Vars / constraints of the last model built: medium 3 760 / 452, large 90 208 / 2 320 (the step-E model with
fixed counts). Feedback iterations: 0 in every run. Both Bench invocations ended with `Result: Success`.

### Comparison with docs/04 §12 (targets for 8 cores) and docs/benchmarks.md

| Size | Column | §12 target | Measured (2 cores) | docs/benchmarks.md (integrator, quiet) | Verdict |
|---|---|---|---|---|---|
| Medium | Stage 1 | < 10 s | 0.06–0.67 s | 0.05–0.33 s | met, consistent |
| Medium | Stage 2 total | < 10 s | 2.1–8.1 s (run 1 cold, 8.1 s) | 2.1–3.6 s | met; cold run 1 close to the limit |
| Medium | Full run | < 30 s | 2.2–9.4 s | 2.2–4.3 s | met |
| Large | Stage 1 | < 60 s | 0.65–1.35 s | 0.54–1.27 s | met, consistent |
| Large | Stage 2 total | < 30 s | 1.6–2.3 s | 1.5–2.2 s | met, consistent |
| Large | Full run | < 120 s | 2.3–4.1 s | 2.1–3.8 s | met, consistent |

Status, rooms used (36 / 176) and cost (41 100 / 204 100) match docs/benchmarks.md exactly. All are `Optimal` and
valid. 2-core caveat: these are upper estimates for the 8-core targets. Stage 2 of medium run 1 (8.1 s) includes
JIT and warm-up. Stage 2 scales with cores (one CP-SAT worker per room in parallel), so it should drop on 8 cores.

### With alternatives (`--alternatives 3 --repeat 1`)

| Size | Status | Total ms | Stage 1 ms | Stage 2 ms | Alternatives ms | Allocations | Budget (2 × 60 s) |
|---|---|---|---|---|---|---|---|
| Medium | Optimal | 26 149 | 192 | 2 128 | 23 496 | 4 (best + 3), all valid | respected |
| Large | Optimal | 66 273 | 1 539 | 2 412 | 61 855 | **1 (no alternative)** | respected, but only half used |

Large: the stage-1 alternative (single §4 model, ~200 000 variables plus diversity terms) finds nothing in its
60 s. `TryStage1Alternative` then returns `OutOfTime`, and the loop ends with
`AlternativesLimited(Reason = TimeBudgetExhausted)` after 62 s of a 120 s budget. It never tries the seat-level
fallback, which costs only about 2 s per realization on large (see **P3-2**). docs/benchmarks.md "Open" records
"no alternatives on large". The reason it reports and the missing fallback are not recorded there.

## 2. Solver review (H1, docs/04 §4.1)

**Step S, room selection: valid lower bound.** (`RoomSelection.cs`)
- *Room types* use the same key as `AddSymmetryBreaking`: layout hash, cost and center when `CenterMatters`.
  Rooms with pins or cuts are singleton types, and `o_τ ≥ pinnedRooms_τ`.
- *Constraints*:
  - Σcap·o ≥ |P|, pins included.
  - Σacc·o ≥ |P_acc|. This is necessary because accessibility is unconditional in the non-explain model
    (`AddAccessibility` and the variable domains).
  - Σℓ·o ≥ max_v s_v and Σℓ^acc·o ≥ max_v s^acc_v. ℓ is the minimum of cap or acc, the hard Room-scoped R2 `m`
    and the hard R3 α / α^acc (`RoomValueLimits`).
  - Each of these is implied by the §4 hard constraints. Center-scoped R2, cuts and R6 are omitted, which only
    weakens the bound.
- *Bound and selected set*: when not proven, `LB = ceil(BestObjectiveBound)`, which stays valid. `R_S` is the
  first `o_τ` rooms of each type by index, consistent with the symmetry order.
- *Cost scale*: `Cost` uses the same per-room scaling as the model, and money has scale 2 in the database, so
  there is no rounding gap.

**Greedy assignment.** It respects capacity, accessibility, room-level R2 and R3 α / α^acc (pins seeded), and cuts.
It does **not** respect Center-scoped R2 across rooms. `allowed[r]` is computed once per class
(`GreedyAssignment.cs:63`), including `max − centerCount`, and placements decrement only `allowed[best]`
(`:83`). The other rooms of the same center keep their stale allowance, so a class can exceed `m` in a center.

*Adversarial test (scratch copy)*:
- Input: center X has 2 rooms of 10 at cost 1; center Y has 1 room of 10 at cost 5. Group A = 8, fillers
  F1–F3 = 4 each. Hard R2 Center max 5.
- `GreedyAssignment.Assign(selected = {X1, X2})` returns `Complete = true` with A 4 + 4 = 8 in center X.
- The pipeline still returns `Optimal`, valid, cost 6 (X1 + Y), via the infeasible step E → step A → step D.
  Stage 1 takes 188 ms against 2–12 ms for the other cases.
- The repo's `ShouldRespectHardLimitsPinsAndCutsInTheGreedyAssignment` passes only because the
  most-free-seats rule spreads class B over both centers. → **P3-1**.

**Step E shortcut: sound.** The evaluation model is the full §4 model over `R_S` (every hard constraint, R5,
soft R2 and R6 with free `z`), with every `n_{k,r}` fixed and `Cost ≥ LB`. If it is feasible with
`SoftPenalty = 0` and `Cost ≤ LB`, the solution:
- is feasible for stage 1;
- has cost equal to a global lower bound;
- has a soft penalty at its minimum (0).

Under `CostThenSoft`, that is lexicographically optimal. An infeasible greedy assignment (as in P3-1) is
rejected here, so correctness never depends on the greedy assignment.

**Fallbacks.**
- *Step A*: Optimal only if the cost equals LB and the soft part is proven.
- *Step C*: the full model at `Cost ≤ C*`, soft-only. It runs whenever the step-A penalty is > 0.
- *Step D*: the full model with `Cost ≥ LB`, valid because LB is a true bound.

Adversarial cases (real `AddSolver()` pipeline + `AllocationValidator`, all **valid**):

| Case | Shortcut fires? | Result |
|---|---|---|
| R5 0.8, 11 participants in 2 rooms of 10 (unavoidable shortage) | no (penalty > 0) | Optimal, cost 2, penalty 50 = validator |
| Soft R2 Room max 2, groups of 8 | no | Optimal, cost 2, penalty 8 = validator |
| R6 KeepTogetherInCenter (greedy splits groups) | no → step A finds 0 | Optimal, penalty 0 |
| Hard Center-scoped R2 (P3-1 input) | wrongly attempted, rejected | Optimal, cost 6 (fallback) |
| Same, `ObjectiveMode.Weighted` (single model) | n/a | Optimal, cost 6 |
| R3 with both accessible seats on one desk (α^acc = 1), 2 accessible group members | yes | Optimal, 2 rooms, valid |
| 3 pins in the dear room + R2 Room max 3 | yes / A | Optimal, valid |

**R-20 (alternatives budget and cuts):** fixed as described.
- `AlternativesRequest.Cuts` comes from the pipeline.
- A linked deadline token covers stage 1 and stage 2.
- The stage-2 per-room limit is capped at the remaining time (`Stage2Input`).
- Medium with 3 alternatives finished in 23.5 s of 120 s.
- Remaining gap: P3-2.

**R-21 (unexplained conflict core):** `ConflictExplainer.cs:32-36` reports `Unknown` (not cancelled) as
`ConflictCore` with every enforcement literal and `Reason = Unminimized`. It is covered by a test with the
`ProofTimeLimitSeconds` seam, and the client shows the note. A `FEASIBLE` explain model still yields no
diagnostic, as documented, and the UI then shows `diagnostics.unexplained`.

**R-22 (one CP-SAT parameter builder):** fixed. `CpSat.Parameters` was removed, and `Layout/CpSatParameters.Build`
is the only builder. The 0.05 s minimum moved there.

## 3. End-to-end (H2)

`cd client && E2E_CHROMIUM_EXECUTABLE=/opt/pw-browsers/chromium npm run e2e`, Postgres `exam-allocation-db` up:

| Run | mocks | real-api | Wall time |
|---|---|---|---|
| 1 | passed (50.7 s) | passed (54.0 s) | 2 m 8 s |
| 2 | passed (51.1 s) | passed (50.8 s) | 1 m 55 s |

No flakiness, and no leaked API calls in mocks mode (the spec asserts this). Playwright stopped its own servers.

**Coverage against docs/smoke-test-phase2.md, steps 1–13.** All 13 steps are walked: session, center, rooms with
accessible seats and create-bulk, xlsx import, three rules and precheck, solve with hub or polling transport,
overview / alternatives / export (4 formats return 200) / search, manual move with validate and apply, dashboard,
pin all and re-run, and the stale badge.

Gaps:
- Smoke step 2 adds a second center; the spec adds one.
- The FrontBack adjacency preview (step 3) is not asserted.
- The hub stage sequence of step 12 is not asserted; only `ProgressUpdated` and `RunCompleted` are. See P3-12.

**Conventions.**
- Test ids come from the feature `TestId` enums, UI texts from `src/i18n/uk/*.json` (`support/texts.ts`), and
  routes from `RoutePath`. Timeouts and ports are named in `support/constants.ts`.
- Remaining literals:
  - `'.MuiDataGrid-cell'` (`happy-path.spec.ts:243`).
  - The `data-transport` / `data-status` attribute names.
  - `MOCK_IMPORTED_PARTICIPANTS = 48` copied from `MOCK_IMPORT_VALID_ROWS` instead of imported (`:27`). See P3-11.

**Mock fixtures.** The mocks project passes the whole flow, with consistent totals, room counts, the session's
own participants in allocations, and staleness per input version. The pins mock implements list, delete-one and
`delete-all`.

## 4. Findings

| ID | Severity | Area | File:line | Description | Evidence | Suggested fix | Resolution |
|---|---|---|---|---|---|---|---|
| P3-1 | Medium | Solver H1 (greedy / step E) | `api/src/Solver/Allocation/GreedyAssignment.cs:63,83,151`; `docs/04-solver.md:275` | The greedy assignment does not respect hard **Center-scoped R2** across rooms of the same center. `allowed[r]` (including `max − centerCount`) is computed once per class, and a placement decrements only the chosen room. docs/04 §4.1 says it "respects every hard stage-1 constraint inside R_S". Correctness is preserved because step E rejects the infeasible fixed assignment. The cost: step E is wasted, step A gets an infeasible hint, and steps A→D can run. On large inputs, step A presolve alone is ~20 s and step D was the path that used to fail with `NoSolutionInTime`. | Scratch test: 2 rooms in center X, A = 8, R2 Center max 5 → `Complete = true`, 8 members of A in X. The pipeline goes through the fallback (stage 1 188 ms vs 2–12 ms) and still ends `Optimal`, valid. | After each placement, recompute (or decrement) `allowed[r']` for every room `r'` in the same center when the class has a Center-scoped limited value. Alternatively, check the center count inside the member loop instead of the cached allowance. Add a regression test with one center and several rooms. | | Fixed — after each placement the allowances of all rooms of that center are recomputed (`GreedyAssignment.Run`); docs/04 §4.1 says so. Test `ShouldKeepCenterSeparationAcrossRoomsOfOneCenterInTheGreedyAssignment` (fails without the fix: 6 in a center limited to 5). |
| P3-2 | Medium | Solver alternatives (R-20) | `api/src/Solver/Alternatives/AlternativesGenerator.cs:108-111,62-65` | When the stage-1 alternative returns `NoSolutionInTime` (not `Infeasible`), the attempt is `OutOfTime`. The loop stops with `AlternativesLimited(TimeBudgetExhausted)` even though about half of the `Stage1TimeLimitSeconds × 2` budget is left, and it never tries seat-level diversity (docs/04 §8 step 2). On large, requesting alternatives returns **0 alternatives** after 62 s, while a seat-level alternative needs about 2 s of stage 2. The reason is also misleading for the user. | Bench large `--alternatives 3 --repeat 1`: Alternatives 61 855 ms, Allocations 1 (budget 120 s). Code path: stage 1 status `NoSolutionInTime` → `OutOfTime` → `TimeBudgetExhausted`. | Treat a stage-1 `NoSolutionInTime` with budget left like `Exhausted` (switch to `SeatLevel` and continue). Report `TimeBudgetExhausted` only when `RemainingSeconds ≤ 0` or the deadline token fired. Consider capping the stage-1 alternative at a share of the budget so seat-level fallback has time. | | Fixed — a stage-1 alternative that ends `NoSolutionInTime` with budget left switches to seat-level diversity (docs/04 §8 step 2). Test `ShouldSwitchToSeatLevelWhenTheStage1AlternativeRunsOutOfTime` (fails without the fix). Bench large `--alternatives 3`: 0 → 1 alternative in the 120 s budget; medium 3/3. More large alternatives need the two-step idea in alternatives mode — deferred (docs/benchmarks.md "Open"). |
| P3-3 | Low | API contract (R-12) | `api/src/Api/Controllers/SolveRunsController.cs:53-56` | `POST solve-runs/{runId}/cancel` declares 202/404/409 only. Its command has a validator (400 `ValidationFailed`), and the handler returns `SolveRunUnknownException` (500). docs/05 says every action declares the statuses its error factory can return, and the R-12 Phase 3 note says "fixed — every action". Also, no read endpoint declares 500 (e.g. `GET sessions/{id}/pins`, `GET sessions/{id}/solve-runs`, `GET solve-runs/{id}/allocations`). | OpenAPI audit script over `contracts/openapi.json`: cancel `202,404,409`; 18 read operations without 5xx. | Add `ProducesResponseType<ValidationProblemDetails>(400)` and `<ProblemDetails>(500)` to cancel. Either declare 500 on reads or narrow the docs/05 wording to write endpoints. Re-export and regenerate. | | Fixed — cancel declares 400 and 500 too; docs/05 now states exactly what is declared (writes: every error-factory status; reads: 400 and 404, no 500). OpenAPI re-exported, client regenerated. |
| P3-4 | Low | Run failure reason (H3) | `api/src/Application/Solving/Mapping/SolverModelMapper.cs:93`; `api/src/Domain/SolveRuns/SolveRun.cs:95` | `SolveStatus.Failed` from the solver (e.g. `ModelTooLarge`, `InternalValidationFailed`) maps to a `Failed` run with reason `SolverError` by default and no `errorMessage`. The UI then shows "Помилка розв'язувача … зверніться до розробників", although `ModelTooLarge` is a user-fixable input-size problem whose diagnostic carries its own suggestion. | Code reading: the executor sets a reason only for `NoSolutionInTime` and for exceptions; `Complete` defaults Failed to `SolverError`. | Map a solver `Failed` run that has error diagnostics to a dedicated reason (e.g. `ModelTooLarge`, or a generic `InvalidInput`), or have the failure alert defer to the diagnostics when any are present. | | Fixed — new `SolveRunFailureReason.ModelTooLarge` set by the executor when the solver fails with a `ModelTooLarge` diagnostic; client explains it (fewer rooms/attributes) and links to the rules, no re-run / error detail. Client test added. |
| P3-5 | Low | Migration / data | `api/src/Infrastructure/Persistence/Migrations/20261117111137_SolveRunFailureReason.cs` | The new `failure_reason` column has no backfill. Failed runs that existed before the migration (including restart-interrupted ones) show as `SolverError` in the client, not `Interrupted` or `NoSolutionInTime`. | The migration only adds a nullable column; client `runFailureReason` falls back to `SolverError`. | Backfill via SQL from `error_message` (`Interrupted by server restart` → `Interrupted`, the no-solution text → `NoSolutionInTime`, other Failed rows → `SolverError`). | | Fixed — data migration `SolveRunFailureReasonBackfill` derives the reason from `error_message` for old failed runs; verified on the compose database in a rolled-back transaction (interrupted → Interrupted, no-solution → NoSolutionInTime, other → SolverError). |
| P3-6 | Low | Client participants filters | `client/src/components/Global/Participants/List/index.tsx:47-55` | URL filters (`search`, `group`, `school`, `accessible`) are read only into the initial `useState`. Navigating to the same route with other query params (e.g. the drawer "Учасники" item while a filtered list is open, or browser back/forward) does not reset the filters, and filter changes are not written to the URL. | Code reading: `useSearchParams()` values are used only as `useState` initialisers. | Derive the filter state from `searchParams` (and `setSearchParams` on change), or key the list component on `location.search`. | | Fixed in part — the list remounts when the URL query changes (`key={location.search}`), so drawer links, deep links and back/forward reset the filters. Writing filter edits back to the URL is deferred (convenience only). |
| P3-7 | Low | Client (stale comment) | `client/src/components/Global/SolveRuns/Common/diagnosticLinks.ts:87-88` | The comment says participants filters "are not deep-linkable yet … Links open the unfiltered list meanwhile", but the builders below now pass `group`, `school` and `accessible` filters. | File content. | Update the comment (only custom attributes remain unfiltered). | | Fixed — comment updated (only custom attributes remain unfiltered). |
| P3-8 | Low | Docs (user guide) | `docs/user-guide.md:253` | It says each diagnostic has links «Перейти до правила» / «Перейти до центру». Since H3, the panel shows a «Де виправити» list with labels such as «Правило «…»», «Аудиторії центру «…»», «Учасники за атрибутом …», «Зафіксовані учасники». The two quoted texts are only fallbacks when a name cannot be resolved. | `solveRuns.json` `diagnostics.link.*`, `linksLabel`; `DiagnosticsPanel.tsx:37-53`. | Describe the «Що зробити» / «Де виправити» block and the link kinds. | | Fixed — user guide §7 describes «Що зробити» / «Де виправити» and the link kinds. |
| P3-9 | Low | Docs (README) | `README.md:132-135` | The "Status" section still says Phase 3 is "in progress". | File content at `d454df0`. | Update it after the Phase 3 sign-off. | | Fixed — README status: Phases 0–3 complete, links to benchmarks, user guide and reviews. |
| P3-10 | Low | Client package | `client/package.json` (`engines`), `client/package-lock.json` | `engines.node >=22` was added to package.json, but the lockfile was not regenerated. `npm install` on a clean clone modifies `package-lock.json` (+3 lines `engines`), leaving the working tree dirty right after the README quick start. | Clean clone at `/tmp/qs-check`: `git status` after `npm install` → ` M client/package-lock.json` (diff: `"engines": {"node": ">=22"}`). | Run `npm install` once and commit the lockfile. | | Fixed — `package-lock.json` regenerated with `engines`; `npm install` on a clean clone leaves the tree clean. |
| P3-11 | Low | e2e conventions | `client/e2e/happy-path.spec.ts:27,243` | `MOCK_IMPORTED_PARTICIPANTS = 48` duplicates `MOCK_IMPORT_VALID_ROWS` (exported from `src/mocks/handlers/imports.ts`) instead of importing it, although the spec already imports from `src`. Inline selector `'.MuiDataGrid-cell'` and attribute names `data-transport` / `data-status` are literals, while `.MuiDataGrid-row` is a named constant in `support/fixtures.ts`. | File content. | Import `MOCK_IMPORT_VALID_ROWS`. Move the cell selector and attribute names to `support/fixtures.ts` / `constants.ts`. | | Fixed — `MOCK_IMPORT_VALID_ROWS` moved to a dependency-free `src/mocks/importFixture.ts` imported by the handlers and the spec; grid-cell selector and `DataAttribute` enum in `e2e/support/fixtures.ts`. |
| P3-12 | Low | e2e coverage | `client/e2e/happy-path.spec.ts` | Compared with docs/smoke-test-phase2.md: one center instead of two (step 2), no FrontBack adjacency preview (step 3), and the hub stage sequence Precheck→…→Done of step 12 is not asserted (only `ProgressUpdated` and `RunCompleted` are). | Spec vs smoke-test table. | Add a second center, and assert the ordered `SolveStage` values from `ProgressUpdated` frames in the real-API project. | | Fixed — the spec adds two centers (badge 1 → 2) and, in the real-API project, asserts the `ProgressUpdated` stage sequence never goes backwards and ends with `Done`. The FrontBack adjacency preview stays covered by the Rooms Jest test (deferred in e2e). Both projects green twice. |
| P3-13 | Info | Diagnostic deep links | `client/src/components/Global/SolveRuns/Common/diagnosticLinks.ts:16,27` | `CapacityTotalExceeded` and `PinRuleConflict` list `CenterRooms`, but the precheck never emits `CenterId` for them (`Precheck.cs:34-37, 217-287`), so that target is always empty. Harmless (the builder returns `[]`). | Grep of `DiagnosticFactory` calls. | Drop the target, or emit `CenterId` from the precheck. | | Fixed — `CenterRooms` dropped for `CapacityTotalExceeded` and `PinRuleConflict` (precheck never sends `CenterId` there); test fixtures no longer send it. |
| P3-14 | Info | Pins read path | `api/src/Api/Services/Implementation/PinsControllerService.cs:20-21` | `GET pins` loads every participant and every room of the session to name the pins, even for a handful of pins (10 000 participants on large). | Code reading. | Query only the pinned participant and room ids (projection), or join in `IPinQueries`. | | Fixed — `IParticipantQueries.GetByIds` / `IRoomQueries.GetByIds` (no seats); `GET pins` loads only the pinned participants and rooms. |
| P3-15 | Info | Frozen model / process | `api/src/Solver/Model/Internal.cs` (`AlternativesRequest.Cuts`) | `Solver/Model` is frozen after Phase 0 ("additive via change request"). The additive optional `Cuts` parameter has no change-request entry. H1 owns `api/src/Solver/**`, so this is process only. | `docs/change-requests.md` has no entry for it. | Record it in change-requests (resolved). | | Fixed — recorded in docs/change-requests.md as an additive [H1] entry (done). |
| P3-16 | Info | Client conventions | `client/src/components/Global/Pins/List/index.tsx:27-30,73-85` | Column widths are local `*_PX` constants (CLAUDE.md: sizes in `constants/ui.ts`), and DataGrid `field` names are string literals. Participants uses a `ParticipantColumn` enum for the same purpose. | File content. | Add a `PinColumn` enum, and move widths to `constants/ui.ts` or keep them as documented feature-local drawing constants. | | Fixed — `PinColumn` enum for DataGrid fields; widths stay feature-local drawing constants (commented), like other grids. |

Not defects (checked):
- *Contract drift*: none. The re-exported OpenAPI equals `contracts/openapi.json` (sorted JSON diff empty), and
  the regenerated `schema.ts` (with the client Prettier config) equals the committed file.
- *`GET solve-runs` read path*: correct.
  - One projected `GetIdsByRuns` query, in the same order as `GetByRun`.
  - The fingerprint is computed only when a finished run exists.
  - `isStale` is false for active runs, as docs/05 states.
  - UUID v7 ids keep manual versions in creation order.
- *Room-local group colours*: actually used for both the seat bars and the legend (`Allocations/Room/index.tsx`).
- *Pins page*:
  - `/sessions/:sessionId/pins` exists, with drawer item «Фіксації».
  - Remove one uses `DELETE pins/{participantId}`; remove all uses `POST pins/delete-all`.
  - Both invalidate the pins list and session-derived queries.
  - `PinDto` names are read-only (ignored by `ToUpsertCommand`).
  - The API returns 204, and 404 for an unknown session.
- *`SolveRunFailureReason`*: consistent across domain, configuration (string, max length), migration, DTO,
  OpenAPI enum and client enum. Restart recovery sets `Interrupted`.

## 5. docs/09 Phase 3 acceptance checklist

| Item | Acceptance | Evidence | Verdict |
|---|---|---|---|
| H1 Performance tuning; `docs/benchmarks.md` | medium & large within 04 §12 | §1: medium full run ≤ 9.4 s, large ≤ 4.1 s, stage 1 ≤ 1.4 s, all Optimal and valid; benchmarks.md present and matching | **Met.** P3-1 and P3-2 are open (greedy center limit, alternatives on large) |
| H2 Playwright e2e (mocks + real API) | green | §3: 2 of 2 tests, twice | **Met** (Low gaps P3-11, P3-12) |
| H3 Infeasibility UX (suggestions per `DiagnosticCode`, deep links) | tests | All 15 `DiagnosticCode`s have `diagnostics.code.*` and `diagnostics.suggestion.*`. `DIAGNOSTIC_LINK_TARGETS` is an exhaustive `Record` (typecheck-enforced). The failure-reason UI covers 3 reasons. `Diagnostics.test.tsx` and `SolveRuns.test.tsx` pass in `npm run ci` | **Met** (P3-4, P3-6, P3-7, P3-13) |
| H4 README quick start, `docs/user-guide.md` (Ukrainian) | reviewed | §8: clean-clone quick start works. User guide checked against i18n (§6) | **Met** (P3-8, P3-9, P3-10) |
| DoD: build zero warnings; tests green | | `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 71 + 169 + 135 + 144 = 519 passed | **Met** |
| DoD: `npm run ci` green | | typecheck, lint, Jest 16 suites / 96 tests, build | **Met** |
| DoD: no `NotImplementedException` / `StatusCode(501)` | | grep → no hits in `api/src` | **Met** |
| DoD: no literal unions / hard-coded routes / query keys | | lint green; greps for `\| '…'` unions, `'/sessions…'` literals and `queryKey: ['…']` in `client/src` and `client/e2e` → no hits | **Met** |
| DoD: docs updated; change requests resolved | | `grep -c "^- \[ \]" docs/change-requests.md` → 0; docs/04 §4.1, §7.2, §8, docs/05, docs/06, docs/08 updated | **Mostly met.** Doc drift: P3-1 (docs/04 greedy claim), P3-8, P3-9, P3-15 |

Deferred Phase 2 items:
- R-5 fixed (large Optimal).
- R-9 fixed (P3-4 and P3-5 are follow-ups).
- R-12 partially fixed (P3-3).
- R-20 fixed (P3-2 is a follow-up).
- R-21 fixed.
- R-22 fixed.
- R-24 fixed for the solve-runs path. The `AllocationEvaluator` double load noted in R-24 is not addressed and
  is not claimed.

## 6. Conventions sample (docs/10, CLAUDE.md)

| File | Result |
|---|---|
| `api/src/Solver/Allocation/RoomSelection.cs` | OK. Pure solver, uses `CpSatParameters`, no magic numbers. |
| `api/src/Solver/Allocation/GreedyAssignment.cs` | Style OK. Logic defect P3-1. |
| `api/src/Solver/Allocation/RoomValueLimits.cs` | OK. |
| `api/src/Solver/Allocation/Stage1Solver.cs` | OK. Time shares are named constants (`SelectionShare`, `AssignmentShare`). Step E reuses `SelectionShare` for its limit, as docs/04 says. |
| `api/src/Solver/Allocation/Stage1ModelBuilder.cs` | OK. `Stage1BuildOptions` record, symmetry key shared via `CenterMatters`. |
| `api/src/Solver/Allocation/ConflictExplainer.cs` | OK. `ConflictCoreReason` enum rendered by name, test seam `internal init`. |
| `api/src/Solver/Alternatives/AlternativesGenerator.cs` | OK (named `MinStage2Seconds`, `BudgetMultiplier`). Logic P3-2. |
| `api/src/Application/Pins/Commands/DeleteAllPinsCommand.cs` + `…Validator.cs` | OK. Command and handler in one file, `Either`, validator file, `CancellationToken`. |
| `api/src/Api/Controllers/PinsController.cs` | OK. `ApiRoutes.Pins.DeleteAll`, typed `ProducesResponseType`. |
| `api/src/Api/Controllers/SolveRunsController.cs` | Cancel is missing 400 and 500 (P3-3). |
| `api/src/Api/Services/Implementation/SolveRunsControllerService.cs` | OK. Reads via queries, no MediatR. |
| `api/src/Domain/SolveRuns/SolveRun.cs`, `SolveRunFailureReason.cs` | OK. Private setter, behaviour method, enum values explicit. |
| `api/src/Infrastructure/Persistence/Configurations/SolveRunsConfiguration.cs` | OK. Enum as string, `ColumnTypes.EnumMaxLength`. |
| `client/src/components/Global/Pins/List/index.tsx` | Mostly OK (`MutationKey`, `queryKeys`, `routes`, i18n, `PinsTestId`). P3-16. |
| `client/src/components/Global/SolveRuns/Common/diagnosticLinks.ts` | OK. Exhaustive `Record<DiagnosticCode, …>`, `routes.*`, enums. Stale comment P3-7. |
| `client/src/components/Global/SolveRuns/Common/FailureReasonAlert.tsx` / `failureReason.ts` | OK. Exhaustive `Record<SolveRunFailureReason, …>`, `FailureReasonAction` enum, i18n. |
| `client/src/components/Global/Allocations/Common/constants.ts` | OK. Named HSL constants. |
| `client/src/constants/routes.ts` | OK. `RoutePath.Pins`, `ParticipantsQueryParam` enum, `routes.pins.list`. |
| `client/src/mocks/handlers/pins.ts` | OK. `apiRoutes.*`, `HttpStatus`, `ErrorCode`, `MockPathParam`. |
| `client/e2e/happy-path.spec.ts` + `support/*` | Mostly OK (TestId enums, i18n texts, `RoutePath`). P3-11. |
| `client/src/enums/Pins.ts`, `client/src/enums/SolveRuns.ts` | OK. |

Owned-path discipline: H1 stayed in `api/src/Solver/**` and its tests, plus docs through change requests. The
integrator commits touched registration files (`Routes.tsx`), DTOs, the migration and `ApiRoutes`, all
recorded in `docs/change-requests.md`, except P3-15.

## 7. Quality gates

| Command | Outcome |
|---|---|
| `dotnet build api/ExamAllocation.sln --no-incremental` | success, **0 warnings, 0 errors** |
| `dotnet test api/ExamAllocation.sln` | Domain 71, Solver 169, Infrastructure 135, Api.Tests.Integration 144: **519 passed, 0 failed** |
| `cd client && npm run ci` | typecheck, lint, Jest **16 suites / 96 tests passed**, vite build passed |
| `cd client && E2E_CHROMIUM_EXECUTABLE=/opt/pw-browsers/chromium npm run e2e` (×2) | **2/2 passed** both times |
| `grep -rn "NotImplementedException\|StatusCode(501)" api/src` | no hits |
| greps: literal unions (`\| '…'`), `'/sessions…'` route literals, `queryKey: ['…']`, hard-coded `localhost`/ports in `client/src` (excluding generated, mocks, tests) | no hits |
| `grep -c "^- \[ \]" docs/change-requests.md` | **0** |
| OpenAPI re-export to a temp file + `openapi-typescript` regeneration | identical to the committed `contracts/openapi.json` and `schema.ts` |

## 8. Clean-clone quick start (README, followed literally)

`git clone /home/claude/p2 /tmp/qs-check && cd /tmp/qs-check && git checkout phase-3` (`d454df0`).

| Step | Command | Outcome |
|---|---|---|
| 1 | `docker compose up -d db` | **Name clash**: `/exam-allocation-db` already exists (the reviewer's running instance, from the original checkout). Expected on a machine with another checkout, so the running container was reused (same credentials and port). On a truly clean machine this step creates the container. |
| 2 | `dotnet run --project api/src/Api` | Builds and starts, migrations applied. `GET /health` 200, `/swagger/index.html` 200, `GET /sessions` 200. CORS preflight from `http://localhost:3000` returns 204. |
| 3 | `cd client && npm install` | OK (1 m 16 s). The hooks are enabled (`core.hooksPath = .githooks`). There are deprecation warnings and 26 moderate `npm audit` findings, which are informational. **Side effect: `client/package-lock.json` is modified (P3-10).** |
| 3 | `npm run build` (in addition) | OK |
| 3 | `npm run dev` (via `npx vite --port 3000 --strictPort`) | `http://localhost:3000` serves `index.html` (`lang="uk"`). `src/config.json` defaults match the README table. |
| 4 | Sample data | `samples/participants-template.xlsx` and `samples/generated/small/solver-input.json` exist. The README `DataGen --size custom …` command works (1 000 participants, 40 rooms). |

No README command failed or was missing. The Playwright note (`npx playwright install chromium`) was not run, per
the review instructions. All servers started for this check were stopped afterwards.

**User guide vs UI labels.** 312 bold or «»-quoted labels were extracted from `docs/user-guide.md`. After
normalising the quotes, 248 match a string in `client/src/i18n/uk/*.json` verbatim.

Sample of ≥ 15 labels confirmed to match a UI string:
«Нова сесія», «Створити сесію», «Підготовка до розрахунку», «Останній розрахунок», «Кількість копій»,
«Аркуш і заголовок», «Відповідність колонок», «Обов'язкове», «Бажане», «Перевірити здійсненність»,
«Запустити розрахунок», «Ручне редагування», «Перевірити зміни», «Зберегти як ручну версію»,
«Штрафи за бажаними правилами», «Зафіксувати все», «Зняти всі фіксації», «Зафіксовані учасники», and the four
pin diagnostic titles.

The rest are explanatory or illustrative text, not missing labels:
- Seat-state descriptions written in lowercase in i18n.
- The example "Ауд. 214".
- XLSX sheet names «Зведення» / «Усі учасники», which are in `Infrastructure/ImportExport/Constants/ExportSheets.cs`.

One drift: P3-8.

## 9. Integrator final verification (after the review fixes, `aa5847e`)

| Gate | Outcome |
|---|---|
| `dotnet build api/ExamAllocation.sln` | **0 warnings, 0 errors** |
| `dotnet test api/ExamAllocation.sln` ×3 consecutive | Domain 71, Solver 171, Infrastructure 135, Api.Tests.Integration 144: **521 passed** on each run |
| `cd client && npm run ci` | typecheck, lint, Jest **97 tests passed**, vite build passed |
| `npm run e2e` ×2 consecutive | run 1: mocks ✓ 56.9 s, real-api ✓ 54.4 s; run 2: mocks ✓ 53.5 s, real-api ✓ 55.6 s |
| Stub / magic-string greps (§7) | no hits; open change requests **0** |
| Clean clone of `phase-3` | `npm install` leaves the tree clean (P3-10 fixed); hooks enabled; the API starts with migrations applied (`/health` healthy, `/swagger` 200, `POST /sessions/create` 200); `npm run dev` serves `http://localhost:3000`. `docker compose up -d db` hit the same container-name clash as §8 (another checkout's container), which was reused. |
| Bench after P3-2 | medium `--alternatives 3`: 3/3 alternatives in 28 s; large: 1 alternative within the 120 s budget (was 0), with `AlternativesLimited` explaining the rest |
