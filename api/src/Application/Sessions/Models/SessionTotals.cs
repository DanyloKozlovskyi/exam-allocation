namespace Application.Sessions.Models;

public sealed record SessionTotals(
    int Centers,
    int Rooms,
    int TotalCapacity,
    int AccessibleCapacity,
    int Participants,
    int NeedsAccessibleSeat,
    int Groups,
    int Rules);