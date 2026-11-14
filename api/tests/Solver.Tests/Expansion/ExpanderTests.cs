using FluentAssertions;
using Solver.Expansion;
using Solver.Model;
using Xunit;
using static Solver.Tests.Layout.LayoutFactory;

namespace Solver.Tests.Expansion;

public class ExpanderTests
{
    private readonly Expander _expander = new();

    [Fact]
    public void ShouldZipSortedMembersWithSortedSeats()
    {
        // Arrange
        var (participants, ids) = Participants("Charlie", "Alice", "Bob");
        var classes = new[] { Class(0, ids) };
        var roomId = Guid.NewGuid();
        var seating = Seating(roomId, (0, Key(1, 0, 0)), (0, Key(0, 10, 0)), (0, Key(0, 2, 0)));

        // Act
        var result = _expander.Expand(classes, seating, participants, 0, SolverDefaults.RandomSeed);

        // Assert
        result.Should().Equal(
            new SeatAssignment(ids[1], roomId, Key(0, 2, 0)),
            new SeatAssignment(ids[2], roomId, Key(0, 10, 0)),
            new SeatAssignment(ids[0], roomId, Key(1, 0, 0)));
    }

    [Fact]
    public void ShouldKeepPinnedParticipantsOnTheirSeats()
    {
        // Arrange
        var (participants, ids) = Participants("A", "B");
        var pinned = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var seating = new Dictionary<Guid, IReadOnlyList<ClassSeatAssignment>>
        {
            [roomId] =
            [
                new ClassSeatAssignment(null, pinned, Key(0, 0, 0)),
                new ClassSeatAssignment(0, null, Key(0, 1, 0)),
                new ClassSeatAssignment(0, null, Key(0, 2, 0)),
            ],
        };

        // Act
        var result = _expander.Expand([Class(0, ids)], seating, participants, 3, SolverDefaults.RandomSeed);

        // Assert
        result.Should().Contain(new SeatAssignment(pinned, roomId, Key(0, 0, 0)));
        result.Select(a => a.ParticipantId).Should().BeEquivalentTo([pinned, ids[0], ids[1]]);
    }

    [Fact]
    public void ShouldSplitClassAcrossRoomsInRoomIdOrder()
    {
        // Arrange
        var (participants, ids) = Participants("A", "B", "C");
        var rooms = new[] { Guid.NewGuid(), Guid.NewGuid() }.Order().ToArray();
        var seating = new Dictionary<Guid, IReadOnlyList<ClassSeatAssignment>>
        {
            [rooms[1]] = [new ClassSeatAssignment(0, null, Key(0, 0, 0))],
            [rooms[0]] = [new ClassSeatAssignment(0, null, Key(0, 1, 0)), new ClassSeatAssignment(0, null, Key(0, 0, 0))],
        };

        // Act
        var result = _expander.Expand([Class(0, ids)], seating, participants, 0, SolverDefaults.RandomSeed);

        // Assert
        result.Should().Equal(
            new SeatAssignment(ids[0], rooms[0], Key(0, 0, 0)),
            new SeatAssignment(ids[1], rooms[0], Key(0, 1, 0)),
            new SeatAssignment(ids[2], rooms[1], Key(0, 0, 0)));
    }

    [Fact]
    public void ShouldShuffleDeterministicallyForAlternatives()
    {
        // Arrange
        var names = Enumerable.Range(0, 20).Select(i => $"P{i:D2}").ToArray();
        var (participants, ids) = Participants(names);
        var roomId = Guid.NewGuid();
        var seating = Seating(roomId, Enumerable.Range(0, names.Length).Select(i => (0, Key(i / 5, i % 5, 0))).ToArray());
        var classes = new[] { Class(0, ids) };

        // Act
        var best = _expander.Expand(classes, seating, participants, 0, SolverDefaults.RandomSeed);
        var alternative = _expander.Expand(classes, seating, participants, 1, SolverDefaults.RandomSeed);
        var again = _expander.Expand(classes, seating, participants, 1, SolverDefaults.RandomSeed);
        var other = _expander.Expand(classes, seating, participants, 2, SolverDefaults.RandomSeed);

        // Assert
        alternative.Should().Equal(again);
        alternative.Should().NotEqual(best);
        other.Should().NotEqual(alternative);
        alternative.Select(a => a.ParticipantId).Should().BeEquivalentTo(ids);
        alternative.Select(a => a.SeatKey).Should().Equal(best.Select(a => a.SeatKey));
        best.Select(a => a.ParticipantId).Should().Equal(ids);
    }

    [Fact]
    public void ShouldThrowWhenClassHasMoreSeatsThanMembers()
    {
        // Arrange
        var (participants, ids) = Participants("A");
        var seating = Seating(Guid.NewGuid(), (0, Key(0, 0, 0)), (0, Key(0, 1, 0)));

        // Act
        var act = () => _expander.Expand([Class(0, ids)], seating, participants, 0, SolverDefaults.RandomSeed);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    private static (IReadOnlyList<SolverParticipant> Participants, Guid[] Ids) Participants(params string[] sortKeys)
    {
        var participants = sortKeys
            .Select(k => new SolverParticipant(Guid.NewGuid(), new Dictionary<string, string>(), false, k))
            .ToList();
        return (participants, participants.Select(p => p.ParticipantId).ToArray());
    }

    private static ParticipantClass Class(int index, IReadOnlyList<Guid> ids)
        => new(index, new Dictionary<string, string>(), false, ids.Reverse().ToList());

    private static Dictionary<Guid, IReadOnlyList<ClassSeatAssignment>> Seating(Guid roomId, params (int ClassIndex, string Key)[] seats)
        => new() { [roomId] = seats.Select(s => new ClassSeatAssignment(s.ClassIndex, null, s.Key)).ToList() };
}