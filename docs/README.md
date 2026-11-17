# Exam Allocation — Documentation

System for allocating exam participants to test centers, rooms and seats under
placement constraints (e.g. groupmates may not share a room or sit at adjacent desks).

| # | Document | What it answers |
|---|----------|-----------------|
| 01 | [Overview & scope](01-overview.md) | What we build, MVP boundaries, glossary |
| 02 | [Architecture & tech stack](02-architecture.md) | Stack, repo layout, project boundaries, key decisions |
| 03 | [Domain & data model](03-domain-model.md) | Entities, DB schema, room layout and seat adjacency |
| 04 | [Solver design](04-solver.md) | Mathematical model, decomposition, rules, infeasibility, alternatives |
| 05 | [API contract](05-api.md) | REST endpoints, DTOs, async solve jobs, errors |
| 06 | [Client](06-frontend.md) | Views, feature components, routes, map, SignalR progress |
| 07 | [Import & export](07-import-export.md) | Excel template, column mapping, validation, XLSX/XML/CSV/PDF export |
| 08 | [Testing & benchmarks](08-testing.md) | Test strategy, synthetic data, performance targets |
| 09 | [Implementation plan](09-implementation-plan.md) | Phases, parallel workstreams, file ownership, acceptance checks |
| 10 | [Coding conventions](10-conventions.md) | pal-suite patterns (MediatR CQRS, FluentValidation, LanguageExt, repositories), no magic strings, client enums |

| Document | What it answers |
|----------|-----------------|
| [User guide (Ukrainian)](user-guide.md) | End-to-end workflow for an exam administrator, UI labels, troubleshooting |
| [Benchmarks](benchmarks.md) | Measured solver timings per dataset size vs the 04 §12 targets (Phase 3, H1) |
| [Phase 2 smoke test](smoke-test-phase2.md) | Real-API happy-path walk through the UI, with results |
| [Phase 2 review](review-phase2.md) | Independent review findings (R-1…R-25) and their resolution |
| [Change requests](change-requests.md) | Cross-workstream change requests and their status |

Quick start (prerequisites, run, tests, benchmarks): [`/README.md`](../README.md#quick-start).
Agent working rules live in [`/CLAUDE.md`](../CLAUDE.md).

**Source-of-truth order** when documents disagree: `10` (how code is written) → `09` (who
does what) → `04` (solver semantics) → `03` (data) → `05` (API) → everything else.
Architecture reference project: `../pal-suite`. Fix the lower-priority doc in the
same change.
