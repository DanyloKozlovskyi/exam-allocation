using Application.Allocations.Models;
using Domain.Allocations;

namespace Api.Dtos;

public record ObjectiveDto(int RoomsUsed, decimal Cost, long SoftPenalty, IReadOnlyDictionary<Guid, long> PenaltyByRule)
{
    public static ObjectiveDto FromDomain(AllocationObjective objective)
        => new(objective.RoomsUsed, objective.Cost, objective.SoftPenalty, objective.PenaltyByRule);
}

public record AllocationSummaryDto(Guid Id, int Index, ObjectiveDto Objective)
{
    public static AllocationSummaryDto FromDomain(Allocation allocation)
        => new(allocation.Id.Value, allocation.Index, ObjectiveDto.FromDomain(allocation.Objective));
}

public record AllocationRoomDto(Guid RoomId, string Name, bool Used, int Filled, int Capacity);

public record AllocationCenterDto(Guid CenterId, string Name, double Latitude, double Longitude, IReadOnlyList<AllocationRoomDto> Rooms);

public record AllocationDto(Guid Id, int Index, ObjectiveDto Objective, IReadOnlyList<AllocationCenterDto> Centers);

public record ParticipantBriefDto(Guid Id, string LastName, string FirstName, string? MiddleName, string? Group, string? School);

public record ViolationDto(ViolationKind Kind, bool IsHard, Guid? RuleId, Guid? ParticipantId, Guid? RoomId, string? SeatKey)
{
    public static ViolationDto FromModel(MoveViolation violation)
        => new(violation.Kind, violation.IsHard, violation.RuleId, violation.ParticipantId, violation.RoomId, violation.SeatKey);
}

public record SeatOccupancyDto(string SeatKey, ParticipantBriefDto? Participant, IReadOnlyList<ViolationDto> Violations);

public record RoomSeatingDto(RoomDto Room, IReadOnlyList<SeatOccupancyDto> Seats);

public record AllocationRowDto(ParticipantBriefDto Participant, Guid CenterId, string CenterName, Guid RoomId, string RoomName, string SeatKey);

public record MoveDto(Guid ParticipantId, Guid RoomId, string SeatKey)
{
    public SeatMove ToModel() => new(ParticipantId, RoomId, SeatKey);
}

public record MovesDto(IReadOnlyList<MoveDto> Moves, bool Force = false);

public record ValidationReportDto(bool IsValid, IReadOnlyList<ViolationDto> Violations, long SoftPenalty)
{
    public static ValidationReportDto FromModel(MoveValidationReport report)
        => new(report.IsValid, report.Violations.Select(ViolationDto.FromModel).ToList(), report.SoftPenalty);
}