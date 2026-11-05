using Solver.Model;

namespace Solver.Tests;

/// <summary>Fluent builder for small solver inputs used across solver tests.</summary>
public sealed class TestInputBuilder
{
    public const string GroupKey = "group";

    private readonly List<SolverRoom> rooms = [];
    private readonly List<SolverParticipant> participants = [];
    private readonly List<SolverRule> rules = [];
    private readonly List<SolverPin> pins = [];
    private SolveOptions options = new() { NumWorkers = 1, Stage1TimeLimitSeconds = 10, Alternatives = 0 };

    public Guid DefaultCenterId { get; } = Guid.NewGuid();

    public TestInputBuilder WithRoom(int rows, int columns, int seatsPerDesk, decimal cost = 1m, Guid? centerId = null, IReadOnlyList<int>? aisles = null)
    {
        rooms.Add(new SolverRoom(
            Guid.NewGuid(),
            centerId ?? DefaultCenterId,
            cost,
            new RoomLayoutModel(rows, columns, seatsPerDesk, aisles ?? [], new HashSet<string>(), new HashSet<string>())));
        return this;
    }

    public TestInputBuilder WithGroup(string group, int size, bool needsAccessibleSeat = false)
    {
        for (var i = 0; i < size; i++)
        {
            participants.Add(new SolverParticipant(
                Guid.NewGuid(),
                new Dictionary<string, string> { [GroupKey] = group },
                needsAccessibleSeat,
                $"{group} {i:D3}"));
        }

        return this;
    }

    public TestInputBuilder WithRule(RuleType type, RuleParameters parameters, bool isHard = true, int weight = 10)
    {
        rules.Add(new SolverRule(Guid.NewGuid(), type, isHard, weight, parameters));
        return this;
    }

    public TestInputBuilder WithPin(Guid participantId, Guid roomId, string? seatKey = null)
    {
        pins.Add(new SolverPin(participantId, roomId, seatKey));
        return this;
    }

    public TestInputBuilder WithOptions(Func<SolveOptions, SolveOptions> configure)
    {
        options = configure(options);
        return this;
    }

    public SolverInput Build() => new(rooms.ToList(), participants.ToList(), rules.ToList(), pins.ToList(), options);
}