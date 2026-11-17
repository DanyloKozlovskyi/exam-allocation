// Named re-exports of the generated contract (npm run gen:api). The only file allowed to declare `type` aliases.
import type { components } from './generated/schema';

export {
  AccessibleSeatingParametersDtoType,
  AdjacencyKind,
  DedupeStrategy,
  DiagnosticArgument,
  DiagnosticCode,
  DiagnosticSeverity,
  ErrorCode,
  ExportFormat,
  FillFromFrontParametersDtoType,
  ImportErrorCode,
  ImportMode,
  KeepTogetherInCenterParametersDtoType,
  MinRoomFillParametersDtoType,
  NoAdjacentByAttributeParametersDtoType,
  ObjectiveMode,
  ParticipantField,
  ParticipantSortField,
  RuleScope,
  RuleType,
  SeparateByAttributeParametersDtoType,
  SolveRunFailureReason,
  SolveRunStatus,
  SolveStage,
  SortDirection,
  SpreadInRoomParametersDtoType,
  ViolationKind,
} from './generated/schema';

type Schemas = components['schemas'];

export type AccessibleSeatingParametersDto = Schemas['AccessibleSeatingParametersDto'];
export type AdjacencyEdgesDto = Schemas['AdjacencyEdgesDto'];
export type AllocationCenterDto = Schemas['AllocationCenterDto'];
export type AllocationDto = Schemas['AllocationDto'];
export type AllocationRoomDto = Schemas['AllocationRoomDto'];
export type AllocationRowDto = Schemas['AllocationRowDto'];
export type AllocationRowDtoPagedDto = Schemas['AllocationRowDtoPagedDto'];
export type AllocationSummaryDto = Schemas['AllocationSummaryDto'];
export type AttributeInfoDto = Schemas['AttributeInfoDto'];
export type ColumnMappingDto = Schemas['ColumnMappingDto'];
export type CreateRoomsBulkDto = Schemas['CreateRoomsBulkDto'];
export type DiagnosticDto = Schemas['DiagnosticDto'];
export type FillFromFrontParametersDto = Schemas['FillFromFrontParametersDto'];
export type ImportErrorDto = Schemas['ImportErrorDto'];
export type ImportMappingDto = Schemas['ImportMappingDto'];
export type ImportPreviewDto = Schemas['ImportPreviewDto'];
export type ImportPreviewRequestDto = Schemas['ImportPreviewRequestDto'];
export type ImportResultDto = Schemas['ImportResultDto'];
export type ImportValidationDto = Schemas['ImportValidationDto'];
export type KeepTogetherInCenterParametersDto = Schemas['KeepTogetherInCenterParametersDto'];
export type MinRoomFillParametersDto = Schemas['MinRoomFillParametersDto'];
export type MoveDto = Schemas['MoveDto'];
export type MovesDto = Schemas['MovesDto'];
export type NoAdjacentByAttributeParametersDto = Schemas['NoAdjacentByAttributeParametersDto'];
export type ObjectiveDto = Schemas['ObjectiveDto'];
export type ParticipantBriefDto = Schemas['ParticipantBriefDto'];
export type ParticipantDto = Schemas['ParticipantDto'];
export type ParticipantDtoPagedDto = Schemas['ParticipantDtoPagedDto'];
export type PinAllDto = Schemas['PinAllDto'];
export type PinDto = Schemas['PinDto'];
export type ProblemDetails = Schemas['ProblemDetails'];
export type RoomDto = Schemas['RoomDto'];
export type RoomLayoutDto = Schemas['RoomLayoutDto'];
export type RoomLayoutPreviewDto = Schemas['RoomLayoutPreviewDto'];
export type RoomSeatingDto = Schemas['RoomSeatingDto'];
export type RuleDto = Schemas['RuleDto'];
export type RuleParametersDto = Schemas['RuleParametersDto'];
export type RuleTypeInfoDto = Schemas['RuleTypeInfoDto'];
export type SeatDto = Schemas['SeatDto'];
export type SeatEdgeDto = Schemas['SeatEdgeDto'];
export type SeatOccupancyDto = Schemas['SeatOccupancyDto'];
export type SeparateByAttributeParametersDto = Schemas['SeparateByAttributeParametersDto'];
export type SessionDto = Schemas['SessionDto'];
export type SessionSummaryDto = Schemas['SessionSummaryDto'];
export type SessionTotalsDto = Schemas['SessionTotalsDto'];
export type SolveOptionsDto = Schemas['SolveOptionsDto'];
export type SolveProgressDto = Schemas['SolveProgressDto'];
export type SolveRunDto = Schemas['SolveRunDto'];
export type SolveRunStatsDto = Schemas['SolveRunStatsDto'];
export type SpreadInRoomParametersDto = Schemas['SpreadInRoomParametersDto'];
export type TestCenterDto = Schemas['TestCenterDto'];
export type ValidationProblemDetails = Schemas['ValidationProblemDetails'];
export type ValidationReportDto = Schemas['ValidationReportDto'];
export type ViolationDto = Schemas['ViolationDto'];
