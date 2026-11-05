using Solver.Model;

namespace Solver.Layout;

public interface ISeatGenerator
{
    IReadOnlyList<SolverSeat> Generate(RoomLayoutModel layout);
}