using Api.Dtos;
using Api.Services.Abstract;
using LanguageExt;

namespace Api.Services.Implementation;

public class TestCentersControllerService : ITestCentersControllerService
{
    public Task<IReadOnlyList<TestCenterDto>> GetAll(Guid sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<TestCenterDto>> Get(Guid sessionId, Guid centerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}