using Domain.Sessions;

namespace Tests.Data;

public static class ExamSessionsData
{
    public const string MainSessionName = "Тестова сесія";
    public const string SecondSessionName = "Друга сесія";

    public static ExamSession MainSession(DateTimeOffset now)
        => ExamSession.New(ExamSessionId.New(), MainSessionName, DateOnly.FromDateTime(now.UtcDateTime), null, now);

    public static ExamSession SecondSession(DateTimeOffset now)
        => ExamSession.New(ExamSessionId.New(), SecondSessionName, null, null, now);
}