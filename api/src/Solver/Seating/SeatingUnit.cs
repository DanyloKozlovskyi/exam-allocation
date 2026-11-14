namespace Solver.Seating;

/// <summary>
/// A group of interchangeable occupants inside one room for stage 2: a participant class
/// (<see cref="ClassIndex"/>), or a single pinned participant (<see cref="PinnedParticipantId"/>),
/// either with a fixed seat (<see cref="FixedSeat"/>) or with the room only.
/// </summary>
internal sealed record SeatingUnit(
    int? ClassIndex,
    Guid? PinnedParticipantId,
    int Count,
    bool NeedsAccessibleSeat,
    IReadOnlyDictionary<string, string> Values,
    int? FixedSeat)
{
    public bool IsMovable => FixedSeat is null;

    /// <summary>Value of an attribute; <c>null</c> (missing) is unique and never conflicts.</summary>
    public string? ValueOf(string attributeKey) => Values.TryGetValue(attributeKey, out var value) ? value : null;
}