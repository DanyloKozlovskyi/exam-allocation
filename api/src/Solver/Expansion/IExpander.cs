using Solver.Model;

namespace Solver.Expansion;

public interface IExpander
{
    IReadOnlyList<SeatAssignment> Expand(
        IReadOnlyList<ParticipantClass> classes,
        IReadOnlyDictionary<Guid, IReadOnlyList<ClassSeatAssignment>> seatingByRoom,
        IReadOnlyList<SolverParticipant> participants,
        int alternativeIndex,
        int randomSeed);
}