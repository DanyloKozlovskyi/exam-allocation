using Api.Dtos;
using Api.Services.Abstract;
using LanguageExt;

namespace Api.Services.Implementation;

public class SolveRunsControllerService : ISolveRunsControllerService
{
    public Task<IReadOnlyList<SolveRunDto>> GetAll(Guid sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<SolveRunDto>> Get(Guid runId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}