namespace Infrastructure.ImportExport.Constants;

/// <summary>Column headers and labels used in XLSX / CSV / PDF exports.</summary>
public static class ExportHeaders
{
    public const string Number = "№";
    public const string LastName = "Прізвище";
    public const string FullName = "ПІБ";
    public const string FirstName = "Ім'я";
    public const string MiddleName = "По батькові";
    public const string Group = "Група";
    public const string School = "Навч. заклад";
    public const string Center = "Центр";
    public const string Address = "Адреса";
    public const string Room = "Аудиторія";
    public const string Row = "Ряд";
    public const string Desk = "Парта";
    public const string Place = "Місце";
    public const string Signature = "Підпис";
    public const string Capacity = "Місткість";
    public const string Filled = "Заповнено";
    public const string RoomsUsed = "Аудиторій використано";

    public const string SessionLabel = "Сесія";
    public const string ExamDateLabel = "Дата іспиту";
    public const string GeneratedAtLabel = "Сформовано";
    public const string AllocationIndexLabel = "Варіант";
    public const string CostLabel = "Вартість";
    public const string SoftPenaltyLabel = "Штраф м'яких правил";
    public const string CentersTitle = "Центри";
    public const string SeatingTitle = "Схема розміщення";
    public const string ListTitle = "Список учасників";
    public const string BoardLabel = "Дошка";
    public const string DeskSeparator = " / ";

    public const string DateFormat = "dd.MM.yyyy";
    public const string DateTimeFormat = "dd.MM.yyyy HH:mm";
    public const string CostFormat = "0.00";

    /// <summary>Model coordinates are 0-based; exports show 1-based numbers.</summary>
    public const int DisplayOffset = 1;

    public static IReadOnlyList<string> ParticipantColumns { get; } =
    [
        Number, LastName, FirstName, MiddleName, Group, School, Center, Address, Room, Row, Desk, Place,
    ];
}