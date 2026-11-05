using Application.SolveRuns.Exceptions;
using Domain.SolveRuns;
using LanguageExt;
using MediatR;

namespace Application.SolveRuns.Commands;

public record RunPrecheckCommand : IRequest<Either<SolveRunException, IReadOnlyList<SolveDiagnostic>>>
{
    public Guid SessionId { get; init; }
}

public class RunPrecheckCommandHandler
    : IRequestHandler<RunPrecheckCommand, Either<SolveRunException, IReadOnlyList<SolveDiagnostic>>>
{
    public Task<Either<SolveRunException, IReadOnlyList<SolveDiagnostic>>> Handle(
        RunPrecheckCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}