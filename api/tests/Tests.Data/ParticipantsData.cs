using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;

namespace Tests.Data;

public static class ParticipantsData
{
    public const string GroupA = "11-А";
    public const string GroupB = "11-Б";
    public const string SchoolOne = "Школа №1";
    public const string SchoolTwo = "Школа №2";
    public const string LanguageKey = "language";
    public const string English = "англійська";

    public static Participant Ivanenko(ExamSessionId sessionId)
        => Create(sessionId, "1001", "Іваненко", "Іван", GroupA, SchoolOne, false, English);

    public static Participant Petrenko(ExamSessionId sessionId)
        => Create(sessionId, "1002", "Петренко", "Петро", GroupA, SchoolTwo, true, null);

    public static Participant Shevchenko(ExamSessionId sessionId)
        => Create(sessionId, "1003", "Шевченко", "Тарас", GroupB, SchoolOne, false, English);

    public static IReadOnlyList<Participant> All(ExamSessionId sessionId)
        => [Ivanenko(sessionId), Petrenko(sessionId), Shevchenko(sessionId)];

    public static Participant Create(
        ExamSessionId sessionId,
        string? externalId,
        string lastName,
        string firstName,
        string? group,
        string? school,
        bool needsAccessibleSeat,
        string? language)
    {
        var attributes = language is null
            ? new Dictionary<AttributeKey, string>()
            : new Dictionary<AttributeKey, string> { [AttributeKey.From(LanguageKey)] = language };
        return Participant.New(ParticipantId.New(), sessionId, externalId, PersonName.From(lastName, firstName, null), group, school, needsAccessibleSeat, attributes, null);
    }
}