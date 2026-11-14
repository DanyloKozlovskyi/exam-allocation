using System.Text;
using Application.Allocations.Models;
using Application.Common.Settings;
using Application.Imports.Models;
using ClosedXML.Excel;
using Domain.Allocations;
using Domain.Rooms;
using Domain.ValueObjects;

namespace Infrastructure.Tests.ImportExport;

/// <summary>Programmatic fixtures for import/export tests.</summary>
public static class ImportExportTestData
{
    public const string SessionName = "Пробне ЗНО 2026";
    public const string CenterName = "Київський політехнічний ліцей №1";
    public const string RoomName = "Ауд. 214";
    public const string SecondRoomName = "Ауд. 215 [корпус: Б]";

    public static ApplicationSettings Settings(int maxRows = ImportSettings.DefaultMaxRows)
        => new() { Import = new ImportSettings { MaxRows = maxRows } };

    public static byte[] Xlsx(params (string Sheet, string?[][] Rows)[] sheets)
    {
        using var workbook = new XLWorkbook();
        foreach (var (name, rows) in sheets)
        {
            var sheet = workbook.Worksheets.Add(name);
            for (var r = 0; r < rows.Length; r++)
            {
                for (var c = 0; c < rows[r].Length; c++)
                {
                    if (rows[r][c] is { } value)
                    {
                        sheet.Cell(r + 1, c + 1).Value = value;
                    }
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] Text(string text, Encoding encoding, bool withPreamble = false)
        => withPreamble ? [.. encoding.GetPreamble(), .. encoding.GetBytes(text)] : encoding.GetBytes(text);

    public static WorkbookSheet Sheet(params string?[][] rows) => new("Аркуш1", rows);

    public static ColumnMapping TemplateMapping(IReadOnlyDictionary<string, string>? attributes = null) => new(
        new Dictionary<ParticipantField, string>
        {
            [ParticipantField.ExternalId] = "ID",
            [ParticipantField.LastName] = "Прізвище",
            [ParticipantField.FirstName] = "Ім'я",
            [ParticipantField.MiddleName] = "По батькові",
            [ParticipantField.Group] = "Група",
            [ParticipantField.School] = "Навчальний заклад",
            [ParticipantField.NeedsAccessibleSeat] = "Особливі потреби",
            [ParticipantField.Notes] = "Примітки",
        },
        attributes ?? new Dictionary<string, string>());

    public static string?[] TemplateHeader() => ["ID", "Прізвище", "Ім'я", "По батькові", "Група", "Навчальний заклад", "Особливі потреби", "Примітки"];

    public static ImportOptions Options(ColumnMapping mapping, DedupeStrategy dedupe = DedupeStrategy.ExternalId, int headerRow = 1)
        => new("Аркуш1", headerRow, mapping, ImportMode.Append, dedupe);

    public static AllocationExportModel ExportModel()
    {
        var layout = RoomLayout.Create(3, 4, 2, [1], [], []);
        var first = Room(Guid.NewGuid(), RoomName, layout, 7);
        var second = Room(Guid.NewGuid(), SecondRoomName, RoomLayout.Create(2, 2, 1, [], [], []), 3);
        var duplicateName = Room(Guid.NewGuid(), RoomName, RoomLayout.Create(1, 1, 1, [], [], []), 1);
        var center = new ExportCenter(Guid.NewGuid(), CenterName, "вул. Хрещатик, 1", 50.4501, 30.5234, [first, second, duplicateName]);
        return new AllocationExportModel(
            SessionName,
            new DateOnly(2026, 5, 20),
            0,
            new AllocationObjective { RoomsUsed = 3, Cost = 12.5m, SoftPenalty = 3 },
            new DateTimeOffset(2026, 11, 5, 10, 30, 0, TimeSpan.Zero),
            [center]);
    }

    private static ExportRoom Room(Guid id, string name, RoomLayout layout, int participants)
    {
        var seats = new List<ExportSeat>();
        var number = 0;
        for (var r = 0; r < layout.Rows; r++)
        {
            for (var c = 0; c < layout.Columns; c++)
            {
                for (var s = 0; s < layout.SeatsPerDesk; s++)
                {
                    var participant = number < participants
                        ? new ExportParticipant(Guid.NewGuid(), number % 2 == 0 ? $"EXT-{name.Length}-{number}" : null, $"Прізвище{number}", $"Ім'я{number}", number % 3 == 0 ? null : "Іванович", "11-А", "Школа №5")
                        : null;
                    seats.Add(new ExportSeat(SeatKey.From(r, c, s).Value, r, c, s, participant));
                    number++;
                }
            }
        }

        return new ExportRoom(id, name, layout.Rows * layout.Columns * layout.SeatsPerDesk, layout, seats);
    }
}