using Application.Allocations.Models;

namespace Api.Services.Abstract;

public interface IImportsControllerService
{
    ExportFile Template();
}