using Api.Services.Abstract;
using Application.Allocations.Models;
using Infrastructure.ImportExport.Constants;
using Infrastructure.ImportExport.Templates;

namespace Api.Services.Implementation;

public class ImportsControllerService : IImportsControllerService
{
    public ExportFile Template()
        => new(ExportFileNames.TemplateFileName, ContentTypes.Xlsx, ParticipantImportTemplate.Build());
}