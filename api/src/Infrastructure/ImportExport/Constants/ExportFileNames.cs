using System.Globalization;
using Application.Allocations.Models;

namespace Infrastructure.ImportExport.Constants;

public static class ExportFileNames
{
    public const string AllocationPrefix = "allocation";
    public const string ManualSuffix = "manual";
    public const string RoomSegment = "room";
    public const string Separator = "-";
    public const string TemplateFileName = "participants-template" + FileExtensions.Xlsx;

    public static string ForAllocation(AllocationExportModel model, string extension, Guid? roomId = null)
    {
        var index = model.AllocationIndex < 0
            ? ManualSuffix
            : model.AllocationIndex.ToString(CultureInfo.InvariantCulture);
        var room = roomId is { } id ? $"{Separator}{RoomSegment}{Separator}{id:N}" : string.Empty;
        return $"{AllocationPrefix}{Separator}{index}{room}{extension}";
    }
}