namespace Domain.Allocations;

public enum ViolationKind
{
    Unseated = 1,
    SeatedMoreThanOnce = 2,
    SeatOccupiedMoreThanOnce = 3,
    SeatNotFound = 4,
    CapacityExceeded = 5,
    AccessibilityViolated = 6,
    PinViolated = 7,
    SeparationViolated = 8,
    AdjacencyViolated = 9,
    MinRoomFillShortfall = 10,
    KeepTogetherSplit = 11,
    SpreadViolated = 12,
    FillFromFront = 13,
    UnknownParticipant = 14,
}