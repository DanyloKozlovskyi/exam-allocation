using Solver.Model;

namespace Solver.Layout;

public interface IIndependentSetCalculator
{
    int Calculate(RoomLayoutModel layout, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide, CancellationToken cancellationToken);
}