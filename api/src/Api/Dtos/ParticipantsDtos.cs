using Application.Participants.Commands;
using Application.Participants.Models;
using Domain.Participants;

namespace Api.Dtos;

public record ParticipantDto(
    Guid? Id,
    Guid? SessionId,
    string? ExternalId,
    string LastName,
    string FirstName,
    string? MiddleName,
    string? Group,
    string? School,
    bool NeedsAccessibleSeat,
    IReadOnlyDictionary<string, string> Attributes,
    string? Notes,
    uint? Version = null)
{
    public static ParticipantDto FromDomain(Participant participant) => new(
        participant.Id.Value,
        participant.SessionId.Value,
        participant.ExternalId,
        participant.Name.LastName,
        participant.Name.FirstName,
        participant.Name.MiddleName,
        participant.Group,
        participant.School,
        participant.NeedsAccessibleSeat,
        participant.Attributes,
        participant.Notes,
        participant.Version);

    public CreateParticipantCommand ToCreateCommand(Guid sessionId) => new()
    {
        SessionId = sessionId,
        ExternalId = ExternalId,
        LastName = LastName,
        FirstName = FirstName,
        MiddleName = MiddleName,
        Group = Group,
        School = School,
        NeedsAccessibleSeat = NeedsAccessibleSeat,
        Attributes = Attributes,
        Notes = Notes,
    };

    public UpdateParticipantCommand ToUpdateCommand(Guid sessionId) => new()
    {
        SessionId = sessionId,
        Id = Id ?? Guid.Empty,
        ExternalId = ExternalId,
        LastName = LastName,
        FirstName = FirstName,
        MiddleName = MiddleName,
        Group = Group,
        School = School,
        NeedsAccessibleSeat = NeedsAccessibleSeat,
        Attributes = Attributes,
        Notes = Notes,
        Version = Version ?? default,
    };
}

public record AttributeInfoDto(string Key, int DistinctValues, int MissingCount, IReadOnlyList<string> SampleValues)
{
    public static AttributeInfoDto FromModel(AttributeSummary summary)
        => new(summary.Key, summary.DistinctValues, summary.MissingCount, summary.SampleValues);
}