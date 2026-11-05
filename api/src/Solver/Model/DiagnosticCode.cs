namespace Solver.Model;

public enum DiagnosticCode
{
    CapacityTotalExceeded = 1,
    AccessibleCapacityExceeded = 2,
    SeparationImpossible = 3,
    NoAdjacencyImpossible = 4,
    PinInvalidSeat = 5,
    PinDuplicateSeat = 6,
    PinRoomOverfull = 7,
    PinRuleConflict = 8,
    AttributeMissing = 9,
    NoActiveRooms = 10,
    ModelTooLarge = 11,
    SeatingInfeasible = 12,
    ConflictCore = 13,
    AlternativesLimited = 14,
    InternalValidationFailed = 15,
}