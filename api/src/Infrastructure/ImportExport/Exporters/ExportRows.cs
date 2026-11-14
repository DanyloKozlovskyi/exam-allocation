using System.Globalization;
using Application.Allocations.Models;
using Infrastructure.ImportExport.Constants;

namespace Infrastructure.ImportExport.Exporters;

public sealed record ExportRow(ExportCenter Center, ExportRoom Room, ExportSeat Seat, ExportParticipant Participant)
{
    public int DisplayRow => Seat.Row + ExportHeaders.DisplayOffset;

    public int DisplayDesk => Seat.Column + ExportHeaders.DisplayOffset;

    public int DisplayPlace => Seat.SeatIndex + ExportHeaders.DisplayOffset;
}

/// <summary>Flattening, sorting and naming helpers shared by the exporters.</summary>
public static class ExportRows
{
    public static StringComparer NameComparer { get; } =
        StringComparer.Create(CultureInfo.GetCultureInfo(ExportLayout.CultureName), ignoreCase: true);

    public static IEnumerable<ExportRow> Occupied(ExportCenter center, ExportRoom room)
        => room.Seats
            .Where(x => x.Participant is not null)
            .Select(x => new ExportRow(center, room, x, x.Participant!));

    /// <summary>All seated participants sorted by last, first and middle name.</summary>
    public static IReadOnlyList<ExportRow> ByName(AllocationExportModel model)
        => SortByName(model.Centers.SelectMany(c => c.Rooms.SelectMany(r => Occupied(c, r)))).ToList();

    public static IOrderedEnumerable<ExportRow> SortByName(IEnumerable<ExportRow> rows)
        => rows
            .OrderBy(x => x.Participant.LastName, NameComparer)
            .ThenBy(x => x.Participant.FirstName, NameComparer)
            .ThenBy(x => x.Participant.MiddleName ?? string.Empty, NameComparer);

    public static IReadOnlyList<ExportRow> BySeat(ExportCenter center, ExportRoom room)
        => Occupied(center, room)
            .OrderBy(x => x.Seat.Row)
            .ThenBy(x => x.Seat.Column)
            .ThenBy(x => x.Seat.SeatIndex)
            .ToList();

    /// <summary>Values of the "all participants" table (<see cref="ExportHeaders.ParticipantColumns"/>).</summary>
    public static IReadOnlyList<object?> ParticipantValues(int number, ExportRow row) =>
    [
        number,
        row.Participant.LastName,
        row.Participant.FirstName,
        row.Participant.MiddleName,
        row.Participant.Group,
        row.Participant.School,
        row.Center.Name,
        row.Center.Address,
        row.Room.Name,
        row.DisplayRow,
        row.DisplayDesk,
        row.DisplayPlace,
    ];

    public static string FullName(ExportParticipant participant)
        => string.Join(ImportValues.Space, new[] { participant.LastName, participant.FirstName, participant.MiddleName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    /// <summary>Surnames at one desk (row, column), ordered by seat index.</summary>
    public static string DeskText(ExportRoom room, int row, int column)
        => string.Join(
            ExportHeaders.DeskSeparator,
            room.Seats
                .Where(x => x.Row == row && x.Column == column && x.Participant is not null)
                .OrderBy(x => x.SeatIndex)
                .Select(x => x.Participant!.LastName));

    /// <summary>Number of aisles to the left of a 0-based desk column.</summary>
    public static int AislesBefore(ExportRoom room, int column)
        => room.Layout.AislesAfterColumns.Count(x => x < column);

    public static string RoomTitle(ExportCenter center, ExportRoom room)
        => $"{center.Name} {ExportSheets.RoomSheetSeparator} {room.Name}";

    public static string FormatDate(DateOnly? date)
        => date?.ToString(ExportHeaders.DateFormat, CultureInfo.InvariantCulture) ?? string.Empty;
}