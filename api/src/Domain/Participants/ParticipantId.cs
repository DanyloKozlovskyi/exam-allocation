namespace Domain.Participants;

public record ParticipantId(Guid Value)
{
    public static ParticipantId Empty() => new(Guid.Empty);

    public static ParticipantId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}