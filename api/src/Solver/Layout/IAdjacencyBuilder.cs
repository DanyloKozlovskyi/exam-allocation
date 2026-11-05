using Solver.Model;

namespace Solver.Layout;

public interface IAdjacencyBuilder
{
    IReadOnlyList<SeatEdge> Build(RoomLayoutModel layout, IReadOnlyList<SolverSeat> seats, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide);
}