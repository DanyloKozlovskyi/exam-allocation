using Api.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface ISolveRunsControllerService
{
    Task<IReadOnlyList<SolveRunDto>> GetAll(Guid sessionId, CancellationToken cancellationToken);

    Task<Option<SolveRunDto>> Get(Guid runId, CancellationToken cancellationToken);
}