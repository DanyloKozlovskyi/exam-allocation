using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Domain.Rules;
using Domain.Rules.Parameters;
using Domain.Sessions;
using LanguageExt;

namespace Api.Services.Implementation;

public class RulesControllerService(IRuleQueries ruleQueries) : IRulesControllerService
{
    public async Task<IReadOnlyList<RuleDto>> GetAll(Guid sessionId, CancellationToken cancellationToken)
    {
        var rules = await ruleQueries.GetAll(new ExamSessionId(sessionId), cancellationToken);
        return rules.Select(RuleDto.FromDomain).ToList();
    }

    public async Task<Option<RuleDto>> Get(Guid sessionId, Guid ruleId, CancellationToken cancellationToken)
    {
        var rule = await ruleQueries.GetById(new ExamSessionId(sessionId), new RuleId(ruleId), cancellationToken);
        return rule.Map(RuleDto.FromDomain);
    }

    public IReadOnlyList<RuleTypeInfoDto> RuleTypes()
    {
        return Enum.GetValues<RuleType>()
            .Select(type => new RuleTypeInfoDto(
                type,
                RuleConstraints.AllowsHard(type),
                RuleConstraints.AllowsSoft(type),
                RuleParametersDto.FromDomain(RuleParameters.CreateDefault(type))))
            .ToList();
    }
}