using Domain.Allocations;
using Domain.Pins;
using Domain.Rooms;

namespace Application.Allocations.Models;

public enum ExportFormat
{
    Xlsx = 1,
    Xml = 2,
    Csv = 3,
    Pdf = 4,
}

public sealed record SeatMove(Guid ParticipantId, Guid RoomId, string SeatKey);

public sealed record MoveViolation(ViolationKind Kind, bool IsHard, Guid? RuleId, Guid? ParticipantId, Guid? RoomId, string? SeatKey);

public sealed record MoveValidationReport(bool IsValid, IReadOnlyList<MoveViolation> Violations, long SoftPenalty);

public sealed record AllocationExportModel(
    string SessionName,
    DateOnly? ExamDate,
    int AllocationIndex,
    AllocationObjective Objective,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ExportCenter> Centers);

public sealed record ExportCenter(Guid Id, string Name, string Address, double Latitude, double Longitude, IReadOnlyList<ExportRoom> Rooms);

public sealed record ExportRoom(Guid Id, string Name, int Capacity, RoomLayout Layout, IReadOnlyList<ExportSeat> Seats);

public sealed record ExportSeat(string SeatKey, int Row, int Column, int SeatIndex, ExportParticipant? Participant);

public sealed record ExportParticipant(Guid Id, string? ExternalId, string LastName, string FirstName, string? MiddleName, string? Group, string? School);

public sealed record ExportFile(string FileName, string ContentType, byte[] Content);

public sealed record PinnedSeat(Pin Pin, string SeatKey);