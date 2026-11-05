using Solver.Model;

namespace Solver.Expansion;

public sealed class Expander : IExpander
{
    public IReadOnlyList<SeatAssignment> Expand(
        IReadOnlyList<ParticipantClass> classes,
        IReadOnlyDictionary<Guid, IReadOnlyList<ClassSeatAssignment>> seatingByRoom,
        IReadOnlyList<SolverParticipant> participants,
        int alternativeIndex,
        int randomSeed)
        => throw new NotImplementedException();
}