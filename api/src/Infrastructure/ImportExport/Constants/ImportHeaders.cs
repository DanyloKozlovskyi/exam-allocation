using Application.Imports.Models;

namespace Infrastructure.ImportExport.Constants;

/// <summary>Template headers (Ukrainian) and synonyms used by the mapping suggester.</summary>
public static class ImportHeaders
{
    public const string ExternalId = "ID";
    public const string LastName = "Прізвище";
    public const string FirstName = "Ім'я";
    public const string MiddleName = "По батькові";
    public const string Group = "Група";
    public const string School = "Навчальний заклад";
    public const string NeedsAccessibleSeat = "Особливі потреби";
    public const string Notes = "Примітки";

    /// <summary>Template columns in order, together with the field each one maps to.</summary>
    public static IReadOnlyList<(string Header, ParticipantField Field)> TemplateColumns { get; } =
    [
        (ExternalId, ParticipantField.ExternalId),
        (LastName, ParticipantField.LastName),
        (FirstName, ParticipantField.FirstName),
        (MiddleName, ParticipantField.MiddleName),
        (Group, ParticipantField.Group),
        (School, ParticipantField.School),
        (NeedsAccessibleSeat, ParticipantField.NeedsAccessibleSeat),
        (Notes, ParticipantField.Notes),
    ];

    /// <summary>Normalized (lower-case, apostrophe-normalized) synonyms per field; matched after <c>HeaderNormalizer.Normalize</c>.</summary>
    public static IReadOnlyDictionary<ParticipantField, IReadOnlyList<string>> Synonyms { get; } =
        new Dictionary<ParticipantField, IReadOnlyList<string>>
        {
            [ParticipantField.LastName] = ["прізвище", "фамилия", "last name", "lastname", "surname"],
            [ParticipantField.FirstName] = ["ім'я", "имя", "first name", "firstname", "name"],
            [ParticipantField.MiddleName] = ["по батькові", "отчество", "middle name", "middlename", "patronymic"],
            [ParticipantField.FullName] = ["піб", "фіо", "фио", "full name", "fullname", "учасник"],
            [ParticipantField.Group] = ["група", "клас", "group", "class"],
            [ParticipantField.School] = ["навчальний заклад", "заклад", "школа", "університет", "school"],
            [ParticipantField.NeedsAccessibleSeat] = ["особливі потреби", "інклюзія", "доступність", "accessible"],
            [ParticipantField.ExternalId] = ["id", "код", "номер", "№"],
            [ParticipantField.Notes] = ["примітки", "примітка", "коментар", "notes", "note", "comment"],
        };
}