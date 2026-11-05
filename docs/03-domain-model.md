# 03 — Domain & Data Model

Entity style, ids and persistence conventions follow [10 §2 and §4](10-conventions.md) (pal-suite):
every entity has a **strongly-typed id** record (`ExamSessionId(Guid Value)`, `TestCenterId`,
`RoomId`, `SeatId`, `ParticipantId`, `RuleId`, `PinId`, `SolveRunId`, `AllocationId`) created
with `New()` (`Guid.CreateVersion7()`), private setters, private constructor + `static New(...)`
and behaviour methods; length limits in `<Entity>Constraints`. Tables and columns are
snake_case (EFCore.NamingConventions); **enums are stored as strings**; timestamps are
`DateTimeOffset` UTC from `IDateTimeProvider`. In the tables below, `Guid` means the
entity's typed id.
Every entity except `ExamSession` belongs to exactly one session (`SessionId`) — deleting a
session cascades.

## Entity diagram

```
ExamSession 1─* TestCenter 1─* Room 1─* Seat
     │
     ├─1─* Participant ─*─ ParticipantAttribute (jsonb on Participant)
     ├─1─* Rule
     ├─1─* Pin            (Participant → Room | Seat)
     └─1─* SolveRun 1─* Allocation 1─* AllocationItem (Participant → Room → Seat)
```

## Entities

### ExamSession
| Field | Type | Notes |
|---|---|---|
| Id | Guid | |
| Name | string(200) | required |
| ExamDate | DateOnly? | informational |
| Description | string(2000)? | |
| CreatedAt / UpdatedAt | DateTimeOffset | |

### TestCenter
| Field | Type | Notes |
|---|---|---|
| Id, SessionId | Guid | |
| Name | string(200) | |
| Address | string(500) | |
| Latitude, Longitude | double | WGS84 |
| GooglePlaceId | string(300)? | from Places Autocomplete |
| CostPerRoom | decimal | default 1.0; used by economic objective (open-room cost) |
| IsActive | bool | inactive centers are excluded from solving |

### Room
| Field | Type | Notes |
|---|---|---|
| Id, CenterId | Guid | |
| Name | string(100) | e.g. “Ауд. 214” |
| Layout | `RoomLayout` (jsonb) | see below |
| Capacity | int | **derived** = count of enabled seats; recomputed on layout save |
| AccessibleCapacity | int | derived |
| CostOverride | decimal? | overrides `TestCenter.CostPerRoom` |
| IsActive | bool | |

### RoomLayout (value object, stored as jsonb)

```jsonc
{
  "rows": 6,                 // desk rows, 0 = front (closest to proctor)
  "columns": 5,              // desks per row
  "seatsPerDesk": 2,         // 1..3
  "aislesAfterColumns": [1, 3],   // aisle between column 1|2 and 3|4 (0-based)
  "disabledSeats": ["r0c4s1"],    // seat keys that don't exist / unusable
  "accessibleSeats": ["r0c0s0", "r0c0s1"]
}
```

Seat key format: `r{row}c{column}s{seatIndex}`, all 0-based. Seat label for humans:
`Ряд {row+1}, Парта {column+1}, Місце {seat+1}`.

### Seat (materialized from layout)
| Field | Type | Notes |
|---|---|---|
| Id, RoomId | Guid | |
| Key | string(20) | unique per room |
| Row, Column, SeatIndex | int | |
| IsAccessible | bool | |

Seats are regenerated when the layout changes. If a layout change would remove a seat that
is referenced by a pin, the API returns `409` with the list of affected pins.

### Participant
| Field | Type | Notes |
|---|---|---|
| Id, SessionId | Guid | |
| ExternalId | string(100)? | id from source file; unique per session when present |
| LastName, FirstName | string(100) | required |
| MiddleName | string(100)? | |
| Group | string(100)? | promoted attribute (indexed) |
| School | string(200)? | promoted attribute (indexed) |
| NeedsAccessibleSeat | bool | |
| Attributes | `Dictionary<string,string>` (jsonb) | custom columns from import; keys are normalized `AttributeKey.Value`s (the entity API takes `IReadOnlyDictionary<AttributeKey,string>`); `Participant.GetAttribute(AttributeKey)` resolves built-ins to the promoted columns |
| Notes | string(1000)? | |

Rules reference attributes by **attribute key**: `group`, `school`, or any custom key
(`attributes.<key>`). The resolver lives in Domain: `ParticipantAttributeResolver.Get(p, key)`.

### Rule
| Field | Type | Notes |
|---|---|---|
| Id, SessionId | Guid | |
| Type | `RuleType` enum | see catalog in 04 |
| IsHard | bool | soft rules contribute weighted penalties |
| Weight | int | 1..1000, soft only |
| IsEnabled | bool | |
| Parameters | `RuleParameters` (jsonb) | typed record per `RuleType` (polymorphic JSON with `type` discriminator); validated by the command validator for that type |
| DisplayName | string(200)? | user label |

### Pin
| Field | Type | Notes |
|---|---|---|
| Id, SessionId, ParticipantId | Guid | unique (SessionId, ParticipantId) |
| RoomId | Guid | required |
| SeatId | Guid? | if null, only the room is fixed |

### SolveRun
| Field | Type | Notes |
|---|---|---|
| Id, SessionId | Guid | |
| Status | `Queued, Running, Succeeded, Infeasible, Failed, Cancelled` | |
| Options | jsonb | `SolveOptions` snapshot (alternatives count, time limits, seed) |
| InputHash | string(64) | SHA-256 of normalized SolverInput, for “stale result” detection |
| Diagnostics | jsonb? | conflict report when Infeasible |
| Stats | jsonb? | timings, objective values, variable counts |
| Error | string? | |
| CreatedAt, StartedAt, FinishedAt | DateTimeOffset? | |

### Allocation / AllocationItem
| Allocation | Type | Notes |
|---|---|---|
| Id, RunId | Guid | |
| Index | int | 0 = best, 1..N alternatives |
| Objective | jsonb | `{ roomsUsed, cost, softPenalty, perRule: {ruleId: penalty} }` |

| AllocationItem | Type | Notes |
|---|---|---|
| AllocationId, ParticipantId | Guid | PK (AllocationId, ParticipantId) |
| RoomId, SeatId | Guid | |

A session is **stale** relative to a run when the current `InputHash` differs from
`SolveRun.InputHash`; UI shows a banner “дані змінилися після розрахунку”.

## Seat adjacency

Computed in `Solver/Layout/AdjacencyBuilder` from the layout plus `AdjacencyKind` flags:

| Kind | Definition (seat A at (r,c,s), seat B at (r',c',s')) |
|---|---|
| `SameDesk` | r = r', c = c', s ≠ s' — **always on** |
| `Side` | r = r', \|c − c'\| = 1, **no aisle** between c and c' (seats at neighbouring desks in a row) |
| `SideAcrossAisle` | r = r', \|c − c'\| = 1, aisle between them (off by default) |
| `FrontBack` | c = c', \|r − r'\| = 1 (any seat indices) |
| `Diagonal` | \|r − r'\| = 1, \|c − c'\| = 1, no aisle between columns |

Optional refinement (flag `SeatLevelSide`, default off): for `Side`, only the facing seats
count (the right-most seat of the left desk and the left-most seat of the right desk).

Edges are undirected, stored as `(minSeatIdx, maxSeatIdx)` pairs; disabled seats are
removed before computing edges.

## Indexes & constraints (EF configuration)

- `Participant (SessionId, ExternalId)` unique filtered `WHERE ExternalId IS NOT NULL`
- `Participant (SessionId, Group)`, `(SessionId, School)`
- `Seat (RoomId, Key)` unique
- `Pin (SessionId, ParticipantId)` unique
- `AllocationItem (AllocationId, RoomId)`
- Cascade delete from `ExamSession` to everything; `Room → Seat` cascade; `Seat` delete
  restricted if referenced by `Pin` (handled in API with 409).
