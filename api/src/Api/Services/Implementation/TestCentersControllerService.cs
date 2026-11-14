using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Domain.Centers;
using Domain.Sessions;
using LanguageExt;

namespace Api.Services.Implementation;

public class TestCentersControllerService(ITestCenterQueries centerQueries) : ITestCentersControllerService
{
    public async Task<IReadOnlyList<TestCenterDto>> GetAll(Guid sessionId, CancellationToken cancellationToken)
    {
        var centers = await centerQueries.GetAll(new ExamSessionId(sessionId), cancellationToken);
        return centers.Select(TestCenterDto.FromDomain).ToList();
    }

    public async Task<Option<TestCenterDto>> Get(Guid sessionId, Guid centerId, CancellationToken cancellationToken)
    {
        var center = await centerQueries.GetById(new ExamSessionId(sessionId), new TestCenterId(centerId), cancellationToken);
        return center.Map(TestCenterDto.FromDomain);
    }
}