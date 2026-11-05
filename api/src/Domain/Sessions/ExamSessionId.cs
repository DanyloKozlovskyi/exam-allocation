namespace Domain.Sessions;

public record ExamSessionId(Guid Value)
{
    public static ExamSessionId Empty() => new(Guid.Empty);

    public static ExamSessionId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}