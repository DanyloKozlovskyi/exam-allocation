using System.Globalization;
using Application.Allocations.Models;
using Application.Common.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using Infrastructure.ImportExport.Constants;

namespace Infrastructure.ImportExport.Exporters;

/// <summary>Same columns as the <see cref="ExportSheets.AllParticipants"/> sheet; UTF-8 with BOM, <c>;</c>-delimited.</summary>
public class CsvAllocationExporter : IAllocationExporter
{
    private static readonly CsvConfiguration Configuration = new(CultureInfo.InvariantCulture)
    {
        Delimiter = CsvSettings.DefaultDelimiter.ToString(),
        NewLine = CsvSettings.ExportNewLine,
    };

    public ExportFormat Format => ExportFormat.Csv;

    public async Task<ExportFile> Export(AllocationExportModel model, Guid? roomId, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, CsvSettings.Utf8WithBom, leaveOpen: true))
        await using (var csv = new CsvWriter(writer, Configuration))
        {
            foreach (var header in ExportHeaders.ParticipantColumns)
            {
                csv.WriteField(header);
            }

            await csv.NextRecordAsync();
            var rows = ExportRows.ByName(model);
            for (var i = 0; i < rows.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var value in ExportRows.ParticipantValues(i + 1, rows[i]))
                {
                    csv.WriteField(value);
                }

                await csv.NextRecordAsync();
            }
        }

        return new ExportFile(ExportFileNames.ForAllocation(model, FileExtensions.Csv), ContentTypes.Csv, stream.ToArray());
    }
}