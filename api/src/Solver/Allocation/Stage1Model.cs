using Google.OrTools.Sat;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>Item a conflict-core literal stands for: a hard rule or the capacity of a center.</summary>
internal sealed record ConflictItem(Guid? RuleId, Guid? CenterId);

/// <summary>Stage-1 CP model (04 §4) with handles to its variables and objective parts.</summary>
internal sealed class Stage1Model
{
    public required CpModel Model { get; init; }

    public required IReadOnlyList<BoolVar> Open { get; init; }

    public required IReadOnlyDictionary<(int ClassIndex, int RoomIndex), IntVar> Counts { get; init; }

    /// <summary>Σ cost_r · y_r with costs scaled by <see cref="SolverDefaults.CostScale"/>.</summary>
    public required LinearExpr Cost { get; init; }

    public required LinearExpr SoftPenalty { get; init; }

    public required IReadOnlyDictionary<Guid, LinearExpr> PenaltyByRule { get; init; }

    public required bool HasSoftPenalty { get; init; }

    public required IReadOnlyList<(BoolVar Literal, ConflictItem Item)> Enforcement { get; init; }

    public void AddHint(IReadOnlyList<RoomClassCount> counts, IReadOnlyDictionary<Guid, int> roomIndexById, IReadOnlySet<int> pinnedRooms)
    {
        var values = counts
            .Where(c => roomIndexById.ContainsKey(c.RoomId))
            .GroupBy(c => (c.ClassIndex, roomIndexById[c.RoomId]))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Count));
        foreach (var (cell, variable) in Counts)
        {
            Model.AddHint(variable, values.GetValueOrDefault(cell));
        }

        var openRooms = values.Where(v => v.Value > 0).Select(v => v.Key.Item2).ToHashSet();
        for (var r = 0; r < Open.Count; r++)
        {
            Model.AddHint(Open[r], openRooms.Contains(r) || pinnedRooms.Contains(r));
        }
    }

    public void AddHintFromSolution(CpSolver solver)
    {
        Model.ClearHints();
        foreach (var variable in Counts.Values)
        {
            Model.AddHint(variable, solver.Value(variable));
        }

        foreach (var open in Open)
        {
            Model.AddHint(open, solver.BooleanValue(open));
        }
    }
}

/// <summary>Two-step solve options of 04 §4.1 for <see cref="Stage1ModelBuilder"/>.</summary>
/// <param name="Rooms">Selected rooms; the others stay closed (step A). Null = all rooms.</param>
/// <param name="CostLowerBound">Redundant <c>Cost ≥ LB</c> from room selection (scaled).</param>
/// <param name="Hint">Hint used when the request has none.</param>
/// <param name="Fixed">The counts of <paramref name="Hint"/> become constants: the model only evaluates that assignment.</param>
internal sealed record Stage1BuildOptions(IReadOnlySet<int>? Rooms, long? CostLowerBound, IReadOnlyList<RoomClassCount>? Hint, bool Fixed = false)
{
    public static Stage1BuildOptions Default { get; } = new(null, null, null);
}