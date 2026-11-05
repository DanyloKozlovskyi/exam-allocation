using Solver.Model;

namespace Solver.Layout;

public sealed class IndependentSetCalculator : IIndependentSetCalculator
{
    public int Calculate(RoomLayoutModel layout, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}