# Benchmarks

Solver timings measured with `api/tools/Bench` on the synthetic datasets of `api/tools/DataGen`
(docs/08 "Synthetic data"). Targets: docs/04 §12 (8 cores).

## Machine

| | |
|---|---|
| CPU | Intel Xeon @ 2.10 GHz, **2 cores** (1 thread per core) |
| RAM | 7 GB |
| Runtime | .NET 10.0.403, Release build, Google.OrTools 9.15.6755 |
| Load | **shared and noisy**: three other agents built and tested in parallel (load average 4–5 on 2 cores). Treat every number as indicative; the integrator re-measures on a quiet machine. |

`Workers: 0` = `Environment.ProcessorCount` = 2 for stage 1; stage 2 runs rooms in parallel with one worker each.

## Datasets (DataGen, seed 1)

| Size | Participants | Groups | Classes | Centers | Rooms | Seats | Rules |
|---|---|---|---|---|---|---|---|
| Small | 300 | – | 17 | 2 | 12 | 588 | R2 group/Room hard, R3 group hard, R4, R5 soft 0.5 |
| Medium | 2 000 | – | 100 | 10 | 80 | 3 922 | same |
| Large | 10 000 | 350 | 508 | 50 | 400 | 19 611 | same (R2 `maxPerScope` = 2) |

Rooms use three layouts (6×5×2 = 60, 5×4×2 = 40, 8×6×1 = 48/47 seats, two accessible seats each),
center costs 1 000–1 400 per room (5 levels), hence ~15 room types on large.

## Results (best allocation only, `--alternatives 0`)

### Before (Phase 2 code, `phase-3` base)

| Size | Runs | Status | Stage 1 ms | Stage 2 ms | Total ms | Rooms used | Cost |
|---|---|---|---|---|---|---|---|
| Small | 3 | Optimal ×3 | 60–123 | 338–422 | 412–684 | 8 | 8 200 |
| Medium | 3 | Optimal ×3 | 7 891–11 585 | 3 453–8 975 | 11 398–20 765 | 36 | 41 100 |
| Large | 2 | **NoSolutionInTime ×2** | 38 773–39 738 | – | 38 802–39 986 | – | – |

(Phase 2 integrator, same machine but quieter: medium stage 1 5.2–7.2 s; large NoSolutionInTime once and
`Feasible` twice in ~75 s with **all 400 rooms open, cost 480 000**.)

### After (H1: two-step stage 1, docs/04 §4.1)

| Size | Runs | Status | Stage 1 ms | Stage 2 ms | Total ms | Rooms used | Cost | Soft penalty | Validator |
|---|---|---|---|---|---|---|---|---|---|
| Small | 3 | Optimal ×3 | 11–124 | 384–483 | 469–723 | 8 | 8 200 | 0 | valid |
| Medium | 3 | Optimal ×3 | 42–227 | 2 019–2 626 | 2 147–3 090 | 36 | 41 100 | 0 | valid |
| Large | 3 | **Optimal ×3** | 736–1 490 | 1 843–3 876 | 2 719–4 767 | **176** | **204 100** | 0 | valid |

All runs are within docs/04 §12 on 2 cores (large: stage 1 < 60 s, stage 2 < 30 s, full < 120 s).
The first run of each size includes JIT/warm-up (precheck ~150 ms).

**Lower bound / optimality.** Large is `Optimal`: stage 1's cost equals the room-selection lower bound
`LB` = optimum of `min Σ_τ cost_τ·o_τ` over the room types `τ` subject to the covering constraints
of docs/04 §4.1 (seats ≥ participants, accessible seats ≥ needs, `ℓ_τ`-weighted rooms ≥ largest
group for the hard R2/R3 limits). Every constraint is necessary, so `LB` bounds every stage-1 cost;
CP-SAT proves `LB = 204 100` in milliseconds. For rooms: at most 60 seats per room → at least
⌈10 000 / 60⌉ = **167 rooms**; 176 rooms = 105 % of that bound (and the cost-optimal set). The
remaining slack comes from cost (cheaper 40/48-seat rooms beat dearer 60-seat ones) and from
R2 (`maxPerScope = 2`, so a 60-seat room needs ≥ 30 groups).

### With alternatives (after)

| Size | `--alternatives` | Status | Stage 1 ms | Stage 2 ms | Alternatives ms | Allocations | Budget (2 × 60 s) |
|---|---|---|---|---|---|---|---|
| Medium | 3 | Optimal | 208 | 3 103 | 38 433 | 4 (best + 3) | respected |
| Large | 3 | Optimal | 1 823 | 2 505 | 58 599 | 1 (none found) | respected |

On large the stage-1 alternative still uses the single §4 model (≈ 204 000 variables plus the
diversity terms) and finds no solution within its 60 s; the loop then stops with
`AlternativesLimited(TimeBudgetExhausted)`. See "Open" below.

### Integrator re-measurement after Phase 3 integration (quiet machine, 2 cores, `--repeat 3`)

Integrated `phase-3` (H1–H4 + change requests), no other load. Total / stage 1 / stage 2 in ms:

| Size | Run 1 | Run 2 | Run 3 | Status, rooms, cost | §12 |
|---|---|---|---|---|---|
| Small | 3 846 / 209 / 656 (cold start, precheck 2.9 s) | 502 / 11 / 483 | 626 / 9 / 606 | Optimal ×3, 8 rooms, 8 200, valid | met |
| Medium | 4 288 / 331 / 3 583 | 2 443 / 80 / 2 326 | 2 176 / 54 / 2 059 | Optimal ×3, 36 rooms, 41 100, valid | met |
| Large | 3 843 / 1 269 / 2 206 | 2 363 / 539 / 1 743 | 2 139 / 540 / 1 547 | Optimal ×3, 176 rooms, 204 100, valid | met |

Large is within all §12 targets (stage 1 < 60 s, stage 2 < 30 s, full < 120 s); its cost equals the
room-selection lower bound (proven optimal), and 176 rooms are 105 % of the seat bound
⌈10 000 / 60⌉ = 167.

## What was tried

| # | Attempt | Result |
|---|---|---|
| 1 | (Phase 2) 3/4/8 workers, load-ordering symmetry breaking, `linearization_level:2`, `interleave_search`, y-linked R2/α bounds | no gain on medium, large unchanged — reverted (review R-5) |
| 2 | (Phase 2) complete greedy hint on all 400 rooms (counts too) | inconsistent with the symmetry order; medium ~25 s — reverted |
| 3 | (Phase 2) covering knapsacks + greedy `y` hint | medium stage 1 12.5–15 s → 5.2–7.2 s; large unchanged — kept |
| 4 | **Step S**: room selection on room types (integer `o_τ`, ~15 types on large) | proves `LB` in < 0.1 s on all sizes — kept |
| 5 | Step S + **step A**: §4 model restricted to the selected rooms (`y = 0` elsewhere), `Cost ≥ LB`, complete greedy hint | medium stage 1 0.6–1.0 s, but large: CP-SAT presolve of the restricted model (~90 000 vars) alone took 19 s; one run Optimal after 20 s, one run `UNKNOWN` after its 30 s share under load (fell back to the full model → NoSolutionInTime) — kept as step A, not sufficient alone |
| 6 | **Step E**: evaluate the greedy assignment with all `n_{k,r}` fixed (trivial presolve); accept if penalty 0 and `Cost = LB` | large stage 1 0.7–1.5 s, Optimal; medium 0.04–0.2 s — kept |

Steps C (global soft check) and D (fallback to the full model) only run when step E/A do not
prove optimality; they are covered by tests (`TwoStepStage1Tests`) but not exercised by the
synthetic datasets.

## Reproduce / confirm on 8 cores

```bash
export PATH=$HOME/.dotnet:$PATH DOTNET_ROOT=$HOME/.dotnet   # if dotnet is installed locally
dotnet build -c Release api/tools/Bench
for size in small medium large; do
  dotnet run -c Release --no-build --project api/tools/Bench -- --size $size --repeat 5
done
dotnet run -c Release --no-build --project api/tools/Bench -- --size medium --repeat 1 --alternatives 3
dotnet run -c Release --no-build --project api/tools/Bench -- --size large --repeat 1 --alternatives 3
```

Bench exits non-zero when a run is not solved, invalid, or over 1.5 × the full-run target.
`EXAM_SOLVER_LOG=1` prints CP-SAT search logs.

## Open

- Alternatives on large: apply the two-step idea to the alternatives mode (diversity + cost slack
  over a selected room set) — today the stage-1 alternative does not find a solution in 60 s.