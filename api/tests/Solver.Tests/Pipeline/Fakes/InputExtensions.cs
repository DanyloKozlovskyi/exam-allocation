using Solver.Model;

namespace Solver.Tests.Pipeline.Fakes;

public static class InputExtensions
{
    /// <summary>Marks the given seats of room <paramref name="roomIndex"/> accessible.</summary>
    public static SolverInput WithAccessibleSeats(this SolverInput input, int roomIndex, params string[] seatKeys)
    {
        var rooms = input.Rooms.ToList();
        rooms[roomIndex] = rooms[roomIndex] with { Layout = rooms[roomIndex].Layout with { AccessibleSeats = seatKeys.ToHashSet() } };
        return input with { Rooms = rooms };
    }

    public static IEnumerable<SolverParticipant> InGroup(this SolverInput input, string group)
        => input.Participants.Where(p => p.Attributes.GetValueOrDefault(TestInputBuilder.GroupKey) == group);

    public static int Movable(this SolverInput input) => input.Participants.Count - input.Pins.Count;
}