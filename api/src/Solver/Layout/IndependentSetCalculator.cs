using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Google.OrTools.Sat;
using Solver.Model;

namespace Solver.Layout;

/// <summary>
/// α_r(kinds): size of a maximum independent set of the seat-adjacency graph (docs/04 §3.2).
/// Solved with a small CP-SAT model (time limit <see cref="SolverDefaults.IndependentSetTimeLimitSeconds"/>);
/// when optimality is not proven, the best found value is used (a lower bound). Results are cached by
/// (layout, kinds, seatLevelSide). Thread-safe.
/// </summary>
public sealed class IndependentSetCalculator(ISeatGenerator seatGenerator, IAdjacencyBuilder adjacencyBuilder) : IIndependentSetCalculator
{
    private const int Workers = 1;
    private const char Separator = '|';
    private const char ItemSeparator = ',';

    private readonly ConcurrentDictionary<string, int> _cache = new(StringComparer.Ordinal);

    public IndependentSetCalculator()
        : this(new SeatGenerator(), new AdjacencyBuilder())
    {
    }

    public int Calculate(RoomLayoutModel layout, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(kinds);
        cancellationToken.ThrowIfCancellationRequested();

        var key = CacheKey(layout, kinds, seatLevelSide);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var seats = seatGenerator.Generate(layout);
        var edges = adjacencyBuilder.Build(layout, seats, kinds, seatLevelSide);
        var alpha = Solve(seats.Count, edges, cancellationToken);
        return _cache.GetOrAdd(key, alpha);
    }

    /// <summary>Greedy maximal independent set (min-degree first); a valid lower bound and CP-SAT hint.</summary>
    internal static bool[] Greedy(int seatCount, IReadOnlyList<SeatEdge> edges)
    {
        var neighbours = new List<int>[seatCount];
        for (var i = 0; i < seatCount; i++)
        {
            neighbours[i] = [];
        }

        foreach (var edge in edges)
        {
            neighbours[edge.A].Add(edge.B);
            neighbours[edge.B].Add(edge.A);
        }

        var chosen = new bool[seatCount];
        var blocked = new bool[seatCount];
        foreach (var seat in Enumerable.Range(0, seatCount).OrderBy(i => neighbours[i].Count).ThenBy(i => i))
        {
            if (blocked[seat])
            {
                continue;
            }

            chosen[seat] = true;
            blocked[seat] = true;
            foreach (var neighbour in neighbours[seat])
            {
                blocked[neighbour] = true;
            }
        }

        return chosen;
    }

    private static int Solve(int seatCount, IReadOnlyList<SeatEdge> edges, CancellationToken cancellationToken)
    {
        if (edges.Count == 0)
        {
            return seatCount;
        }

        var greedy = Greedy(seatCount, edges);
        var lowerBound = greedy.Count(x => x);

        var model = new CpModel();
        var x = new BoolVar[seatCount];
        for (var i = 0; i < seatCount; i++)
        {
            x[i] = model.NewBoolVar(string.Empty);
            model.AddHint(x[i], greedy[i]);
        }

        foreach (var edge in edges)
        {
            model.AddBoolOr([x[edge.A].Not(), x[edge.B].Not()]);
        }

        model.Maximize(LinearExpr.Sum(x));

        using var solver = new CpSolver
        {
            StringParameters = CpSatParameters.Build(Workers, SolverDefaults.IndependentSetTimeLimitSeconds, SolverDefaults.RandomSeed),
        };

        CpSolverStatus status;
        using (cancellationToken.Register(solver.StopSearch))
        {
            status = solver.Solve(model);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return status is CpSolverStatus.Optimal or CpSolverStatus.Feasible
            ? Math.Max(lowerBound, (int)Math.Round(solver.ObjectiveValue))
            : lowerBound;
    }

    private static string CacheKey(RoomLayoutModel layout, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide)
    {
        var normalizedKinds = kinds.Append(AdjacencyKind.SameDesk).Distinct().Order().ToList();

        // seatLevelSide only refines Side; normalize it away otherwise so equivalent requests share a cache entry.
        var effectiveSeatLevelSide = seatLevelSide && normalizedKinds.Contains(AdjacencyKind.Side) && layout.SeatsPerDesk > 1;

        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"{layout.Rows}{Separator}{layout.Columns}{Separator}{layout.SeatsPerDesk}{Separator}");
        builder.AppendJoin(ItemSeparator, layout.AislesAfterColumns.Distinct().Order());
        builder.Append(Separator);
        builder.AppendJoin(ItemSeparator, layout.DisabledSeats.Order(StringComparer.Ordinal));
        builder.Append(Separator);
        builder.AppendJoin(ItemSeparator, normalizedKinds.Select(k => (int)k));
        builder.Append(Separator);
        builder.Append(effectiveSeatLevelSide);
        return builder.ToString();
    }
}