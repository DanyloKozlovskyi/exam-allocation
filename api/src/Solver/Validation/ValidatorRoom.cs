using Solver.Model;

namespace Solver.Validation;

/// <summary>Enabled seat of a room as re-derived by the validator (independent of Layout/SeatGenerator).</summary>
internal sealed record ValidatorSeat(string Key, int Row, int Column, int SeatIndex, bool IsAccessible);

/// <summary>
/// Validator-private view of a room: enabled seats re-derived from <see cref="RoomLayoutModel"/> and a
/// brute-force adjacency predicate (docs/03 "Seat adjacency"). Deliberately does not use
/// <c>ISeatGenerator</c>/<c>IAdjacencyBuilder</c> so that the validator stays an independent oracle.
/// </summary>
internal sealed class ValidatorRoom
{
    private const int AdjacentDistance = 1;
    private const int FirstSeatIndex = 0;

    private readonly HashSet<int> aisles;
    private readonly int lastSeatIndex;

    public ValidatorRoom(SolverRoom room)
    {
        Room = room;
        var layout = room.Layout;
        aisles = [.. layout.AislesAfterColumns];
        lastSeatIndex = layout.SeatsPerDesk - 1;

        var seats = new Dictionary<string, ValidatorSeat>(StringComparer.Ordinal);
        for (var row = 0; row < layout.Rows; row++)
        {
            for (var column = 0; column < layout.Columns; column++)
            {
                for (var seat = 0; seat < layout.SeatsPerDesk; seat++)
                {
                    var key = SeatKeyFormat.Format(row, column, seat);
                    if (layout.DisabledSeats.Contains(key))
                    {
                        continue;
                    }

                    seats[key] = new ValidatorSeat(key, row, column, seat, layout.AccessibleSeats.Contains(key));
                }
            }
        }

        Seats = seats;
    }

    public SolverRoom Room { get; }

    public IReadOnlyDictionary<string, ValidatorSeat> Seats { get; }

    public int Capacity => Seats.Count;

    /// <summary>True when the two distinct seats are adjacent for the given kinds; SameDesk is always on.</summary>
    public bool AreAdjacent(ValidatorSeat first, ValidatorSeat second, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide)
    {
        if (first.Key == second.Key)
        {
            return false;
        }

        var rowDistance = Math.Abs(first.Row - second.Row);
        var columnDistance = Math.Abs(first.Column - second.Column);

        if (rowDistance == 0 && columnDistance == 0)
        {
            return true;
        }

        var aisleBetween = columnDistance == AdjacentDistance && aisles.Contains(Math.Min(first.Column, second.Column));

        if (rowDistance == 0 && columnDistance == AdjacentDistance)
        {
            if (aisleBetween)
            {
                return kinds.Contains(AdjacencyKind.SideAcrossAisle);
            }

            if (!kinds.Contains(AdjacencyKind.Side))
            {
                return false;
            }

            if (!seatLevelSide)
            {
                return true;
            }

            var (left, right) = first.Column < second.Column ? (first, second) : (second, first);
            return left.SeatIndex == lastSeatIndex && right.SeatIndex == FirstSeatIndex;
        }

        if (columnDistance == 0 && rowDistance == AdjacentDistance)
        {
            return kinds.Contains(AdjacencyKind.FrontBack);
        }

        if (rowDistance == AdjacentDistance && columnDistance == AdjacentDistance)
        {
            return !aisleBetween && kinds.Contains(AdjacencyKind.Diagonal);
        }

        return false;
    }
}