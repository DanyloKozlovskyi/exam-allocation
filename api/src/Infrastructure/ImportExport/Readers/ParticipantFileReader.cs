using System.Globalization;
using System.Text;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.Imports.Models;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Infrastructure.ImportExport.Constants;

namespace Infrastructure.ImportExport.Readers;

/// <summary>
/// Reads .xlsx (ClosedXML) and .csv (CsvHelper) files into raw rows. Row <c>i</c> of a sheet is the
/// sheet's 1-based row <c>i + 1</c>; empty cells are <c>null</c>. Header-row selection is done later by
/// the row validator via <see cref="ImportOptions.HeaderRow"/>.
/// Throws <see cref="InvalidDataException"/> for unsupported or unreadable files and when a sheet has
/// more rows than <see cref="ImportSettings.MaxRows"/>.
/// </summary>
public class ParticipantFileReader(ApplicationSettings settings) : IParticipantFileReader
{
    static ParticipantFileReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public WorkbookPreview Read(Stream content, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.Equals(extension, FileExtensions.Xlsx, StringComparison.OrdinalIgnoreCase))
        {
            return ReadXlsx(content);
        }

        if (string.Equals(extension, FileExtensions.Csv, StringComparison.OrdinalIgnoreCase))
        {
            return ReadCsv(content, fileName);
        }

        throw new InvalidDataException($"Unsupported file type '{extension}'. Expected {FileExtensions.Xlsx} or {FileExtensions.Csv}");
    }

    private WorkbookPreview ReadXlsx(Stream content)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(content);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidDataException("The file is not a valid .xlsx workbook", ex);
        }

        using (workbook)
        {
            var sheets = workbook.Worksheets.Select(ReadWorksheet).ToList();
            return new WorkbookPreview(sheets);
        }
    }

    private WorkbookSheet ReadWorksheet(IXLWorksheet worksheet)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        EnsureRowLimit(lastRow, worksheet.Name);

        var rows = new List<IReadOnlyList<string?>>(lastRow);
        for (var rowNumber = 1; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var cells = new string?[lastColumn];
            for (var column = 1; column <= lastColumn; column++)
            {
                var cell = row.Cell(column);
                cells[column - 1] = cell.IsEmpty() ? null : cell.GetFormattedString();
            }

            rows.Add(cells);
        }

        return new WorkbookSheet(worksheet.Name, rows);
    }

    private WorkbookPreview ReadCsv(Stream content, string fileName)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        var text = Decode(buffer.ToArray());
        var delimiter = DetectDelimiter(text);

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = false,
            BadDataFound = null,
            MissingFieldFound = null,
            DetectColumnCountChanges = false,
        };

        var rows = new List<IReadOnlyList<string?>>();
        using var reader = new StringReader(text);
        using var csv = new CsvParser(reader, configuration);
        try
        {
            while (csv.Read())
            {
                EnsureRowLimit(rows.Count + 1, fileName);
                rows.Add(csv.Record?.Select(x => string.IsNullOrEmpty(x) ? null : x).ToArray() ?? []);
            }
        }
        catch (CsvHelperException ex)
        {
            throw new InvalidDataException("The file is not a valid CSV file", ex);
        }

        return new WorkbookPreview([new WorkbookSheet(Path.GetFileNameWithoutExtension(fileName), rows)]);
    }

    private void EnsureRowLimit(int rowCount, string sheetName)
    {
        if (rowCount > settings.Import.MaxRows)
        {
            throw new InvalidDataException($"Sheet '{sheetName}' contains more than {settings.Import.MaxRows} rows");
        }
    }

    /// <summary>UTF-8 (with or without BOM); Windows-1251 if the bytes are not valid UTF-8.</summary>
    internal static string Decode(byte[] bytes)
    {
        var preamble = Encoding.UTF8.Preamble;
        if (bytes.AsSpan().StartsWith(preamble))
        {
            return Encoding.UTF8.GetString(bytes, preamble.Length, bytes.Length - preamble.Length);
        }

        try
        {
            return CsvSettings.StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(CsvSettings.FallbackCodePage).GetString(bytes);
        }
    }

    /// <summary>Picks the candidate delimiter occurring most often (outside quotes) in the first lines.</summary>
    internal static char DetectDelimiter(string text)
    {
        var counts = CsvSettings.CandidateDelimiters.ToDictionary(x => x, _ => 0);
        var inQuotes = false;
        var lines = 0;
        foreach (var ch in text)
        {
            if (ch == CsvSettings.Quote)
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && counts.ContainsKey(ch))
            {
                counts[ch]++;
            }
            else if (!inQuotes && ch == CsvSettings.NewLine && ++lines >= CsvSettings.DelimiterProbeLines)
            {
                break;
            }
        }

        var best = CsvSettings.CandidateDelimiters.OrderByDescending(x => counts[x]).First();
        return counts[best] > 0 ? best : CsvSettings.DefaultDelimiter;
    }
}