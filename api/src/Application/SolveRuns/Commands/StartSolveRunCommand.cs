using Application.SolveRuns.Exceptions;
using Domain.SolveRuns;
using LanguageExt;
using MediatR;

namespace Application.SolveRuns.Commands;

public record StartSolveRunCommand : IRequest<Either<SolveRunException, SolveRun>>
{
    public Guid SessionId { get; init; }

    public SolveRunOptions Options { get; init; } = new();
}

public class StartSolveRunCommandHandler
    : IRequestHandler<StartSolveRunCommand, Either<SolveRunException, SolveRun>>
{
    public Task<Either<SolveRunException, SolveRun>> Handle(
        StartSolveRunCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}