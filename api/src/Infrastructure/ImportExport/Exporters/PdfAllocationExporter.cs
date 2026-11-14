using System.Globalization;
using Application.Allocations.Models;
using Application.Common.Interfaces;
using Infrastructure.ImportExport.Constants;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.ImportExport.Exporters;

/// <summary>
/// Per room: a seating grid page (A4 landscape) and an alphabetical list with seat labels and a signature column.
/// <c>roomId</c> limits the document to one room.
/// </summary>
public class PdfAllocationExporter : IAllocationExporter
{
    static PdfAllocationExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ExportFormat Format => ExportFormat.Pdf;

    public Task<ExportFile> Export(AllocationExportModel model, Guid? roomId, CancellationToken cancellationToken)
    {
        var rooms = model.Centers
            .SelectMany(c => c.Rooms.Select(r => (Center: c, Room: r)))
            .Where(x => roomId is null || x.Room.Id == roomId)
            .ToList();

        var document = Document.Create(container =>
        {
            if (rooms.Count == 0)
            {
                container.Page(page =>
                {
                    Setup(page);
                    page.Content().Text(model.SessionName).FontSize(ExportLayout.PdfTitleFontSize).Bold();
                });
            }

            foreach (var (center, room) in rooms)
            {
                cancellationToken.ThrowIfCancellationRequested();
                container.Page(page => GridPage(page, model, center, room));
                container.Page(page => ListPage(page, model, center, room));
            }
        });

        var content = document.GeneratePdf();
        return Task.FromResult(new ExportFile(ExportFileNames.ForAllocation(model, FileExtensions.Pdf, roomId), ContentTypes.Pdf, content));
    }

    private static void Setup(PageDescriptor page)
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(ExportLayout.PdfMargin);
        page.DefaultTextStyle(x => x.FontSize(ExportLayout.PdfFontSize));
    }

    private static void Header(PageDescriptor page, AllocationExportModel model, ExportCenter center, ExportRoom room, string title)
    {
        page.Header().Column(column =>
        {
            column.Item().Text(ExportRows.RoomTitle(center, room)).FontSize(ExportLayout.PdfTitleFontSize).Bold();
            column.Item().Text(string.Join(ExportHeaders.DeskSeparator, new[] { model.SessionName, ExportRows.FormatDate(model.ExamDate), title }.Where(x => x.Length > 0)));
            column.Item().PaddingBottom(ExportLayout.PdfSpacing);
        });
    }

    private static void GridPage(PageDescriptor page, AllocationExportModel model, ExportCenter center, ExportRoom room)
    {
        Setup(page);
        Header(page, model, center, room, ExportHeaders.SeatingTitle);
        var layout = room.Layout;
        var aisles = layout.AislesAfterColumns.Where(x => x < layout.Columns - 1).ToHashSet();

        page.Content().Column(column =>
        {
            column.Spacing(ExportLayout.PdfSpacing);
            column.Item().Border(ExportLayout.PdfBorder).AlignCenter().Padding(ExportLayout.PdfCellPadding).Text(ExportHeaders.BoardLabel).Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(ExportLayout.PdfRowLabelWidth);
                    for (var c = 0; c < layout.Columns; c++)
                    {
                        columns.RelativeColumn();
                        if (aisles.Contains(c))
                        {
                            columns.ConstantColumn(ExportLayout.PdfAisleWidth);
                        }
                    }
                });

                for (var r = 0; r < layout.Rows; r++)
                {
                    table.Cell().Padding(ExportLayout.PdfCellPadding).Text($"{ExportHeaders.Row} {r + ExportHeaders.DisplayOffset}");
                    for (var c = 0; c < layout.Columns; c++)
                    {
                        table.Cell().Border(ExportLayout.PdfBorder).Padding(ExportLayout.PdfCellPadding).Text(ExportRows.DeskText(room, r, c));
                        if (aisles.Contains(c))
                        {
                            table.Cell();
                        }
                    }
                }
            });
        });
    }

    private static void ListPage(PageDescriptor page, AllocationExportModel model, ExportCenter center, ExportRoom room)
    {
        Setup(page);
        Header(page, model, center, room, ExportHeaders.ListTitle);
        var rows = ExportRows.SortByName(ExportRows.Occupied(center, room)).ToList();

        page.Content().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(ExportLayout.PdfNumberWidth);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.ConstantColumn(ExportLayout.PdfSeatColumnWidth);
                columns.ConstantColumn(ExportLayout.PdfSeatColumnWidth);
                columns.ConstantColumn(ExportLayout.PdfSeatColumnWidth);
                columns.ConstantColumn(ExportLayout.PdfSignatureWidth);
            });

            table.Header(header =>
            {
                foreach (var title in new[] { ExportHeaders.Number, ExportHeaders.FullName, ExportHeaders.Group, ExportHeaders.Row, ExportHeaders.Desk, ExportHeaders.Place, ExportHeaders.Signature })
                {
                    header.Cell().Border(ExportLayout.PdfBorder).Background(Colors.Grey.Lighten3).Padding(ExportLayout.PdfCellPadding).Text(title).Bold();
                }
            });

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                foreach (var value in new[]
                         {
                             (i + 1).ToString(CultureInfo.InvariantCulture),
                             ExportRows.FullName(row.Participant),
                             row.Participant.Group ?? string.Empty,
                             row.DisplayRow.ToString(CultureInfo.InvariantCulture),
                             row.DisplayDesk.ToString(CultureInfo.InvariantCulture),
                             row.DisplayPlace.ToString(CultureInfo.InvariantCulture),
                             string.Empty,
                         })
                {
                    table.Cell().Border(ExportLayout.PdfBorder).Padding(ExportLayout.PdfCellPadding).Text(value);
                }
            }
        });
    }
}