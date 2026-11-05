using Solver.Model;

namespace Solver.Layout;

public sealed class AdjacencyBuilder : IAdjacencyBuilder
{
    public IReadOnlyList<SeatEdge> Build(RoomLayoutModel layout, IReadOnlyList<SolverSeat> seats, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide)
        => throw new NotImplementedException();
}