using Application.Common.Interfaces;
using Application.Imports.Models;

namespace Application.Imports.Services;

/// <summary>Builds the mapping-step preview (headers, sample rows, suggested mapping) for one sheet and header row.</summary>
public static class ImportPreviewBuilder
{
    public static ImportPreview Build(
        Guid importId,
        WorkbookPreview workbook,
        WorkbookSheet sheet,
        int headerRow,
        IColumnMappingSuggester suggester)
    {
        var headerIndex = headerRow - 1;
        var headers = sheet.Rows.Count > headerIndex
            ? sheet.Rows[headerIndex].Select(x => x?.Trim() ?? string.Empty).ToList()
            : [];
        var sampleRows = sheet.Rows
            .Skip(headerRow)
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .Take(ImportPreview.SampleRowCount)
            .ToList();

        return new ImportPreview(
            importId,
            workbook.Sheets.Select(x => x.Name).ToList(),
            headers,
            sampleRows,
            suggester.Suggest(headers));
    }
}