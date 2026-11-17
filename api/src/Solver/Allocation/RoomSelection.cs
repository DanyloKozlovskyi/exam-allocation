using Google.OrTools.Sat;
using Solver.Layout;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>Outcome of step S of 04 §4.1: the selected rooms and the lower bound on the stage-1 cost (scaled).</summary>
internal sealed record RoomSelectionResult(IReadOnlySet<int> Selected, long Cost, long LowerBound, bool Optimal);

/// <summary>
/// Step S of 04 §4.1: minimum-cost room selection over room types (integer number of open rooms per type),
/// subject to necessary covering conditions only, so its bound is a lower bound of the stage-1 cost.
/// </summary>
internal static class RoomSelection
{
    public static RoomSelectionResult? Solve(
        Stage1Context context,
        IReadOnlyList<Stage1Cut> cuts,
        long[] costs,
        int workers,
        double seconds,
        CancellationToken cancellationToken)
    {
        var types = Types(context, cuts, costs);
        var model = new CpModel();
        var counts = types.Select((t, i) => model.NewIntVar(t.Pinned, t.Rooms.Count, $"o_{i}")).ToArray();

        model.Add(LinearExpr.WeightedSum(counts, types.Select(t => (long)t.Sample.Capacity)) >= Participants(context));
        var accessible = AccessibleParticipants(context);
        if (accessible > 0)
        {
            model.Add(LinearExpr.WeightedSum(counts, types.Select(t => (long)t.Sample.AccessibleCapacity)) >= accessible);
        }

        foreach (var limit in RoomValueLimits.Build(context, cancellationToken))
        {
            var largest = LargestValue(context, limit.Key, accessibleOnly: false);
            if (largest > 0)
            {
                model.Add(LinearExpr.WeightedSum(counts, types.Select(t => (long)limit.PerRoom(t.Sample))) >= largest);
            }

            var largestAccessible = LargestValue(context, limit.Key, accessibleOnly: true);
            if (largestAccessible > 0)
            {
                model.Add(LinearExpr.WeightedSum(counts, types.Select(t => (long)limit.PerRoomAccessible(t.Sample))) >= largestAccessible);
            }
        }

        model.Minimize(LinearExpr.WeightedSum(counts, types.Select(t => t.Cost)));
        using var solver = new CpSolver { StringParameters = CpSatParameters.Build(workers, seconds, context.Input.Options.RandomSeed) };
        var status = CpSat.Solve(solver, model, null, cancellationToken);
        if (!CpSat.HasSolution(status))
        {
            return null;
        }

        var selected = new HashSet<int>();
        for (var i = 0; i < types.Count; i++)
        {
            foreach (var room in types[i].Rooms.Take((int)solver.Value(counts[i])))
            {
                selected.Add(room.Index);
            }
        }

        var cost = (long)Math.Round(solver.ObjectiveValue);
        var bound = status == CpSolverStatus.Optimal ? cost : (long)Math.Ceiling(solver.BestObjectiveBound);
        return new RoomSelectionResult(selected, cost, bound, status == CpSolverStatus.Optimal);
    }

    /// <summary>Room types: the symmetry key of 04 §4; rooms with pins or cuts are types of their own. Rooms inside a type are in index order.</summary>
    private static List<RoomType> Types(Stage1Context context, IReadOnlyList<Stage1Cut> cuts, long[] costs)
    {
        var centerMatters = Stage1ModelBuilder.CenterMatters(context.Input.Rules);
        var cutRooms = cuts.Select(c => c.RoomId).ToHashSet();
        return context.Rooms
            .Where(r => r.Capacity > 0)
            .GroupBy(r => context.Pins.Count(r.Room.RoomId) > 0 || cutRooms.Contains(r.Room.RoomId)
                ? (Layout: r.Index.ToString(System.Globalization.CultureInfo.InvariantCulture), Cost: costs[r.Index], Center: r.Room.RoomId)
                : (Layout: r.LayoutKey, Cost: costs[r.Index], Center: centerMatters ? r.Room.CenterId : Guid.Empty))
            .Select(g =>
            {
                var rooms = g.OrderBy(r => r.Index).ToList();
                return new RoomType(rooms[0], rooms, g.Key.Cost, rooms.Count(r => context.Pins.Count(r.Room.RoomId) > 0));
            })
            .OrderBy(t => t.Sample.Index)
            .ToList();
    }

    private static long Participants(Stage1Context context) => context.Classes.Sum(c => (long)c.Size) + context.Pins.Pinned.Count;

    private static long AccessibleParticipants(Stage1Context context)
        => context.Classes.Where(c => c.NeedsAccessibleSeat).Sum(c => (long)c.Size)
            + context.Pins.Pinned.Count(p => p.Participant.NeedsAccessibleSeat);

    /// <summary>max_v s_v(a) (or s^acc_v(a)), pins included.</summary>
    private static long LargestValue(Stage1Context context, string key, bool accessibleOnly)
    {
        var totals = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var (value, classes) in context.ClassesByValue(key))
        {
            totals[value] = classes.Where(c => !accessibleOnly || c.NeedsAccessibleSeat).Sum(c => (long)c.Size);
        }

        foreach (var (_, participant) in context.Pins.Pinned)
        {
            if ((!accessibleOnly || participant.NeedsAccessibleSeat) && AttributeValues.TryGet(participant, key, out var value))
            {
                totals[value] = totals.GetValueOrDefault(value) + 1;
            }
        }

        return totals.Count == 0 ? 0 : totals.Values.Max();
    }

    private sealed record RoomType(RoomFacts Sample, IReadOnlyList<RoomFacts> Rooms, long Cost, int Pinned);
}