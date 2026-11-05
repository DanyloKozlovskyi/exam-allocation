using Application.SolveRuns.Exceptions;
using Domain.SolveRuns;
using LanguageExt;
using MediatR;

namespace Application.SolveRuns.Commands;

public record CancelSolveRunCommand : IRequest<Either<SolveRunException, SolveRun>>
{
    public Guid RunId { get; init; }
}

public class CancelSolveRunCommandHandler
    : IRequestHandler<CancelSolveRunCommand, Either<SolveRunException, SolveRun>>
{
    public Task<Either<SolveRunException, SolveRun>> Handle(
        CancelSolveRunCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}