using System.Threading.Channels;
using Application.Common.Interfaces;
using Domain.SolveRuns;

namespace Infrastructure.Solving;

public class SolveQueue : ISolveQueue
{
    private readonly Channel<SolveRunId> channel = Channel.CreateUnbounded<SolveRunId>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    public ValueTask EnqueueAsync(SolveRunId runId, CancellationToken cancellationToken)
        => channel.Writer.WriteAsync(runId, cancellationToken);

    public IAsyncEnumerable<SolveRunId> ReadAllAsync(CancellationToken cancellationToken)
        => channel.Reader.ReadAllAsync(cancellationToken);
}