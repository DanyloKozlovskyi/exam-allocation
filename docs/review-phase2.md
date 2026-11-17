# Phase 2 — Independent review (I7)

Reviewer: independent agent (did not write the Phase 2 code). Date: 2026-11-15.
Scope: `git diff main..phase-2` (5 commits, 141 files), the solver against docs/04, the Phase 2
integration code (solve lifecycle, hub, allocation endpoints, export builder, staleness), contracts and
conventions (docs/10).

## Summary verdict

Phase 2 is functionally complete and in good shape. The build has 0 warnings, all 489 .NET tests and
66 client tests pass, `npm run ci` is green, there is no `NotImplementedException` or 501 stub left in
`api/src`, and the committed `contracts/openapi.json` and `client/src/api/generated/schema.ts` match
what the current code generates. The solver follows docs/04 closely. Every allocation the benchmarks
produced passed the independent validator. I found **no High-severity defects**.
**Five Medium findings** need attention:
- The "single active run" rule can be bypassed by concurrent requests. I reproduced this: 10 parallel
  POSTs created 10 runs.
- After a manual edit, the client labels the manual allocation as "best".
- The SignalR client does not re-subscribe after an automatic reconnect.
- `pin-all` issues one query and one save per participant and is not atomic.
- On this 2-core machine, large-instance stage 1 uses its whole time limit and returns a
  solution that opens every room. Stage 1 also misses the docs/04 §12 targets on medium and large.

The remaining items are Low/Info: doc/code drift, small semantic mismatches and test gaps.
Recommendation: fix R-1 to R-3 (each is small) before or with the Phase 2 squash-merge, and schedule the
rest for Phase 3.

## Benchmarks

Machine: **2 CPU cores** (`nproc` = 2). docs/04 §12 targets assume 8 cores. All numbers below are as
measured here, Release build, `NumWorkers = 0` (= 2 CP-SAT workers), 3 repeats, alternatives 0.

DataGen (Debug, as in CLAUDE.md):

```
$ dotnet run --project api/tools/DataGen -- --size medium --out samples/generated
Generated medium (seed 1): 2000 participants, 70 groups, 10 centers, 80 rooms (3922 seats), 4 rules → /home/claude/p2/samples/generated/medium
```

### Medium — `dotnet run -c Release --project api/tools/Bench -- --size medium`

Participants: 2000 · Rooms: 80 · Seats: 3922 · Rules: 4 · Alternatives: 0 · Workers: 0 · Repeat: 3 · Target: 30 s (fail > 45 s)

| Run | Status | Total ms | Precheck ms | Stage 1 ms | Stage 2 ms | Feedback it. | Alternatives ms | Classes | Vars | Constraints | Allocations | Rooms used | Cost | Soft penalty | Validator |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Optimal | 18543 | 159 | 14309 | 3998 | 0 | 0 | 100 | 8160 | 11660 | 1 | 36 | 41100 | 0 | valid |
| 2 | Optimal | 18622 | 2 | 14935 | 3662 | 0 | 0 | 100 | 8160 | 11660 | 1 | 36 | 41100 | 0 | valid |
| 3 | Optimal | 17239 | 8 | 13555 | 3629 | 0 | 0 | 100 | 8160 | 11660 | 1 | 36 | 41100 | 0 | valid |

Result: Success

### Large — `dotnet run -c Release --project api/tools/Bench -- --size large`

Participants: 10000 · Rooms: 400 · Seats: 19611 · Rules: 4 · Alternatives: 0 · Workers: 0 · Repeat: 3 · Target: 120 s (fail > 180 s)

| Run | Status | Total ms | Precheck ms | Stage 1 ms | Stage 2 ms | Feedback it. | Alternatives ms | Classes | Vars | Constraints | Allocations | Rooms used | Cost | Soft penalty | Validator |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Feasible | 75695 | 200 | 63417 | 11856 | 0 | 0 | 508 | 204000 | 282361 | 1 | 400 | 480000 | 15050 | valid |
| 2 | Feasible | 72855 | 7 | 62814 | 9971 | 0 | 0 | 508 | 204000 | 282361 | 1 | 400 | 480000 | 15050 | valid |
| 3 | Feasible | 71576 | 6 | 61922 | 9602 | 0 | 0 | 508 | 204000 | 282361 | 1 | 400 | 480000 | 15050 | valid |

Result: Success

### Comparison with docs/04 §12 (8-core targets vs 2-core measurements)

| Size | Column | Target | Measured (min–max) | Verdict |
|---|---|---|---|---|
| Medium | Classes | ~80 | 100 | more classes than expected (70 groups × accessibility split) — informational |
| Medium | Stage 1 | < 10 s | 13.6–14.9 s | **missed** (2 cores; proven optimal) |
| Medium | Stage 2 total | < 10 s | 3.6–4.0 s | met |
| Medium | Full run | < 30 s | 17.2–18.6 s | met |
| Large | Classes | ~400 | 508 | informational |
| Large | Stage 1 | < 60 s | 61.9–63.4 s | **missed**: equals the 60 s `Stage1TimeLimitSeconds`; status `Feasible` (not optimal) |
| Large | Stage 2 total | < 30 s | 9.6–11.9 s | met |
| Large | Full run | < 120 s | 71.6–75.7 s | met |

**Integrator re-measurement after the R-5 stage-1 change** (same machine, 2 cores, Bench medium ×5):
Stage 1 5.2–7.2 s (< 10 s, **met**), Stage 2 3.5–4.6 s (met), full run 8.7–12.0 s (met), all `Optimal`,
all valid. Large: unchanged (Stage 1 hits its limit; full run 75 s < 120 s) — reported, deferred to H1.

All 6 benchmark allocations are reported **valid** by `AllocationValidator`. Quality caveat for large:
stage 1 stops at its time limit with all 400 rooms open (cost 480 000, R5 penalty 15 050), although
about 205 rooms of the 19 611 seats would hold 10 000 participants (see R-5). Re-run on an 8-core
machine before drawing conclusions about the targets.

## Findings

Severity: High = wrong results / data loss under normal use; Medium = real bug or contract violation
with limited blast radius, or a significant quality/performance gap; Low = edge case, doc/code drift
with small impact; Info = observation, no change required.

| ID | Severity | Area | File:line | Description | Evidence | Suggested fix | Resolution |
|---|---|---|---|---|---|---|---|
| R-1 | Medium | Solve lifecycle / concurrency | `api/src/Application/SolveRuns/Commands/StartSolveRunCommand.cs:35,47-53,61` | The single-active-run rule is a check-then-insert with no lock or DB constraint. Concurrent `solve-runs/create` requests for one session all pass `EnsureNoActiveRun` and each creates a run. The window includes `fingerprint.Compute`, which loads the whole session. | Live API on a throwaway Postgres: 10 parallel `POST sessions/{id}/solve-runs/create` → 10 × `200`, and `GET …/solve-runs` returned 10 runs. Several were created while an earlier run was still `Queued`/`Running` (e.g. created 22:13:37.324 while the run created at 37.157 finished only at 37.389). The existing test `ShouldRejectSecondActiveRunAndCancelQueuedRun` sends the requests one after the other. | Add a partial unique index `solve_runs(session_id) WHERE status IN ('Queued','Running')` (migration) and map the unique violation to `SolveRunAlreadyActiveException`. Alternatively, take a per-session `pg_advisory_xact_lock` (or a `SemaphoreSlim` keyed by session in a singleton) around check + insert. | | Fixed — partial unique index `ix_solve_runs_session_id_active` (migration `SolveRunActiveIndex`) + `ISolveRunRepository.TryAddActive` → 409 on a lost race. Tests `ShouldAllowOnlyOneActiveRunPerSessionInTheDatabase`, `ShouldAnswerConcurrentCreatesWithOneRunOrConflict`; live: 6 parallel creates → 1×200, 5×409. |
| R-2 | Medium | Client / contract | `api/src/Infrastructure/Persistence/Repositories/AllocationRepository.cs:28`; `client/src/components/Global/SolveRuns/View/index.tsx:134`; `client/src/components/Global/Sessions/View/index.tsx:30`; `client/src/components/Layout/navigation.ts` (`allocationIds?.[0]`) | `SolveRunDto.allocationIds` is ordered by `Index` ascending. A manual allocation has `Index = -1` (`AllocationConstraints.ManualIndex`), so after `apply-moves` it comes first. The client treats position 0 as the best allocation: the run view labels the manual version "Найкраще" and numbers the real best as 1, and the dashboard and drawer "Результати" links open the manual version. Every further apply-moves adds another `-1` entry. | Code: `GetByRun … .OrderBy(x => x.Index)`; `ApplyMovesCommand.cs:62` uses `ManualIndex`; client `index === 0 ? t('view.bestAllocation') …` and `const bestAllocationId = lastRun?.allocationIds?.[0]`. | Either order `allocationIds` so solver allocations come first (`Index >= 0` by index, then manual by `CreatedAt`), or expose the index (e.g. `AllocationSummaryDto[]` on `SolveRunDto`) and select `index === 0` in the client. | | Fixed — `GetByRun` orders solver allocations by index, manual versions last; run view labels by `AllocationSummaryDto.index`. Test `ShouldKeepBestAllocationFirstAfterManualVersion`. |
| R-3 | Medium | Client SignalR | `client/src/hooks/useSolveProgress.ts:51,75` | The hook uses `withAutomaticReconnect()` but has no `onreconnected` handler. After a reconnect the connection id is new and the server-side group membership (`HubGroups.Run`) is gone, so no further `ProgressUpdated`/`RunCompleted` arrive. `onclose` does not fire on a successful reconnect, so the polling fallback never starts and the view can stay in "Running" until the user reloads. | Repository-wide search: no `onreconnected`/`onreconnecting` handler anywhere in `client/src`. In Hub transport, `useSolveRunQuery` is created with `poll: false`. | In `connection.onreconnected(() => connection.invoke(HubMethod.SubscribeToRun, runId))` re-subscribe (the hub replays last progress/completion). Optionally also refetch the run, and poll while `onreconnecting` is in progress. | | Fixed — `onreconnected` re-invokes `SubscribeToRun` (hub replays last progress / `RunCompleted`); failure falls back to polling. Client test covers the reconnect. |
| R-4 | Medium | Allocations / pin-all | `api/src/Application/Allocations/Commands/PinAllCommand.cs:44,49,62` | For each allocation item the handler runs `GetByParticipant` plus `UpdateAsync`, and every `UpdateAsync` calls `SaveChanges` and clears the change tracker. That is up to 2·N round trips (10 000 participants → about 20 000 statements) with no transaction, so a failure part-way leaves a half-pinned session. It also does not apply the seat-uniqueness rule that `UpsertPinCommand` enforces (`PinConflictException`, `UpsertPinCommand.cs:104-109`). With a `roomIds` filter, a participant outside the filter who is pinned to a seat in a filtered room keeps that pin, and a second pin is created on the same seat; the next precheck then fails with `PinDuplicateSeat`. | Code reading. The integration test `ShouldServeSeatingRowsMovesPinsAndExport` covers only `PinAllDto(null)` on a clean session. | Load all session pins once (tracked), update or create them in memory, remove or report pins of other participants that hold a target seat, and call `SaveChanges` once inside `ITransactionRunner`. | | Fixed — one tracked load + one `SaveChanges` (`IPinRepository.GetAllTracked` / `SaveBatchAsync`, atomic); conflicting pins of participants outside the batch are removed. Test `ShouldPinRoomWithoutDoublePinningASeat`. |
| R-5 | Medium (Needs confirmation on 8 cores) | Solver performance / quality | `api/src/Solver/Allocation/Stage1Solver.cs:57-77` (CostThenSoft) | On large, stage 1 uses the full 60 s and returns `Feasible` with all 400 rooms open (cost 480 000), although about 205 rooms would hold the 10 000 participants and nothing in the rules forces more (R2 `maxPerScope = 2` and the R3 α-bound allow ≥ 2 per group per room). The cost phase (36 s at 2 workers) does not improve on its first solution. Stage 1 also misses the §12 target on medium (13.6–14.9 s vs < 10 s). | Large and medium Bench tables above (`Rooms used` 400 / 400; `Stage 1 ms` ≈ 62 000 on large and ≈ 14 000 on medium). | Re-measure on 8 cores. If it reproduces: hint the cost phase with a greedy fill (cheapest rooms first, respecting R2/α), or warm-start with a fixed `y_r` from a capacity-only LP; also consider `num_search_workers ≥ 8` even on fewer cores. | | Medium fixed, large deferred. Stage 1 now states the covering knapsacks `Σ cap_r·y_r ≥ |P|`, `Σ acc_r·y_r ≥ |P_acc|` and gets a greedy `y` hint (docs/04 §4): Bench medium on 2 cores — Stage 1 5.2–7.2 s (< 10 s), Stage 2 3.5–4.6 s, full run 8.7–12.0 s, all Optimal and valid. Large unchanged (run 1 NoSolutionInTime at 40 s; runs 2–3 Feasible in ~75 s with all 400 rooms open, full run < 120 s, Stage 1 > 60 s) → Phase 3 H1 (8-core measurement; candidates: complete hint consistent with symmetry, room aggregation by identical type). Tried and reverted: 3/4/8 workers, load-ordering symmetry breaking, `linearization_level:2`, `interleave_search`, y-linked R2/α bounds, complete greedy hint. |
| R-6 | Low | Solver vs docs/04 §4 | `api/src/Solver/Allocation/Stage1ModelBuilder.cs:285-296`; `api/src/Solver/Allocation/PinFacts.cs` | docs/04 §4 (accessibility refinement) says pinned accessible participants count on the left of `Σ n_{k,r} ≤ α^acc_r`. The code bounds only class loads (`RoomLoad(room, accessibleClasses) ≤ accessibleAlpha`), and `PinFacts` has no per-(value, room) accessible count. Pinned needs-accessible members of a value can therefore push stage 2 into infeasibility, and the feedback loop (which cuts the largest value, not necessarily this one) then has to recover. | Crafted input (`Bench --input`): room A with 2 accessible seats on one desk (α^acc = 1), room B with 2 non-adjacent accessible seats, group G = {P1 pinned to A (room only), P2}, both needing accessible seats, and hard R3 `SameDesk`. Result: **1 feedback iteration**, `Optimal`, valid. The same input without the pin needs 0 feedback iterations. | Add `PinFacts.AccessibleValue(key, value, room)` (room-only pins, or seat pins on accessible seats) and include it in the bound and in the `accessibleUpper < 2` skip. Otherwise, update docs/04 §4 to match the code. | | Fixed — `PinFacts.AccessibleValue` (pinAcc_{a,v,r}) added to the accessible α-bound; docs/04 §4 updated. Test `ShouldCountPinnedAccessibleGroupmatesInTheAccessibleBound` (fails without the fix: 1 feedback iteration). |
| R-7 | Low | Solver vs validator (R5) | `api/src/Solver/Allocation/Stage1ModelBuilder.cs:306` vs `api/src/Solver/Validation/AllocationValidator.cs:224` | Stage 1 computes the R5 target as `⌈ratio·cap⌉`; the validator and docs/04 §10 use `⌈ratio·cap − 1e-9⌉`. Floating-point noise makes them differ for some ratio/capacity pairs, so the stage-1 R5 penalty stored in `ObjectiveBreakdown` (and shown in the UI) can differ from the validator penalty. That validator value is also what `apply-moves` stores for manual allocations. | Python check over ratios 0.01–0.99 and capacities 1–199: 22 pairs differ, e.g. 0.28 × 25 → stage 1 target 8, validator 7; 0.14 × 50; 0.07 × 100. | Use the same tolerance constant in stage 1 (e.g. a shared `SolverDefaults.FillRatioTolerance`). | | Fixed — stage 1 uses `⌈ratio·cap − SolverDefaults.FillRatioTolerance⌉` like the validator; docs/04 §4 updated. |
| R-8 | Low | Solve lifecycle / cancellation | `api/src/Application/Solving/Services/SolveRunExecutor.cs:29,43-47`; `api/src/Application/SolveRuns/Commands/CancelSolveRunCommand.cs:41-50` | Lost-cancel race: the executor reads the run as `Queued` and registers its token. If cancel arrives before `MarkRunning` is saved, the handler sees `Queued`, skips the token, and writes `Cancelled` (and notifies `RunCompleted(Cancelled)`). The executor then saves `Running` over it, because `SolveRun` has no concurrency token, and runs to `Succeeded`. The window is small (ms) but real. | Code reading. `SolveRunsConfiguration` has no `IsRowVersion()`, unlike other aggregates. | Map `xmin` as a concurrency token on `SolveRun`, or let the cancel handler call `cancellation.Cancel(run.Id)` whatever the status (it returns true once registered), and let the executor re-check status after `MarkRunning` with a conditional update. | | Fixed — `SolveRun.Version` (xmin) + `ISolveRunRepository.TryUpdate`; executor skips a run changed before start, cancel falls back to the token when the worker won. |
| R-9 | Low | Restart recovery vs docs/02 | `api/src/Infrastructure/Persistence/ApplicationDbContextInitialiser.cs:13,41,46` | docs/02 §Solve flow: "runs left `Running` → `Failed` with `ErrorCode.RunInterrupted`". The code also fails `Queued` runs (reasonable, because the channel is in memory) and stores the English text "Interrupted by server restart" in `ErrorMessage`. `ErrorCode.RunInterrupted` is never used. `SolveRunExecutor.NoSolutionInTimeMessage` is also English user-visible text. | `grep RunInterrupted` → only the enum declaration. Test `ShouldFailInterruptedRunsOnStartup`. | Expose a machine-readable reason (e.g. an `ErrorCode` field on `SolveRunDto`, or a diagnostic) and update docs/02 to include `Queued`. The client then renders Ukrainian text through i18n. | | Deferred — exposing a machine-readable reason needs a `SolveRunDto` contract field; belongs with Phase 3 H3 (infeasibility UX). docs/02 now describes the actual behaviour (Queued and Running → Failed, message text). |
| R-10 | Low | Client ↔ API limits | `client/src/components/Global/SolveRuns/Common/constants.ts:40`; `client/src/components/Global/SolveRuns/RunDialog/index.tsx:80,128` vs `api/src/Domain/SolveRuns/SolveRunOptions.cs:11` | The client accepts a time limit ≥ 1 s, but the API requires ≥ `MinTimeLimitSeconds` = 5, so 1–4 s gives a 400 `ValidationFailed`. Seed accepts up to `Number.MAX_SAFE_INTEGER`, but the API field is `int` (binding fails above 2³¹−1). Integer fields are parsed with `parseFloat`, so 2.5 alternatives also fails binding. | Code reading. | Mirror the backend constants (5 s minimum, int32 seed maximum) and validate integers with `Number.isInteger`. | | Fixed — client mirrors the API: time limit ≥ 5 s, seed ≤ int32, integer fields checked with `Number.isInteger`. |
| R-11 | Low | Client progress stepper | `client/src/components/Global/SolveRuns/Common/constants.ts:25-33` | `SOLVE_STAGE_ORDER` lists `Alternatives` before `Expanding`, but the pipeline reports `Expanding` before `Alternatives` (docs/04 §8a steps 5→7; `SolverPipeline.cs:114,130`; smoke test step 12). The stepper goes backwards from Expanding to Alternatives on every run that computes alternatives. That is the same symptom the smoke test fixed for nested Stage1/Stage2. | Code and the smoke-test stage sequence "Precheck → Stage1 → Stage2 → Expanding → Alternatives → Done". | Swap the two entries. In docs/04 §1, note that the `SolveStage` enum order is not the reporting order. | | Fixed — stepper order Expanding → Alternatives; docs/04 §1 notes enum order ≠ reporting order. |
| R-12 | Low | API contract | `api/src/Api/Controllers/SolveRunsController.cs:50`; `contracts/openapi.json` | `POST solve-runs/{runId}/cancel` returns `202` (docs/05), but OpenAPI declares only `200`, because there is no `[ProducesResponseType]`. No Phase 2 endpoint declares its 4xx responses (404/409/400). | OpenAPI dump: `POST /solve-runs/{runId}/cancel … ['200']`. | Add `[ProducesResponseType(StatusCodes.Status202Accepted)]` (and the 4xx ProblemDetails types), then re-export and regenerate the client. | | Fixed for cancel (202/404/409 declared, OpenAPI re-exported). Declaring 4xx on every endpoint is a project-wide convention change (no controller uses `ProducesResponseType`) — deferred to Phase 3. |
| R-13 | Low | API read semantics | `api/src/Api/Services/Implementation/AllocationsControllerService.cs:108-110` | `GET allocations/{unknownId}/participants` returns `200 {items: [], total: 0}` instead of `404`, unlike `GET allocations/{unknownId}` (404). docs/05 read convention: `None` → `NotFound()`. | Live: `allocations/0000…0001/participants -> 200 {"items":[],"page":1,"pageSize":50,"total":0}`; `allocations/0000…0001 -> 404`. | Return `Option<PagedDto<…>>` and map `None` to 404. | | Fixed — `Participants` returns `Option`; unknown allocation → 404. Test `ShouldReturnNotFoundForRowsOfUnknownAllocation`. |
| R-14 | Low | Docs drift (docs/05) | `docs/05-api.md:20,93`; `api/src/Api/Controllers/AllocationsController.cs:43-52`; `api/src/Api/Dtos/SolveRunsDtos.cs:35` | docs/05 says allocation rows take `sortBy` and `DiagnosticDto` has `data`. The code has no `sortBy` on `allocations/{id}/participants` (it always sorts by name) and the field is `arguments`. | OpenAPI: parameters `['allocationId','page','pageSize','search','sortDirection']`; `DiagnosticDto` properties `['code','severity','message','ruleId','arguments']`. | Update docs/05 (or add `sortBy`). | | Fixed — docs/05 updated (`sortBy` only for participants; `arguments`). |
| R-15 | Low | Staleness | `api/src/Application/SolveRuns/Commands/StartSolveRunCommand.cs:59` vs `api/src/Application/Solving/Services/SolveRunExecutor.cs:68` | `InputHash` is computed when the run is enqueued, but `SolverInput` is built later when the worker picks the run up. Runs queue behind other sessions' runs because there is one worker. An edit made in between yields a run whose data does not match its hash, so `isStale` is wrong in both directions. | Code reading. | Compute the fingerprint in the executor from the same snapshot (or from `SolverInput`) and store it with `MarkRunning`. | | Fixed — executor re-computes the fingerprint and stores it with `MarkRunning(now, inputHash)` right before building the solver input. |
| R-16 | Low | Resource handling | `api/src/Solver/Seating/SeatingSolver.cs:258`; `api/src/Solver/Layout/IndependentSetCalculator.cs:106` | `CpSolver` is `IDisposable` (stage 1 and the conflict explainer use `using var solver`), but stage 2 (one per room per realization) and the α calculator never dispose it, which leaves native response buffers to the finalizer. | Code reading. | Use `using var solver = new CpSolver { … }`. | | Fixed — `using var solver` in `SeatingSolver` and `IndependentSetCalculator`. |
| R-17 | Low | Tests | `api/tests/Api.Tests.Integration/SolveRuns/SolveRunsControllerTests.cs` | Not covered by tests: cancelling a **Running** run (token path → `Cancelled`); `apply-moves` without `force` on a hard violation (409 `MoveViolatesHardRule`); `pin-all` with `roomIds`; concurrent create (R-1). The smoke test says "cancel … covered elsewhere by integration tests", but only the Queued path is. `api/tests/Api.Tests.Integration/Allocations/` (an owned path) does not exist; allocation tests live under `SolveRuns/`. | `grep "public async Task Should"` in `SolveRuns/` lists 8 tests; none cancels a running run or exercises the 409 path. | Add the four tests (a slow scenario, or a fake `ISolverPipeline` that waits on the token, makes Running-cancel deterministic). | | Fixed — added running-run cancel (token path), 409 `MoveViolatesHardRule` + force, pin-all with `roomIds`, concurrent creates, unknown-allocation 404; allocation tests moved to `Api.Tests.Integration/Allocations/`. |
| R-18 | Low | Config | `api/src/Application/Common/Settings/ApplicationSettings.cs:17-29` | `Solver:Stage1TimeLimitSeconds`, `Solver:DefaultAlternatives` and `Solver:MaxConcurrentRuns` are bound from appsettings but never read. Defaults come from `SolveRunOptions` constants, and the worker is always single. Editing appsettings has no effect. | `grep` shows no consumer outside `ApplicationSettings.cs`. | Use them (`SolveOptionsDto.ToOptions` defaults, worker degree) or remove them from settings and docs/02. | | Fixed — unused `MaxConcurrentRuns`, `Stage1TimeLimitSeconds`, `DefaultAlternatives` removed from settings, appsettings and docs/02 (per-run options carry them). |
| R-19 | Info | Docs drift (docs/08) | `docs/08-testing.md:87-88` | The doc still says Bench uses `MinimalServiceProvider` without `Microsoft.Extensions.DependencyInjection`; the change request removed it in Phase 2. | `api/tools/Bench/MinimalServiceProvider.cs` deleted in the diff. | Update docs/08 §Benchmarks. | | Fixed — docs/08 Benchmarks updated. |
| R-20 | Info | Solver alternatives | `api/src/Solver/Alternatives/AlternativesGenerator.cs:88-131,134-154`; `api/src/Solver/Pipeline/SolverPipeline.cs:132` | (a) The feedback cuts learned for the best allocation are not passed to the alternatives generator, so each stage-1 alternative can rediscover them, costing up to `MaxFeedbackIterations` re-solves. (b) The `Stage1TimeLimitSeconds × 2` budget bounds only stage-1 solves; stage 2 and seat-level fallback realizations are not checked against it, so alternatives can overrun the budget by several × (rooms × 5 s / cores). | Code reading. | Pass the main pipeline's cuts in `AlternativesRequest`, and check `RemainingSeconds` (and cap stage-2 per-room time) before each realization. | | Deferred to Phase 3 H1 — passing cuts to alternatives and budgeting stage 2 changes solver behaviour/time budgets; no correctness impact (alternatives are validated). |
| R-21 | Info | Solver conflict core | `api/src/Solver/Allocation/ConflictExplainer.cs:27-31` | If the assumption solve does not prove infeasibility within `Stage1TimeLimitSeconds` (e.g. `UNKNOWN`), `Explain` returns no diagnostics, so the run ends `Infeasible` with an empty diagnostics list. | Code reading. | Emit a fallback `ConflictCore` with all hard-rule literals (or a dedicated "unexplained" diagnostic). | | Deferred to Phase 3 H3 — fallback "unexplained" diagnostic is part of the infeasibility UX work; the run still ends `Infeasible` and precheck diagnostics remain available. |
| R-22 | Info | Conventions | `api/src/Solver/Allocation/CpSat.cs:13-18` and `api/src/Solver/Layout/CpSatParameters.cs:8-13` | Two CP-SAT parameter builders (WS-A and WS-B) duplicate the parameter names and `EXAM_SOLVER_LOG` constants. Behaviour is identical. | Code reading. | Keep one (e.g. `Solver/Layout/CpSatParameters`) and delete the other. | | Deferred to Phase 3 H1 (owns `Solver/**`) — duplication only, behaviour identical. |
| R-23 | Info | Folder placement vs docs/02 | `api/src/Application/SolveRuns/Commands/*` | docs/02 places `StartSolveRun`/`CancelSolveRun`/`RunPrecheck` under `Application/Solving/Commands`; the code uses `Application/SolveRuns/Commands` (matches the docs/09 feature table). | `ls`. | Align docs/02. | | Fixed — docs/02 tree aligned (`SolveRuns/Commands`, `Solving/Services`). |
| R-24 | Info | Read-path cost | `api/src/Api/Services/Implementation/SolveRunsControllerService.cs:16-55`; `api/src/Application/Allocations/Services/AllocationEvaluator.cs:259-279` | Every `GET solve-runs/{id}` (the polling fallback, every 1–2 s) and every `RunCompleted` push recomputes the session fingerprint by loading all rooms, seats, participants, rules and pins. `GET sessions/{id}/solve-runs` runs one allocation query per run (N+1). `AllocationEvaluator.Load` loads rooms and participants twice (once directly, once through `SolverInputBuilder`). | Code reading. | Cache the fingerprint per session (invalidate on writes) or compute it once per list call, add a single `GetByRuns` allocation query, and reuse the loaded entities when building `SolverInput`. | | Deferred to Phase 3 H1 — read-path performance (fingerprint caching, N+1); correct today, measured cost acceptable for MVP data sizes. |
| R-25 | Info | Reference material | `/mnt/user-data/uploads/pal-suite/{api,client}` | The pal-suite reference supplied for this review contains only `api/.editorconfig` and `client/.prettierrc`. Both are byte-identical to the repo copies. Conventions were therefore checked against docs/10 and CLAUDE.md, not pal-suite source. | `ls -la` and `diff`. | — | | No change — informational; conventions were checked against docs/10 and CLAUDE.md. |

## Conventions sample

Checked against docs/10 and CLAUDE.md (pal-suite source code was not available, see R-25).
Lint rules for literal unions, `prefer-enum-initializers` and `no-magic-numbers` are enforced by
ESLint and pass.

| # | File | Layer | Result |
|---|---|---|---|
| 1 | `api/src/Application/SolveRuns/Commands/StartSolveRunCommand.cs` | Command + handler | ✅ record command, handler in the same file, `Either<SolveRunException, SolveRun>`, `BindAsync` chain, unknown exception wrapped. Concurrency issue R-1. |
| 2 | `api/src/Application/SolveRuns/Commands/StartSolveRunCommandValidator.cs` | Validator | ✅ separate file, limits from `SolveRunOptions` constants (`MinRatio`/`MaxRatio` local consts acceptable) |
| 3 | `api/src/Application/SolveRuns/Commands/CancelSolveRunCommand.cs` (+ `…Validator.cs`) | Command | ✅ Either, validator present. Race R-8. |
| 4 | `api/src/Application/Allocations/Commands/ApplyMovesCommand.cs` | Command | ⚠ Either returned, but uses `moved.Match(_ => throw …)` unwrapping instead of a `Bind` chain (style only); validator only checks `AllocationId` (empty `Moves` allowed → creates a copy) |
| 5 | `api/src/Application/Allocations/Commands/PinAllCommand.cs` | Command | ⚠ Either + validator OK; N+1 / no transaction / no seat-uniqueness (R-4) |
| 6 | `api/src/Application/SolveRuns/Exceptions/SolveRunExceptions.cs` | Exceptions | ✅ hierarchy with `ErrorCode` on each type |
| 7 | `api/src/Api/Modules/Errors/SolveRunErrorFactory.cs`, `AllocationErrorFactory.cs` | Error factories | ✅ NotFound → 404, AlreadyActive/NotCancellable/ViolatesHardRule → 409, InvalidMove/Argument → 400, Unknown → 500, ProblemDetails with `code` (verified live) |
| 8 | `api/src/Infrastructure/Persistence/Repositories/SolveRunRepository.cs` | Repository + queries | ✅ extends `RepositoryBase`, implements both interfaces, queries `AsNoTracking` + `Option`. `GetAllActive` is unused (dead code). |
| 9 | `api/src/Infrastructure/Persistence/Repositories/AllocationRepository.cs` | Repository + queries | ✅ registered three times (concrete + both interfaces). Ordering issue R-2. |
| 10 | `api/src/Api/Controllers/SolveRunsController.cs` | Controller | ✅ writes via `ISender`, reads via `ISolveRunsControllerService`, routes from `ApiRoutes`. Missing `ProducesResponseType` (R-12). |
| 11 | `api/src/Api/Controllers/AllocationsController.cs` | Controller | ✅ same pattern. ⚠ `ValidateMoves` (read-like) goes through MediatR, which docs/05/09 allow as a POST command. |
| 12 | `api/src/Api/Services/Implementation/AllocationsControllerService.cs` | Controller service | ⚠ returns `Option`/lists correctly except `Participants` (R-13); filtering and paging done in memory |
| 13 | `api/src/Api/Dtos/SolveRunsDtos.cs` | DTOs | ✅ records, `FromDomain`/`ToOptions` manual mapping, enums as Domain enums (string-serialized) |
| 14 | `api/src/Api/Hubs/SolveProgressHub.cs`, `SignalRSolveProgressNotifier.cs`, `Api/Constants/HubRoutes.cs` | Hub | ✅ method names and groups from `SolveProgressHubMethods`/`HubGroups`, last-progress replay, `RunCompleted` on late subscribe |
| 15 | `api/src/Application/Solving/Services/SolveRunExecutor.cs` | Application service | ⚠ English user-visible constant `NoSolutionInTimeMessage` (R-9); otherwise primary-ctor DI, `CancellationToken` everywhere |
| 16 | `api/src/Infrastructure/Persistence/ApplicationDbContextInitialiser.cs` | Infrastructure | ⚠ English `InterruptedRunMessage` instead of `ErrorCode.RunInterrupted` (R-9) |
| 17 | `client/src/hooks/useSolveProgress.ts` | Client hook | ⚠ hub constants from `constants/hub.ts` (enums ✅), but invalidates with an inline `[QueryKey.SolveRuns, sessionId]` instead of `queryKeys.solveRuns.list(sessionId)`; missing reconnect handling (R-3) |
| 18 | `client/src/components/Global/Queries/allocations.ts` | Client queries | ✅ `queryKeys.*`, `apiRoutes.*`, `ApiQueryParam` enum, typed DTOs from `api/dtos` |
| 19 | `client/src/components/Global/Allocations/Common/mutations.ts` | Client mutations | ✅ endpoints via `apiRoutes` |
| 20 | `client/src/components/Global/SolveRuns/Common/constants.ts` | Client constants | ⚠ enums from the generated schema ✅; limits drift from the backend (R-10); stage order (R-11) |
| 21 | `client/src/enums/SolveRuns.ts` | Client enums | ✅ string enums with initializers, `TestId` enum |
| 22 | `client/src/components/Layout/navigation.ts` | Client layout | ✅ enum keys, `routes.*` builders; uses `allocationIds[0]` (R-2) |
| 23 | `client/src/components/Global/SolveRuns/RunDialog/index.tsx` | Client component | ✅ `MutationKey`, i18n keys as enum, test ids as enum; limits drift (R-10) |

No string-literal union types, no hand-edited generated files (regeneration diff is empty), no new
packages in the Phase 2 diff.

## Quality gates

| Command | Outcome |
|---|---|
| `dotnet build api/ExamAllocation.sln` | **Build succeeded, 0 warnings, 0 errors** |
| `dotnet test api/ExamAllocation.sln` (Docker/Testcontainers) | **All passed**: Domain.Tests 71, Solver.Tests 159, Infrastructure.Tests 133, Api.Tests.Integration 126 (489 total, 0 failed, 0 skipped) |
| `cd client && npm run ci` (typecheck + lint + jest + build) | **Passed**: 13 suites, 66 tests; lint clean; Vite build OK; no warnings in output |
| `grep -rn "NotImplementedException\|StatusCode(501)" api/src` | **No matches** (also none for `Status501`) |
| OpenAPI drift: `dotnet run --project api/src/Api -- --export-openapi <tmp>` vs `contracts/openapi.json` | **Identical** (normalized JSON diff empty) |
| Client schema drift: `openapi-typescript … --enum --enum-values` + prettier vs `client/src/api/generated/schema.ts` | **Identical** |
| Live API probe (throwaway Postgres on port 55432, API on 5099; removed afterwards) | Error codes and status codes as documented (404 `SolveRunNotFound`, 409 `SolveRunNotCancellable`, 400 `ValidationFailed` with `code`). R-1 and R-13 reproduced. |

## Phase 2 acceptance checklist (docs/09, verified by the integrator)

| Task | docs/09 acceptance | Evidence | Result |
|---|---|---|---|
| I1 | full build + tests green; change requests applied | `dotnet build` 0 warnings; `dotnet test` 507 green on 3 consecutive runs; `npm run ci` 66 green; `docs/change-requests.md` 0 open entries (29 done, 4 deferred with reasons, 1 resolved in Phase 1); OpenAPI re-exported, client types regenerated | ✅ |
| I2 | mapping tests | `Solving/SolverInputBuilderTests`: Domain → `SolverInput` (rooms/layout/cost, participants + group attribute, enabled rules only, pins with seat keys, options + server settings), `SolverOutput` → allocations with seat ids, unknown seat rejected, status mapping, diagnostic round trip | ✅ |
| I3 | integration: small session → `Succeeded`, allocations persisted | `ShouldSolveAndPersistAllocations`; lifecycle: `ShouldRejectSecondActiveRunAndCancelQueuedRun`, `ShouldAllowOnlyOneActiveRunPerSessionInTheDatabase`, `ShouldAnswerConcurrentCreatesWithOneRunOrConflict`, `ShouldCancelRunningRunThroughItsToken`, `ShouldFailInterruptedRunsOnStartup` | ✅ |
| I4 | `HubConnection` test receives `RunCompleted` | `ShouldPushCompletionToSubscriberDuringRun`, `ShouldReplayCompletionToLateHubSubscriber` | ✅ |
| I5 | tests (read endpoints, validate/apply moves, pin-all, export, isStale) | `ShouldServeSeatingRowsMovesPinsAndExport`, `ShouldMarkSessionStaleAfterRelevantChange`, `Allocations/AllocationsControllerTests` (409 + force, manual ordering, pin-all with rooms, 404) | ✅ |
| I6 | manual smoke of the happy path (real API) | `docs/smoke-test-phase2.md`: 13 steps passed, re-run after each fix round | ✅ |
| I7 | independent review, defects fixed or listed | this file: 25 findings, Resolution column filled | ✅ |
| §12 | Bench medium within targets; large reported | medium Stage 1 5.2–7.2 s, Stage 2 3.5–4.6 s, full 8.7–12.0 s (2 cores) ✅; large reported (Stage 1 over 60 s, full run ~75 s) → Phase 3 H1 | ✅ / reported |

While writing the I2 tests, the integrator also found and fixed that the integration-test host ignored
`appsettings.Test.json` (settings were bound in `Program` before the factory's deferred configuration);
`IntegrationTestWebFactory` now passes it with `UseConfiguration`.
