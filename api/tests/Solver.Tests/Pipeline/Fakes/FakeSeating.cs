using Solver.Expansion;
using Solver.Model;
using Solver.Seating;

namespace Solver.Tests.Pipeline.Fakes;

/// <summary>
/// Greedy stage-2 double: interleaves classes and puts each unit on the first free seat that does not touch the
/// same value of a hard R3 attribute. Returns Infeasible when greedy gets stuck (the pipeline then adds cuts).
/// </summary>
public sealed class FakeSeatingSolver : ISeatingSolver
{
    private const int DiversityAttempts = 20;
    private readonly FakeSeatGenerator seatGenerator = new();
    private readonly FakeAdjacencyBuilder adjacencyBuilder = new();

    public int Calls { get; private set; }

    public SeatingResult Solve(SeatingRequest request, CancellationToken cancellationToken)
    {
        lock (this)
        {
            Calls++;
        }

        var seats = seatGenerator.Generate(request.Room.Layout);
        if (request.PreviousSeatings.Count == 0)
        {
            return Seat(request, seats, Enumerable.Range(0, seats.Count).ToList());
        }

        for (var attempt = 0; attempt < DiversityAttempts; attempt++)
        {
            var order = Enumerable.Range(0, seats.Count).ToArray();
            new Random(request.RandomSeed + attempt).Shuffle(order);
            var result = Seat(request, seats, order);
            var key = Signature(result.Assignments);
            if (result.Status == SolveStatus.Optimal && request.PreviousSeatings.All(p => Signature(p) != key))
            {
                return result;
            }
        }

        return new SeatingResult(SolveStatus.Infeasible, [], 0, new Dictionary<Guid, long>());
    }

    private SeatingResult Seat(SeatingRequest request, IReadOnlyList<SolverSeat> seats, IReadOnlyList<int> order)
    {
        var hardRules = request.Rules.Where(r => r.IsHard).Select(r => r.Parameters).OfType<NoAdjacentByAttributeParameters>().ToList();
        var neighbours = hardRules
            .Select(p => FakeIndependentSetCalculator.Neighbours(adjacencyBuilder.Build(request.Room.Layout, seats, p.Adjacency, p.SeatLevelSide), seats.Count))
            .ToList();
        var occupant = new IReadOnlyDictionary<string, string>?[seats.Count];
        var taken = new bool[seats.Count];
        var assignments = new List<ClassSeatAssignment>();
        var indexByKey = seats.Select((s, i) => (s.Key, i)).ToDictionary(e => e.Key, e => e.i);

        foreach (var pin in request.Pins.Where(p => p.SeatKey is not null))
        {
            var index = indexByKey[pin.SeatKey!];
            taken[index] = true;
            occupant[index] = request.PinnedParticipants.First(p => p.ParticipantId == pin.ParticipantId).Attributes;
            assignments.Add(new ClassSeatAssignment(null, pin.ParticipantId, pin.SeatKey!));
        }

        var units = new List<UnitInfo>();
        foreach (var pin in request.Pins.Where(p => p.SeatKey is null))
        {
            var participant = request.PinnedParticipants.First(p => p.ParticipantId == pin.ParticipantId);
            units.Add(new UnitInfo(null, participant, participant.NeedsAccessibleSeat, participant.Attributes));
        }

        var pending = units
            .Select(u => (Unit: u, Count: 1))
            .Concat(request.Counts.Select(c => (Unit: new UnitInfo(c.ClassIndex, null, request.Classes[c.ClassIndex].NeedsAccessibleSeat, request.Classes[c.ClassIndex].Values), c.Count)))
            .ToList();
        foreach (var index in order)
        {
            if (taken[index])
            {
                continue;
            }

            var chosen = Enumerable.Range(0, pending.Count)
                .Where(i => pending[i].Count > 0 && (!pending[i].Unit.Accessible || seats[index].IsAccessible))
                .Where(i => !Conflicts(index, pending[i].Unit.Values, hardRules, neighbours, occupant))
                .OrderByDescending(i => pending[i].Unit.Accessible == seats[index].IsAccessible)
                .ThenByDescending(i => pending[i].Count)
                .ThenBy(i => i)
                .DefaultIfEmpty(-1)
                .First();
            if (chosen < 0)
            {
                continue;
            }

            var unit = pending[chosen].Unit;
            taken[index] = true;
            occupant[index] = unit.Values;
            assignments.Add(new ClassSeatAssignment(unit.ClassIndex, unit.Pinned?.ParticipantId, seats[index].Key));
            pending[chosen] = (unit, pending[chosen].Count - 1);
        }

        if (pending.Any(p => p.Count > 0))
        {
            return new SeatingResult(SolveStatus.Infeasible, [], 0, new Dictionary<Guid, long>());
        }

        return new SeatingResult(SolveStatus.Optimal, assignments, 0, new Dictionary<Guid, long>());
    }

    private bool Conflicts(
        int index,
        IReadOnlyDictionary<string, string> values,
        IReadOnlyList<NoAdjacentByAttributeParameters> rules,
        IReadOnlyList<List<int>[]> neighbours,
        IReadOnlyDictionary<string, string>?[] occupant)
    {
        for (var r = 0; r < rules.Count; r++)
        {
            if (!values.TryGetValue(rules[r].AttributeKey, out var value))
            {
                continue;
            }

            if (neighbours[r][index].Any(n => occupant[n] is { } other && other.TryGetValue(rules[r].AttributeKey, out var v) && v == value))
            {
                return true;
            }
        }

        return false;
    }

    private string Signature(IReadOnlyList<ClassSeatAssignment> assignments)
        => string.Join(";", assignments.OrderBy(a => a.SeatKey, StringComparer.Ordinal).Select(a => $"{a.SeatKey}:{a.ClassIndex}:{a.PinnedParticipantId}"));
}

public sealed record UnitInfo(int? ClassIndex, SolverParticipant? Pinned, bool Accessible, IReadOnlyDictionary<string, string> Values);

/// <summary>Seating double that fails the first <paramref name="failures"/> calls, then delegates.</summary>
public sealed class FailingSeatingSolver(ISeatingSolver inner, int failures) : ISeatingSolver
{
    private int calls;

    public SeatingResult Solve(SeatingRequest request, CancellationToken cancellationToken)
        => Interlocked.Increment(ref calls) <= failures
            ? new SeatingResult(SolveStatus.Infeasible, [], 0, new Dictionary<Guid, long>())
            : inner.Solve(request, cancellationToken);
}

/// <summary>Deterministic expander double: class members in order (shuffled per alternative), pins as given.</summary>
public sealed class FakeExpander : IExpander
{
    public IReadOnlyList<SeatAssignment> Expand(
        IReadOnlyList<ParticipantClass> classes,
        IReadOnlyDictionary<Guid, IReadOnlyList<ClassSeatAssignment>> seatingByRoom,
        IReadOnlyList<SolverParticipant> participants,
        int alternativeIndex,
        int randomSeed)
    {
        var queues = classes.ToDictionary(
            c => c.Index,
            c =>
            {
                var ids = c.ParticipantIds.ToArray();
                if (alternativeIndex > 0)
                {
                    new Random(randomSeed + alternativeIndex).Shuffle(ids);
                }

                return new Queue<Guid>(ids);
            });
        var result = new List<SeatAssignment>();
        foreach (var (roomId, seating) in seatingByRoom.OrderBy(e => e.Key))
        {
            foreach (var assignment in seating.OrderBy(a => a.SeatKey, StringComparer.Ordinal))
            {
                var participantId = assignment.PinnedParticipantId ?? queues[assignment.ClassIndex!.Value].Dequeue();
                result.Add(new SeatAssignment(participantId, roomId, assignment.SeatKey));
            }
        }

        return result;
    }
}