using Application.Common.Interfaces.Repositories;
using Application.Rules.Exceptions;
using Domain.Rules;
using Domain.Rules.Parameters;
using Domain.Sessions;
using LanguageExt;
using MediatR;
using Rule = Domain.Rules.Rule;

namespace Application.Rules.Commands;

public record CreateRuleCommand : IRequest<Either<RuleException, Rule>>
{
    public Guid SessionId { get; init; }

    public RuleParameters? Parameters { get; init; }

    public bool IsHard { get; init; }

    public int Weight { get; init; } = RuleConstraints.DefaultWeight;

    public bool IsEnabled { get; init; } = true;

    public string? DisplayName { get; init; }
}

public class CreateRuleCommandHandler(
    IExamSessionRepository sessionRepository,
    IRuleRepository ruleRepository)
    : IRequestHandler<CreateRuleCommand, Either<RuleException, Rule>>
{
    public Task<Either<RuleException, Rule>> Handle(
        CreateRuleCommand request,
        CancellationToken cancellationToken)
    {
        var id = RuleId.New();
        var sessionId = new ExamSessionId(request.SessionId);

        return EnsureSessionExists(id, sessionId, cancellationToken)
            .BindAsync(_ => CreateEntity(id, sessionId, request, cancellationToken));
    }

    private async Task<Either<RuleException, ExamSession>> EnsureSessionExists(RuleId id, ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetById(sessionId, cancellationToken);
        return session.Match<Either<RuleException, ExamSession>>(
            s => s,
            () => new RuleSessionNotFoundException(id, sessionId.Value));
    }

    private async Task<Either<RuleException, Rule>> CreateEntity(
        RuleId id,
        ExamSessionId sessionId,
        CreateRuleCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = Rule.New(id, sessionId, request.Parameters!, request.IsHard, request.Weight, request.IsEnabled, request.DisplayName);
            return await ruleRepository.AddAsync(entity, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new RuleParametersInvalidException(id, exception.Message);
        }
        catch (Exception exception)
        {
            return new RuleUnknownException(id, exception);
        }
    }
}