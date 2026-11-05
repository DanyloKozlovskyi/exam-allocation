using Api.Dtos;
using Api.Services.Abstract;
using LanguageExt;

namespace Api.Services.Implementation;

public class RulesControllerService : IRulesControllerService
{
    public Task<IReadOnlyList<RuleDto>> GetAll(Guid sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<RuleDto>> Get(Guid sessionId, Guid ruleId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<RuleTypeInfoDto> RuleTypes()
    {
        throw new NotImplementedException();
    }
}