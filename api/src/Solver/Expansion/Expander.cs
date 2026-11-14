using Solver.Layout;
using Solver.Model;

namespace Solver.Expansion;

/// <summary>
/// Expands class-level seatings into individual seat assignments (docs/04 §9).
/// Rooms are processed in <see cref="Guid"/> order; inside a room, seats of a class are taken in
/// seat-key order (row, column, seat) and filled with the class members in <c>SortKey</c> order
/// (alternative 0) or in an order shuffled with <c>Random(randomSeed + alternativeIndex)</c> (i &gt; 0).
/// Pinned participants keep the seat stage 2 gave them.
/// </summary>
public sealed class Expander : IExpander
{
    public IReadOnlyList<SeatAssignment> Expand(
        IReadOnlyList<ParticipantClass> classes,
        IReadOnlyDictionary<Guid, IReadOnlyList<ClassSeatAssignment>> seatingByRoom,
        IReadOnlyList<SolverParticipant> participants,
        int alternativeIndex,
        int randomSeed)
    {
        ArgumentNullException.ThrowIfNull(classes);
        ArgumentNullException.ThrowIfNull(seatingByRoom);
        ArgumentNullException.ThrowIfNull(participants);

        var queues = BuildQueues(classes, participants, alternativeIndex, randomSeed);
        var result = new List<SeatAssignment>();

        foreach (var roomId in seatingByRoom.Keys.Order())
        {
            var seats = seatingByRoom[roomId]
                .OrderBy(a => SeatKeyParser.SortKey(a.SeatKey))
                .ThenBy(a => a.SeatKey, StringComparer.Ordinal);
            foreach (var seat in seats)
            {
                if (seat.PinnedParticipantId is { } pinned)
                {
                    result.Add(new SeatAssignment(pinned, roomId, seat.SeatKey));
                    continue;
                }

                if (seat.ClassIndex is not { } classIndex || !queues.TryGetValue(classIndex, out var queue))
                {
                    throw new InvalidOperationException($"Seat {seat.SeatKey} in room {roomId} references an unknown class.");
                }

                if (!queue.TryDequeue(out var participantId))
                {
                    throw new InvalidOperationException($"Class {classIndex} has more seats than members.");
                }

                result.Add(new SeatAssignment(participantId, roomId, seat.SeatKey));
            }
        }

        return result;
    }

    private static Dictionary<int, Queue<Guid>> BuildQueues(
        IReadOnlyList<ParticipantClass> classes,
        IReadOnlyList<SolverParticipant> participants,
        int alternativeIndex,
        int randomSeed)
    {
        var sortKeys = participants
            .GroupBy(p => p.ParticipantId)
            .ToDictionary(g => g.Key, g => g.First().SortKey);
        var random = alternativeIndex > 0 ? new Random(unchecked(randomSeed + alternativeIndex)) : null;
        var queues = new Dictionary<int, Queue<Guid>>();

        foreach (var participantClass in classes.OrderBy(c => c.Index))
        {
            var members = participantClass.ParticipantIds
                .OrderBy(id => sortKeys.GetValueOrDefault(id, string.Empty), StringComparer.Ordinal)
                .ThenBy(id => id)
                .ToArray();
            random?.Shuffle(members);
            queues[participantClass.Index] = new Queue<Guid>(members);
        }

        return queues;
    }
}