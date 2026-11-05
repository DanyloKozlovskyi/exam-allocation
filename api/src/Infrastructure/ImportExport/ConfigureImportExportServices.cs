using Application.Common.Interfaces;
using Infrastructure.ImportExport.Exporters;
using Infrastructure.ImportExport.Mapping;
using Infrastructure.ImportExport.Readers;
using Infrastructure.ImportExport.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.ImportExport;

public static class ConfigureImportExportServices
{
    public static void AddImportExportServices(this IServiceCollection services)
    {
        services.AddScoped<IParticipantFileReader, ParticipantFileReader>();
        services.AddScoped<IColumnMappingSuggester, ColumnMappingSuggester>();
        services.AddScoped<IParticipantRowValidator, ParticipantRowValidator>();
        services.AddScoped<IAllocationExporter, XlsxAllocationExporter>();
        services.AddScoped<IAllocationExporter, XmlAllocationExporter>();
        services.AddScoped<IAllocationExporter, CsvAllocationExporter>();
        services.AddScoped<IAllocationExporter, PdfAllocationExporter>();
    }
}