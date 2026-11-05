using Application.Sessions.Commands;
using Application.Sessions.Models;
using Domain.Sessions;

namespace Api.Dtos;

public record SessionDto(
    Guid? Id,
    string Name,
    DateOnly? ExamDate,
    string? Description,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null,
    uint? Version = null)
{
    public static SessionDto FromDomain(ExamSession session)
        => new(session.Id.Value, session.Name, session.ExamDate, session.Description, session.CreatedAt, session.UpdatedAt, session.Version);

    public CreateSessionCommand ToCreateCommand() => new()
    {
        Name = Name,
        ExamDate = ExamDate,
        Description = Description,
    };

    public UpdateSessionCommand ToUpdateCommand() => new()
    {
        Id = Id ?? Guid.Empty,
        Name = Name,
        ExamDate = ExamDate,
        Description = Description,
        Version = Version ?? default,
    };
}

public record SessionSummaryDto(Guid Id, string Name, DateOnly? ExamDate, DateTimeOffset UpdatedAt)
{
    public static SessionSummaryDto FromDomain(ExamSession session)
        => new(session.Id.Value, session.Name, session.ExamDate, session.UpdatedAt);
}

public record SessionTotalsDto(
    int Centers,
    int Rooms,
    int TotalCapacity,
    int AccessibleCapacity,
    int Participants,
    int NeedsAccessibleSeat,
    int Groups,
    int Rules,
    bool IsStale)
{
    public static SessionTotalsDto FromModel(SessionTotals totals, bool isStale)
        => new(totals.Centers, totals.Rooms, totals.TotalCapacity, totals.AccessibleCapacity, totals.Participants, totals.NeedsAccessibleSeat, totals.Groups, totals.Rules, isStale);
}