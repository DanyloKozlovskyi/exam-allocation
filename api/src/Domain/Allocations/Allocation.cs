using Domain.Common;
using Domain.Participants;
using Domain.Rooms;
using Domain.SolveRuns;

namespace Domain.Allocations;

public class Allocation : IEntity<AllocationId>
{
    private Allocation(AllocationId id, SolveRunId runId, int index, AllocationObjective objective, DateTimeOffset createdAt)
    {
        Id = id;
        RunId = runId;
        Index = index;
        Objective = objective;
        CreatedAt = createdAt;
    }

    private Allocation()
    {
        Id = AllocationId.Empty();
        RunId = SolveRunId.Empty();
        Objective = new AllocationObjective();
    }

    public AllocationId Id { get; private set; }

    public SolveRunId RunId { get; private set; }

    public int Index { get; private set; }

    public AllocationObjective Objective { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<AllocationItem> Items { get; private set; } = [];

    public static Allocation New(
        AllocationId id,
        SolveRunId runId,
        int index,
        AllocationObjective objective,
        IReadOnlyList<(ParticipantId ParticipantId, RoomId RoomId, SeatId SeatId)> items,
        DateTimeOffset now)
    {
        Guard.GreaterThanOrEqual(index, AllocationConstraints.ManualIndex, nameof(index));
        Guard.NotNull(objective, nameof(objective));
        var allocation = new Allocation(id, runId, index, objective, now);
        foreach (var (participantId, roomId, seatId) in items)
        {
            allocation.Items.Add(AllocationItem.New(AllocationItemId.New(), id, participantId, roomId, seatId));
        }

        return allocation;
    }
}