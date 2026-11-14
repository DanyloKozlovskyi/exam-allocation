using ClosedXML.Excel;
using Tools.Shared.SyntheticData;

namespace DataGen;

/// <summary>Writes participants in the import template format (docs/07 "Template").</summary>
internal static class ParticipantsWorkbookWriter
{
    public const string SheetName = "Учасники";

    private const int HeaderRow = 1;
    private const int FirstColumn = 1;
    private const string Yes = "так";
    private const string No = "ні";

    /// <summary>Fixed document timestamp so the workbook content is reproducible.</summary>
    private static readonly DateTime DocumentTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly string[] Headers =
    [
        TemplateHeaders.ExternalId,
        TemplateHeaders.LastName,
        TemplateHeaders.FirstName,
        TemplateHeaders.MiddleName,
        TemplateHeaders.Group,
        TemplateHeaders.School,
        TemplateHeaders.NeedsAccessibleSeat,
        TemplateHeaders.Notes,
    ];

    public static void Write(string path, IReadOnlyList<ParticipantRecord> participants)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Created = DocumentTimestamp;
        workbook.Properties.Modified = DocumentTimestamp;
        var sheet = workbook.Worksheets.Add(SheetName);

        for (var i = 0; i < Headers.Length; i++)
        {
            sheet.Cell(HeaderRow, FirstColumn + i).Value = Headers[i];
        }

        sheet.Row(HeaderRow).Style.Font.Bold = true;

        for (var p = 0; p < participants.Count; p++)
        {
            var participant = participants[p];
            var row = HeaderRow + 1 + p;
            object?[] values =
            [
                participant.ExternalId,
                participant.LastName,
                participant.FirstName,
                participant.MiddleName,
                participant.Group,
                participant.School,
                participant.NeedsAccessibleSeat ? Yes : No,
                participant.Notes,
            ];

            for (var i = 0; i < values.Length; i++)
            {
                sheet.Cell(row, FirstColumn + i).Value = values[i]?.ToString() ?? string.Empty;
            }
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
    }

    /// <summary>Ukrainian template headers (mirror of Infrastructure/ImportExport/Constants, which tools cannot reference).</summary>
    private static class TemplateHeaders
    {
        public const string ExternalId = "ID";
        public const string LastName = "Прізвище";
        public const string FirstName = "Ім'я";
        public const string MiddleName = "По батькові";
        public const string Group = "Група";
        public const string School = "Навчальний заклад";
        public const string NeedsAccessibleSeat = "Особливі потреби";
        public const string Notes = "Примітки";
    }
}