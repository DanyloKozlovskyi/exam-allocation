using Solver.Model;

namespace Solver.Seating;

public interface ISeatingSolver
{
    SeatingResult Solve(SeatingRequest request, CancellationToken cancellationToken);
}