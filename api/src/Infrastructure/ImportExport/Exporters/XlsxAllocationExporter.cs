using System.Globalization;
using Application.Allocations.Models;
using Application.Common.Interfaces;
using ClosedXML.Excel;
using Infrastructure.ImportExport.Constants;

namespace Infrastructure.ImportExport.Exporters;

/// <summary>
/// Sheets: <see cref="ExportSheets.Summary"/>, <see cref="ExportSheets.AllParticipants"/> and one per room
/// (list sorted by seat followed by a seating grid; aisles are narrow empty columns).
/// </summary>
public class XlsxAllocationExporter : IAllocationExporter
{
    public ExportFormat Format => ExportFormat.Xlsx;

    public Task<ExportFile> Export(AllocationExportModel model, Guid? roomId, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        WriteSummary(workbook.Worksheets.Add(ExportSheets.Summary), model);
        WriteAllParticipants(workbook.Worksheets.Add(ExportSheets.AllParticipants), model);

        var names = new SheetNames([ExportSheets.Summary, ExportSheets.AllParticipants]);
        foreach (var center in model.Centers)
        {
            foreach (var room in center.Rooms)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteRoom(workbook.Worksheets.Add(names.ForRoom(center.Name, room.Name)), center, room);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(new ExportFile(ExportFileNames.ForAllocation(model, FileExtensions.Xlsx), ContentTypes.Xlsx, stream.ToArray()));
    }

    private static void WriteSummary(IXLWorksheet sheet, AllocationExportModel model)
    {
        var row = ExportLayout.FirstRow;
        void Line(string label, object value)
        {
            sheet.Cell(row, ExportLayout.FirstColumn).Value = label;
            sheet.Cell(row, ExportLayout.FirstColumn).Style.Font.Bold = true;
            sheet.Cell(row, ExportLayout.FirstColumn + 1).Value = XLCellValue.FromObject(value);
            row++;
        }

        Line(ExportHeaders.SessionLabel, model.SessionName);
        Line(ExportHeaders.ExamDateLabel, ExportRows.FormatDate(model.ExamDate));
        Line(ExportHeaders.GeneratedAtLabel, model.GeneratedAt.ToString(ExportHeaders.DateTimeFormat, CultureInfo.InvariantCulture));
        Line(ExportHeaders.AllocationIndexLabel, model.AllocationIndex);
        Line(ExportHeaders.RoomsUsed, model.Objective.RoomsUsed);
        Line(ExportHeaders.CostLabel, model.Objective.Cost);
        Line(ExportHeaders.SoftPenaltyLabel, model.Objective.SoftPenalty);

        row += ExportLayout.SectionGapRows;
        sheet.Cell(row++, ExportLayout.FirstColumn).Value = ExportHeaders.CentersTitle;
        var headerRow = row;
        WriteHeader(sheet, row++, [ExportHeaders.Center, ExportHeaders.Address, ExportHeaders.RoomsUsed, ExportHeaders.Capacity, ExportHeaders.Filled]);
        foreach (var center in model.Centers)
        {
            var filled = center.Rooms.Sum(r => r.Seats.Count(s => s.Participant is not null));
            var used = center.Rooms.Count(r => r.Seats.Any(s => s.Participant is not null));
            WriteValues(sheet, row++, [center.Name, center.Address, used, center.Rooms.Sum(r => r.Capacity), filled]);
        }

        sheet.Range(headerRow, ExportLayout.FirstColumn, row - 1, ExportLayout.FirstColumn + 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sheet.Columns().AdjustToContents();
    }

    private static void WriteAllParticipants(IXLWorksheet sheet, AllocationExportModel model)
    {
        WriteHeader(sheet, ExportLayout.FirstRow, ExportHeaders.ParticipantColumns);
        var rows = ExportRows.ByName(model);
        for (var i = 0; i < rows.Count; i++)
        {
            WriteValues(sheet, ExportLayout.FirstRow + 1 + i, ExportRows.ParticipantValues(i + 1, rows[i]));
        }

        var lastRow = ExportLayout.FirstRow + rows.Count;
        sheet.Range(ExportLayout.FirstRow, ExportLayout.FirstColumn, lastRow, ExportHeaders.ParticipantColumns.Count).SetAutoFilter();
        sheet.SheetView.FreezeRows(ExportLayout.FirstRow);
        sheet.Columns().AdjustToContents();
    }

    private static void WriteRoom(IXLWorksheet sheet, ExportCenter center, ExportRoom room)
    {
        var row = ExportLayout.FirstRow;
        sheet.Cell(row, ExportLayout.FirstColumn).Value = ExportRows.RoomTitle(center, room);
        sheet.Cell(row++, ExportLayout.FirstColumn).Style.Font.Bold = true;

        WriteHeader(sheet, row++, [ExportHeaders.Number, ExportHeaders.LastName, ExportHeaders.FirstName, ExportHeaders.MiddleName, ExportHeaders.Group, ExportHeaders.Row, ExportHeaders.Desk, ExportHeaders.Place]);
        var seated = ExportRows.BySeat(center, room);
        for (var i = 0; i < seated.Count; i++)
        {
            var x = seated[i];
            WriteValues(sheet, row++, [i + 1, x.Participant.LastName, x.Participant.FirstName, x.Participant.MiddleName, x.Participant.Group, x.DisplayRow, x.DisplayDesk, x.DisplayPlace]);
        }

        sheet.Columns().AdjustToContents();
        row += ExportLayout.SectionGapRows;
        WriteGrid(sheet, row, room);
    }

    private static void WriteGrid(IXLWorksheet sheet, int startRow, ExportRoom room)
    {
        var layout = room.Layout;
        sheet.Cell(startRow, ExportLayout.FirstColumn).Value = ExportHeaders.SeatingTitle;
        sheet.Cell(startRow, ExportLayout.FirstColumn).Style.Font.Bold = true;

        var boardRow = startRow + 1;
        var lastGridColumn = GridColumn(room, layout.Columns - 1);
        var board = sheet.Range(boardRow, ExportLayout.FirstColumn + 1, boardRow, lastGridColumn).Merge();
        board.Value = ExportHeaders.BoardLabel;
        board.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        board.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

        for (var r = 0; r < layout.Rows; r++)
        {
            var sheetRow = boardRow + 1 + r;
            sheet.Cell(sheetRow, ExportLayout.FirstColumn).Value = $"{ExportHeaders.Row} {r + ExportHeaders.DisplayOffset}";
            for (var c = 0; c < layout.Columns; c++)
            {
                var cell = sheet.Cell(sheetRow, GridColumn(room, c));
                cell.Value = ExportRows.DeskText(room, r, c);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.WrapText = true;
            }
        }

        for (var c = 0; c < layout.Columns; c++)
        {
            sheet.Column(GridColumn(room, c)).Width = ExportLayout.DeskColumnWidth;
        }

        foreach (var aisle in layout.AislesAfterColumns.Where(x => x < layout.Columns - 1))
        {
            sheet.Column(GridColumn(room, aisle) + 1).Width = ExportLayout.AisleColumnWidth;
        }
    }

    /// <summary>Sheet column of a 0-based desk column: after the row-label column, shifted by aisles.</summary>
    private static int GridColumn(ExportRoom room, int column)
        => ExportLayout.FirstColumn + 1 + column + ExportRows.AislesBefore(room, column);

    private static void WriteHeader(IXLWorksheet sheet, int row, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            var cell = sheet.Cell(row, ExportLayout.FirstColumn + i);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
    }

    private static void WriteValues(IXLWorksheet sheet, int row, IReadOnlyList<object?> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            sheet.Cell(row, ExportLayout.FirstColumn + i).Value = XLCellValue.FromObject(values[i]);
        }
    }
}