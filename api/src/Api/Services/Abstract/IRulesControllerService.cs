using Api.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IRulesControllerService
{
    Task<IReadOnlyList<RuleDto>> GetAll(Guid sessionId, CancellationToken cancellationToken);

    Task<Option<RuleDto>> Get(Guid sessionId, Guid ruleId, CancellationToken cancellationToken);

    IReadOnlyList<RuleTypeInfoDto> RuleTypes();
}