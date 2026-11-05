namespace Domain.Rules;

public enum RuleType
{
    SeparateByAttribute = 1,
    NoAdjacentByAttribute = 2,
    AccessibleSeating = 3,
    MinRoomFill = 4,
    KeepTogetherInCenter = 5,
    SpreadInRoom = 6,
    FillFromFront = 7,
}