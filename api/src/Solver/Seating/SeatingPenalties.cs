namespace Solver.Seating;

/// <summary>Evaluates stage-2 soft penalties (R3 soft, R8, R9) of a concrete seating, per rule.</summary>
internal static class SeatingPenalties
{
    public static IReadOnlyDictionary<Guid, long> Evaluate(SeatingContext context, int[] occupant)
    {
        var result = new Dictionary<Guid, long>();

        foreach (var rule in context.AdjacencyRules.Where(r => !r.Rule.IsHard))
        {
            var key = rule.Parameters.AttributeKey;
            var pairs = context.Edges(rule.Parameters.Adjacency, rule.Parameters.SeatLevelSide)
                .Count(e => occupant[e.A] >= 0
                    && occupant[e.B] >= 0
                    && context.Units[occupant[e.A]].ValueOf(key) is { } value
                    && string.Equals(value, context.Units[occupant[e.B]].ValueOf(key), StringComparison.Ordinal));
            Add(result, rule.Rule.RuleId, (long)rule.Rule.Weight * pairs);
        }

        foreach (var rule in context.SpreadRules)
        {
            var pairs = context.Edges(rule.Parameters.Adjacency, false).Count(e => occupant[e.A] >= 0 && occupant[e.B] >= 0);
            Add(result, rule.Rule.RuleId, (long)rule.Rule.Weight * pairs);
        }

        if (context.FillRules.Count > 0)
        {
            long rowSum = Enumerable.Range(0, occupant.Length).Where(p => occupant[p] >= 0).Sum(p => (long)context.Seats[p].Row);
            foreach (var rule in context.FillRules)
            {
                Add(result, rule.RuleId, rule.Weight * rowSum);
            }
        }

        return result;
    }

    private static void Add(Dictionary<Guid, long> result, Guid ruleId, long penalty)
        => result[ruleId] = result.GetValueOrDefault(ruleId) + penalty;
}