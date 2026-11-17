# 05 — API Contract

Controllers (`[ApiController]`), routes in `Api/Constants/ApiRoutes.cs`, pal-suite route style:
collection `GET`, `POST …/create`, `PUT …/edit` (id in body), `DELETE …/{id}`.
JSON camelCase, **enums as strings**. DTOs are `record`s in `Api/Dtos/<Feature>Dtos.cs`.
Swagger document exported to `contracts/openapi.json` (committed).

## Conventions

- **Write endpoints** build a command from the DTO and `sender.Send(...)`; the
  `Either<FeatureException, Entity>` is matched into `Dto` or `e.ToObjectResult()`.
- **Read endpoints** call `I<Feature>ControllerService` (no MediatR) returning
  `IReadOnlyList<Dto>` / `Option<Dto>` → `NotFound()` when `None`.
- **Errors** (`<Feature>ErrorFactory`): `ProblemDetails { title, status, code }` where
  `code` is `ErrorCode` (string enum). Status mapping per exception type:
  `*NotFoundException` → 404, `*AlreadyExistsException`/`*ConflictException` → 409,
  `*ArgumentException` → 400, `*UnknownException` → 500.
- **Validation** failures (FluentValidation via `ValidationBehaviour`) → 400
  `ValidationProblemDetails { errors: { field: [msg] }, code: "ValidationFailed" }`.
- **Declared responses**: every action lists its success status (200 with the DTO, 202, 204).
  Write actions (commands) also list every `ProblemDetails` status their feature's error factory can
  return (400/404/409/413/422/500); read actions list 400 for invalid query parameters and 404 where
  they return `NotFound()` — an unexpected server error (500) is not declared on reads.
- **Paging** (participants, allocation rows): query `page`, `pageSize`, `search`, `sortDirection`
  (`SortDirection` enum) → `PagedDto<T> { items, page, pageSize, total }`. Participants also take
  `sortBy` (`ParticipantSortField` enum); allocation rows are always sorted by name.
- **Concurrency**: DTOs carry `version` (Postgres `xmin`); `edit` with a stale version →
  409 `ErrorCode.ConcurrencyConflict`.

## Endpoints

### Sessions — `SessionsController`
| Method | Route | Body | Response |
|---|---|---|---|
| GET | `sessions` | | `SessionSummaryDto[]` |
| GET | `sessions/{sessionId}` | | `SessionDto` |
| GET | `sessions/{sessionId}/totals` | | `SessionTotalsDto { totalCapacity, accessibleCapacity, participants, needsAccessible, groups, isStale }` |
| POST | `sessions/create` | `SessionDto` | `SessionDto` |
| PUT | `sessions/edit` | `SessionDto` | `SessionDto` |
| DELETE | `sessions/{sessionId}` | | `204` |

### Test centers — `TestCentersController`
| GET | `sessions/{sessionId}/centers` | | `TestCenterDto[]` (with roomCount, capacity) |
| GET | `sessions/{sessionId}/centers/{centerId}` | | `TestCenterDto` |
| POST | `sessions/{sessionId}/centers/create` | `TestCenterDto` | `TestCenterDto` |
| PUT | `sessions/{sessionId}/centers/edit` | `TestCenterDto` | `TestCenterDto` |
| DELETE | `sessions/{sessionId}/centers/{centerId}` | | `204` |

### Rooms — `RoomsController`
| GET | `centers/{centerId}/rooms` | | `RoomDto[]` |
| GET | `centers/{centerId}/rooms/{roomId}` | | `RoomDto` (incl. `seats: SeatDto[]`, `capacity`, `accessibleCapacity`) |
| POST | `centers/{centerId}/rooms/create` | `RoomDto` | `RoomDto` |
| POST | `centers/{centerId}/rooms/create-bulk` | `CreateRoomsBulkDto { template: RoomDto, names: string[] }` | `RoomDto[]` |
| PUT | `centers/{centerId}/rooms/edit` | `RoomDto` | `RoomDto`; removing a pinned seat → 409 `ErrorCode.SeatReferencedByPin`, affected pins in the `data` extension |
| DELETE | `centers/{centerId}/rooms/{roomId}` | | `204` |
| POST | `rooms/preview-layout` | `RoomLayoutDto` | `RoomLayoutPreviewDto { seats, capacity, accessibleCapacity, adjacency: AdjacencyEdgesDto[] { kind: AdjacencyKind, edges: [ [a,b] ] } }` (no persistence) |

### Participants — `ParticipantsController`
| GET | `sessions/{sessionId}/participants` | paging + `group`, `school`, `needsAccessibleSeat` | `PagedDto<ParticipantDto>` |
| GET | `sessions/{sessionId}/participants/{participantId}` | | `ParticipantDto` |
| POST | `sessions/{sessionId}/participants/create` | `ParticipantDto` | `ParticipantDto` |
| PUT | `sessions/{sessionId}/participants/edit` | `ParticipantDto` | `ParticipantDto` |
| DELETE | `sessions/{sessionId}/participants/{participantId}` | | `204` |
| POST | `sessions/{sessionId}/participants/delete-all` | | `204` |
| GET | `sessions/{sessionId}/attributes` | | `AttributeInfoDto[] { key, distinctValues, missingCount, sampleValues[] }` |

### Rules — `RulesController`
| GET | `sessions/{sessionId}/rules` | | `RuleDto[]` |
| GET | `rule-types` | | `RuleTypeInfoDto[] { type: RuleType, allowsHard, allowsSoft, defaultParameters }` |
| POST | `sessions/{sessionId}/rules/create` | `RuleDto { type, isHard, weight, isEnabled, displayName?, parameters }` | `RuleDto` |
| PUT | `sessions/{sessionId}/rules/edit` | `RuleDto` | `RuleDto` |
| DELETE | `sessions/{sessionId}/rules/{ruleId}` | | `204` |

`RuleDto.parameters` is a **discriminated set of typed DTOs**, one per `RuleType`
(`SeparateByAttributeParametersDto`, `NoAdjacentByAttributeParametersDto`, …) exposed as a
`oneOf` with discriminator `type` — no free-form JSON on the wire.

### Pins — `PinsController`
| GET | `sessions/{sessionId}/pins` | | `PinDto[]` with read-only `participantName`, `roomName`, `centerId` |
| POST | `sessions/{sessionId}/pins/create` | `PinDto { participantId, roomId, seatKey? }` | `PinDto` (upsert) |
| DELETE | `sessions/{sessionId}/pins/{participantId}` | | `204` |
| POST | `sessions/{sessionId}/pins/delete-all` | | `204` (removes every pin of the session) |
| POST | `allocations/{allocationId}/pin-all` | `PinAllDto { roomIds?: Guid[] }` | `PinDto[]` |

### Imports — `ImportsController` (details in 07)
| POST | `sessions/{sessionId}/imports/upload` | multipart `file` | `ImportPreviewDto { importId, sheets[], headers[], sampleRows[][], suggestedMapping: ColumnMappingDto }` |
| POST | `imports/{importId}/validate` | `ImportMappingDto { sheet, headerRow, mapping: ColumnMappingDto, mode: ImportMode, dedupeBy: DedupeStrategy }` | `ImportValidationDto { validRows, errorRows, errors: ImportErrorDto[] { row, column, code: ImportErrorCode }, duplicates[] }` |
| POST | `imports/{importId}/commit` | same as validate + `skipInvalid` | `ImportResultDto { inserted, updated, skipped }` |
| GET | `imports/template` | | XLSX file |

`ColumnMappingDto` maps **`ParticipantField` enum → source header** plus `attributes: { key → header }`.

### Solve runs — `SolveRunsController`
| POST | `sessions/{sessionId}/solve-runs/create` | `SolveOptionsDto { alternatives?, diversityRatio?, costSlack?, timeLimitSeconds?, objectiveMode?: ObjectiveMode, seed? }` | `SolveRunDto` (status `Queued`) |
| GET | `sessions/{sessionId}/solve-runs` | | `SolveRunDto[]` newest first |
| GET | `solve-runs/{runId}` | | `SolveRunDto { id, status: SolveRunStatus, createdAt, startedAt, finishedAt, stats, diagnostics: DiagnosticDto[], allocationIds[], isStale, errorMessage?, failureReason?: SolveRunFailureReason }`; `allocationIds`: solver allocations by index (best first), then manual versions; `isStale` is computed for finished runs only (active runs report `false`); `failureReason` (`Interrupted`, `NoSolutionInTime`, `SolverError`) is set only for `Failed` runs, `errorMessage` is free-text detail |
| POST | `solve-runs/{runId}/cancel` | | `202` |
| POST | `sessions/{sessionId}/precheck` | | `DiagnosticDto[] { code: DiagnosticCode, severity: DiagnosticSeverity, message, ruleId?, arguments }` |

Second run while one is `Queued`/`Running` → 409 `ErrorCode.SolveRunAlreadyActive` (also under
concurrent creates: a partial unique index allows one active run per session).

### Allocations — `AllocationsController`
| GET | `solve-runs/{runId}/allocations` | | `AllocationSummaryDto[] { id, index, objective }`, same order as `allocationIds` |
| GET | `allocations/{allocationId}` | | `AllocationDto { id, index, objective, centers: [{ centerId, rooms: [{ roomId, used, filled, capacity }] }] }` |
| GET | `allocations/{allocationId}/rooms/{roomId}` | | `RoomSeatingDto { room, seats: [{ seatKey, participant?, violations: ViolationDto[] }] }` |
| GET | `allocations/{allocationId}/participants` | paging + search | `PagedDto<AllocationRowDto>` |
| POST | `allocations/{allocationId}/validate-moves` | `MovesDto { moves: [{ participantId, roomId, seatKey }] }` | `ValidationReportDto` |
| POST | `allocations/{allocationId}/apply-moves` | `MovesDto` + `force` | `AllocationDto` (new manual allocation, `index = -1`) |

### Exports — `ExportsController` (details in 07)
| GET | `allocations/{allocationId}/export` | `?format=` `ExportFormat` (`Xlsx`, `Xml`, `Csv`, `Pdf`), `?roomId=` (Pdf only) | file |

### Meta — `ApiEndpointsModule`
`GET /health` → `"healthy"` (pal-suite). Swagger UI at `/swagger` (Development).

## SignalR — `SolveProgressHub` at `HubRoutes.SolveProgress` (`/hubs/solve-progress`)

| Direction | Method (constants in `SolveProgressHubMethods`) | Payload |
|---|---|---|
| client → server | `SubscribeToRun` | `runId` (adds connection to group `HubGroups.Run(runId)`) |
| client → server | `UnsubscribeFromRun` | `runId` |
| server → client | `ProgressUpdated` | `SolveProgressDto { runId, stage: SolveStage, percent?, objective?, bestBound?, roomsDone?, roomsTotal?, alternativesDone? }` |
| server → client | `RunCompleted` | `SolveRunDto` |

On subscribe, the hub immediately sends the last known progress (kept in memory per run),
or `RunCompleted` if the run already finished.

## Enums exposed in the contract (string-serialized, generated as TS enums)

`ErrorCode`, `RuleType`, `AdjacencyKind`, `RuleScope`, `ObjectiveMode`, `SolveRunStatus`,
`SolveStage`, `DiagnosticCode`, `DiagnosticSeverity`, `ImportMode`, `DedupeStrategy`,
`ParticipantField`, `ImportErrorCode`, `ExportFormat`, `ParticipantSortField`, `SortDirection`,
`ViolationKind`.

`AdjacencyKind` is a list on the wire (`AdjacencyKind[]`), not a bit flag, so it stays a
plain string enum for the client.
