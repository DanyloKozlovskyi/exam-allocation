using Domain.Common;

namespace Domain.Sessions;

public class ExamSession : IEntity<ExamSessionId>, IVersioned
{
    private ExamSession(ExamSessionId id, string name, DateOnly? examDate, string? description, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        ExamDate = examDate;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public ExamSessionId Id { get; private set; }

    public string Name { get; private set; }

    public DateOnly? ExamDate { get; private set; }

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public uint Version { get; private set; }

    public static ExamSession New(ExamSessionId id, string name, DateOnly? examDate, string? description, DateTimeOffset now)
    {
        Validate(name, description);
        return new ExamSession(id, name.Trim(), examDate, description, now);
    }

    public void UpdateDetails(string name, DateOnly? examDate, string? description, DateTimeOffset now)
    {
        Validate(name, description);
        Name = name.Trim();
        ExamDate = examDate;
        Description = description;
        UpdatedAt = now;
    }

    private static void Validate(string name, string? description)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Guard.MaxLength(name, ExamSessionConstraints.NameMaxLength, nameof(name));
        Guard.MaxLength(description, ExamSessionConstraints.DescriptionMaxLength, nameof(description));
    }
}