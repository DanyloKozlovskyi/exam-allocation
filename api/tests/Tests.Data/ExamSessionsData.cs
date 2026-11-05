using Domain.Sessions;

namespace Tests.Data;

public static class ExamSessionsData
{
    public const string MainSessionName = "Тестова сесія";

    public static ExamSession MainSession(DateTimeOffset now)
        => ExamSession.New(ExamSessionId.New(), MainSessionName, DateOnly.FromDateTime(now.UtcDateTime), null, now);
}