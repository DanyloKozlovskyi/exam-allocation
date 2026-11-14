using Solver.Model;
using Solver.Validation;

namespace Solver.Tests.Validation;

/// <summary>Hand-built inputs with known ids so allocations can be written seat by seat.</summary>
internal sealed class ValidatorScenario
{
    public const string GroupKey = TestInputBuilder.GroupKey;

    private readonly List<SolverRoom> rooms = [];
    private readonly List<SolverParticipant> participants = [];
    private readonly List<SolverRule> rules = [];
    private readonly List<SolverPin> pins = [];
    private readonly List<SeatAssignment> items = [];

    public Guid CenterId { get; } = Guid.NewGuid();

    public static string Seat(int row, int column, int seat = 0) => SeatKeyFormat.Format(row, column, seat);

    public Guid Room(
        int rows,
        int columns,
        int seatsPerDesk,
        Guid? centerId = null,
        IReadOnlyList<int>? aisles = null,
        IReadOnlyCollection<string>? disabled = null,
        IReadOnlyCollection<string>? accessible = null)
    {
        var id = Guid.NewGuid();
        rooms.Add(new SolverRoom(
            id,
            centerId ?? CenterId,
            1m,
            new RoomLayoutModel(rows, columns, seatsPerDesk, aisles ?? [], new HashSet<string>(disabled ?? []), new HashSet<string>(accessible ?? []))));
        return id;
    }

    public Guid Participant(string? group, bool needsAccessibleSeat = false)
    {
        var id = Guid.NewGuid();
        var attributes = group is null ? new Dictionary<string, string>() : new Dictionary<string, string> { [GroupKey] = group };
        participants.Add(new SolverParticipant(id, attributes, needsAccessibleSeat, $"{group} {participants.Count:D3}"));
        return id;
    }

    public Guid Rule(RuleType type, RuleParameters parameters, bool isHard = true, int weight = 1)
    {
        var id = Guid.NewGuid();
        rules.Add(new SolverRule(id, type, isHard, weight, parameters));
        return id;
    }

    public ValidatorScenario Pin(Guid participantId, Guid roomId, string? seatKey = null)
    {
        pins.Add(new SolverPin(participantId, roomId, seatKey));
        return this;
    }

    public ValidatorScenario Place(Guid participantId, Guid roomId, string seatKey)
    {
        items.Add(new SeatAssignment(participantId, roomId, seatKey));
        return this;
    }

    /// <summary>Places a new participant of the given group on the seat and returns its id.</summary>
    public Guid Seated(string? group, Guid roomId, string seatKey, bool needsAccessibleSeat = false)
    {
        var id = Participant(group, needsAccessibleSeat);
        Place(id, roomId, seatKey);
        return id;
    }

    public ValidationReport Validate()
        => new AllocationValidator().Validate(new SolverInput(rooms, participants, rules, pins, new SolveOptions()), items);
}