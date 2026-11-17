using Application.Common.Interfaces.Queries;
using Application.SolveRuns.Exceptions;
using Application.Solving.Mapping;
using Application.Solving.Services;
using Domain.Sessions;
using Domain.SolveRuns;
using LanguageExt;
using MediatR;
using Solver.Precheck;

namespace Application.SolveRuns.Commands;

public record RunPrecheckCommand : IRequest<Either<SolveRunException, IReadOnlyList<SolveDiagnostic>>>
{
    public Guid SessionId { get; init; }
}

public class RunPrecheckCommandHandler(
    IExamSessionQueries sessionQueries,
    ISolverInputBuilder inputBuilder,
    IPrecheck precheck)
    : IRequestHandler<RunPrecheckCommand, Either<SolveRunException, IReadOnlyList<SolveDiagnostic>>>
{
    public async Task<Either<SolveRunException, IReadOnlyList<SolveDiagnostic>>> Handle(
        RunPrecheckCommand request,
        CancellationToken cancellationToken)
    {
        var sessionId = new ExamSessionId(request.SessionId);
        var session = await sessionQueries.GetById(sessionId, cancellationToken);
        if (session.IsNone)
        {
            return new SolveRunSessionNotFoundException(SolveRunId.Empty(), request.SessionId);
        }

        try
        {
            var input = await inputBuilder.Build(sessionId, new SolveRunOptions(), cancellationToken);
            var diagnostics = precheck.Run(input, cancellationToken);
            return diagnostics.Select(SolverModelMapper.ToDomainDiagnostic).ToList();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new SolveRunUnknownException(SolveRunId.Empty(), exception);
        }
    }
}