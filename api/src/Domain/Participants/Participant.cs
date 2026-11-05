using Domain.Common;
using Domain.Sessions;
using Domain.ValueObjects;

namespace Domain.Participants;

public class Participant : IEntity<ParticipantId>, IVersioned
{
    private Participant(
        ParticipantId id,
        ExamSessionId sessionId,
        string? externalId,
        PersonName name,
        string? group,
        string? school,
        bool needsAccessibleSeat,
        Dictionary<string, string> attributes,
        string? notes)
    {
        Id = id;
        SessionId = sessionId;
        ExternalId = externalId;
        Name = name;
        Group = group;
        School = school;
        NeedsAccessibleSeat = needsAccessibleSeat;
        Attributes = attributes;
        Notes = notes;
    }

    private Participant()
    {
        Id = ParticipantId.Empty();
        SessionId = ExamSessionId.Empty();
        Name = PersonName.From("-", "-", null);
    }

    public ParticipantId Id { get; private set; }

    public ExamSessionId SessionId { get; private set; }

    public string? ExternalId { get; private set; }

    public PersonName Name { get; private set; }

    public string? Group { get; private set; }

    public string? School { get; private set; }

    public bool NeedsAccessibleSeat { get; private set; }

    /// <summary>Custom attributes keyed by normalized <see cref="AttributeKey"/> value.</summary>
    public Dictionary<string, string> Attributes { get; private set; } = [];

    public string? Notes { get; private set; }

    public uint Version { get; private set; }

    public static Participant New(
        ParticipantId id,
        ExamSessionId sessionId,
        string? externalId,
        PersonName name,
        string? group,
        string? school,
        bool needsAccessibleSeat,
        IReadOnlyDictionary<AttributeKey, string> attributes,
        string? notes)
    {
        Validate(externalId, group, school, attributes, notes);
        return new Participant(id, sessionId, Clean(externalId), name, Clean(group), Clean(school), needsAccessibleSeat, ToStorage(attributes), Clean(notes));
    }

    public void UpdateDetails(
        string? externalId,
        PersonName name,
        string? group,
        string? school,
        bool needsAccessibleSeat,
        IReadOnlyDictionary<AttributeKey, string> attributes,
        string? notes)
    {
        Validate(externalId, group, school, attributes, notes);
        ExternalId = Clean(externalId);
        Name = name;
        Group = Clean(group);
        School = Clean(school);
        NeedsAccessibleSeat = needsAccessibleSeat;
        Attributes = ToStorage(attributes);
        Notes = Clean(notes);
    }

    /// <summary>Resolves an attribute value: built-in keys map to promoted columns.</summary>
    public string? GetAttribute(AttributeKey key)
    {
        return key.Value switch
        {
            AttributeKeys.Group => Group,
            AttributeKeys.School => School,
            _ => Attributes.GetValueOrDefault(key.Value),
        };
    }

    private static void Validate(
        string? externalId,
        string? group,
        string? school,
        IReadOnlyDictionary<AttributeKey, string> attributes,
        string? notes)
    {
        Guard.MaxLength(externalId, ParticipantConstraints.ExternalIdMaxLength, nameof(externalId));
        Guard.MaxLength(group, ParticipantConstraints.GroupMaxLength, nameof(group));
        Guard.MaxLength(school, ParticipantConstraints.SchoolMaxLength, nameof(school));
        Guard.MaxLength(notes, ParticipantConstraints.NotesMaxLength, nameof(notes));
        Guard.IsTrue(attributes.Count <= ParticipantConstraints.MaxAttributes, nameof(attributes));
        foreach (var value in attributes.Values)
        {
            Guard.MaxLength(value, ParticipantConstraints.AttributeValueMaxLength, nameof(attributes));
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Dictionary<string, string> ToStorage(IReadOnlyDictionary<AttributeKey, string> attributes)
        => attributes.ToDictionary(x => x.Key.Value, x => x.Value.Trim());
}