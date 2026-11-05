using Api.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface ITestCentersControllerService
{
    Task<IReadOnlyList<TestCenterDto>> GetAll(Guid sessionId, CancellationToken cancellationToken);

    Task<Option<TestCenterDto>> Get(Guid sessionId, Guid centerId, CancellationToken cancellationToken);
}