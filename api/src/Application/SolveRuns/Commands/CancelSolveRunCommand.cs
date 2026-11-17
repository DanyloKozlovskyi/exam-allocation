using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.SolveRuns.Exceptions;
using Domain.SolveRuns;
using LanguageExt;
using MediatR;

namespace Application.SolveRuns.Commands;

public record CancelSolveRunCommand : IRequest<Either<SolveRunException, SolveRun>>
{
    public Guid RunId { get; init; }
}

/// <summary>Queued runs are cancelled immediately; running runs get their token cancelled and are completed by the worker.</summary>
public class CancelSolveRunCommandHandler(
    ISolveRunRepository runRepository,
    ISolveRunCancellation cancellation,
    ISolveProgressNotifier notifier,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CancelSolveRunCommand, Either<SolveRunException, SolveRun>>
{
    public async Task<Either<SolveRunException, SolveRun>> Handle(
        CancelSolveRunCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new SolveRunId(request.RunId);
        var found = await runRepository.GetById(runId, cancellationToken);
        return found.IsSome
            ? await Cancel(found.Match(x => x, () => throw new InvalidOperationException()), cancellationToken)
            : new SolveRunNotFoundException(runId);
    }

    private async Task<Either<SolveRunException, SolveRun>> Cancel(SolveRun run, CancellationToken cancellationToken)
    {
        if (!run.IsActive)
        {
            return new SolveRunNotCancellableException(run.Id);
        }

        if (run.Status == SolveRunStatus.Running && cancellation.Cancel(run.Id))
        {
            return run;
        }

        try
        {
            run.Complete(SolveRunStatus.Cancelled, [], null, null, dateTimeProvider.UtcNow);
            if (await runRepository.TryUpdate(run, cancellationToken))
            {
                await notifier.NotifyCompleted(run, cancellationToken);
                return run;
            }

            // The worker started (or finished) it meanwhile: cancel through the token, or report the final state.
            var current = await runRepository.GetById(run.Id, cancellationToken);
            return current.Match<Either<SolveRunException, SolveRun>>(
                latest => latest.Status == SolveRunStatus.Running && cancellation.Cancel(latest.Id)
                    ? latest
                    : new SolveRunNotCancellableException(latest.Id),
                () => new SolveRunNotFoundException(run.Id));
        }
        catch (Exception exception)
        {
            return new SolveRunUnknownException(run.Id, exception);
        }
    }
}