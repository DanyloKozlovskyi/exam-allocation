using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.SolveRuns.Exceptions;
using Application.Solving.Services;
using Domain.Sessions;
using Domain.SolveRuns;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.SolveRuns.Commands;

public record StartSolveRunCommand : IRequest<Either<SolveRunException, SolveRun>>
{
    public Guid SessionId { get; init; }

    public SolveRunOptions Options { get; init; } = new();
}

public class StartSolveRunCommandHandler(
    IExamSessionQueries sessionQueries,
    ISolveRunRepository runRepository,
    ISessionFingerprint fingerprint,
    ISolveQueue queue,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<StartSolveRunCommand, Either<SolveRunException, SolveRun>>
{
    public Task<Either<SolveRunException, SolveRun>> Handle(
        StartSolveRunCommand request,
        CancellationToken cancellationToken)
    {
        var sessionId = new ExamSessionId(request.SessionId);
        return EnsureSessionExists(sessionId, request.SessionId, cancellationToken)
            .BindAsync(_ => EnsureNoActiveRun(sessionId, cancellationToken))
            .BindAsync(_ => Start(sessionId, request.Options, cancellationToken));
    }

    private async Task<Either<SolveRunException, Unit>> EnsureSessionExists(ExamSessionId sessionId, Guid rawId, CancellationToken cancellationToken)
    {
        var session = await sessionQueries.GetById(sessionId, cancellationToken);
        return session.Match<Either<SolveRunException, Unit>>(
            _ => Unit.Default,
            () => new SolveRunSessionNotFoundException(SolveRunId.Empty(), rawId));
    }

    private async Task<Either<SolveRunException, Unit>> EnsureNoActiveRun(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        var active = await runRepository.GetActive(sessionId, cancellationToken);
        return active.Match<Either<SolveRunException, Unit>>(
            run => new SolveRunAlreadyActiveException(run.Id),
            () => Unit.Default);
    }

    private async Task<Either<SolveRunException, SolveRun>> Start(ExamSessionId sessionId, SolveRunOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var hash = await fingerprint.Compute(sessionId, cancellationToken);
            var run = SolveRun.New(SolveRunId.New(), sessionId, options, hash, dateTimeProvider.UtcNow);
            if (!await runRepository.TryAddActive(run, cancellationToken))
            {
                // Lost a race with a concurrent create: the database allows one active run per session.
                var active = await runRepository.GetActive(sessionId, cancellationToken);
                return new SolveRunAlreadyActiveException(active.Match(x => x.Id, () => run.Id));
            }

            await queue.EnqueueAsync(run.Id, cancellationToken);
            return run;
        }
        catch (Exception exception)
        {
            return new SolveRunUnknownException(SolveRunId.Empty(), exception);
        }
    }
}