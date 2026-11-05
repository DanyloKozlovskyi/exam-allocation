# 01 — Overview & Scope

## Problem

An organizer runs a mass test (school/university exam, external testing). They have:

- a set of **test centers** (buildings) picked on a map, each with **rooms**;
- each room has a **layout** of desks and seats;
- a list of **participants** (students) with attributes: group, school, special needs…;
- **rules**: e.g. “no two participants from the same group in one room”, “groupmates must
  not sit at adjacent desks”, “participants needing accessibility go to accessible seats”.

They need a seating plan that satisfies all hard rules, uses resources economically,
can be inspected visually, adjusted by hand, and exported.

## What the system does

1. **Session setup** — create an *exam session* (one exam event).
2. **Centers** — search and pick centers on Google Maps; set name, address, coordinates.
3. **Rooms** — per center, define rooms with a grid layout (rows × desks × seats per desk,
   aisles, disabled/accessible seats). Room capacity is derived from the layout.
4. **Participants** — import from XLSX/CSV with column mapping and validation; edit in a table.
5. **Rules** — configure from a catalog of rule templates (hard or soft, with parameters).
6. **Solve** — run the optimizer asynchronously; get one optimal allocation plus N
   diverse alternatives, or an explanation of which rules conflict.
7. **Review** — see allocation on the map (per-center load) and on room seating charts;
   pin participants manually and re-solve the rest.
8. **Export** — XLSX (per center / per room sheets), XML (documented schema), CSV,
   PDF seating charts and room lists.

## Important non-goal: “all possible arrangements”

Enumerating all valid arrangements is impossible (30 people in 30 seats = 30! ≈ 2.6·10³²).
The system returns **one optimal** allocation and **N diverse alternatives** (default 5,
configurable up to 20), each scored against the objective.

## MVP scope

| In MVP | Later |
|--------|-------|
| Single organizer, no auth (local/dev deployment) | Users, roles, audit log |
| Sessions, centers, rooms, participants, rules CRUD | Versioning / history of sessions |
| Rule catalog R1–R7 (see [04](04-solver.md#rule-catalog)) | Distance-to-home rule (needs geocoding / Distance Matrix) |
| Async solve with progress, infeasibility explanation | Multi-day / multi-slot exams |
| N alternatives, manual pins + re-solve | Drag-and-drop seat swapping with live validation |
| XLSX / XML / CSV export, PDF room charts | Email/notifications to participants |
| Ukrainian UI (i18n-ready) | English UI |

## Scale targets

| Scenario | Participants | Centers | Rooms | Target end-to-end solve |
|----------|--------------|---------|-------|-------------------------|
| Small    | 300          | 2       | 12    | < 5 s |
| Medium   | 2 000        | 10      | 80    | < 30 s |
| Large    | 10 000       | 50      | 400   | < 120 s |

## Glossary

| Term (EN, used in code) | UA | Meaning |
|---|---|---|
| ExamSession | Сесія тестування | One exam event; root aggregate for everything else |
| TestCenter | Центр тестування | Building/location on the map |
| Room | Аудиторія | Room inside a center, has a layout |
| Desk | Парта | Physical desk at (row, column) in a room |
| Seat | Місце | One seat at a desk (a desk has 1–3 seats) |
| Participant | Учасник | Person being seated |
| Attribute | Атрибут | Key/value on a participant (`group`, `school`, custom) |
| Rule | Правило | Constraint from the catalog; hard or soft |
| Pin | Фіксація | Manual fixation of a participant to a room or seat |
| SolveRun | Запуск розв'язувача | One asynchronous optimization job |
| Allocation | Розподіл | One complete assignment (participant → room → seat) |
| Alternative | Альтернатива | Additional allocation from the same run |
| Adjacency | Сусідство | Relation between seats per configured adjacency kinds |
