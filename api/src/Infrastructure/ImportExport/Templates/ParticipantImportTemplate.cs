using ClosedXML.Excel;
using Infrastructure.ImportExport.Constants;

namespace Infrastructure.ImportExport.Templates;

/// <summary>Builds the participants import template (<c>samples/participants-template.xlsx</c>, <c>GET imports/template</c>).</summary>
public static class ParticipantImportTemplate
{
    public static byte[] Build()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(ExportSheets.Template);
        for (var i = 0; i < ImportHeaders.TemplateColumns.Count; i++)
        {
            var cell = sheet.Cell(ExportLayout.FirstRow, ExportLayout.FirstColumn + i);
            cell.Value = ImportHeaders.TemplateColumns[i].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        sheet.SheetView.FreezeRows(ExportLayout.FirstRow);
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}