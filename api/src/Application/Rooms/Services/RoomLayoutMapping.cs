using Application.Rooms.Models;
using Domain.Rooms;
using Domain.ValueObjects;
using Solver.Layout;
using Solver.Model;
using DomainAdjacencyKind = Domain.Rules.AdjacencyKind;
using SolverAdjacencyKind = Solver.Model.AdjacencyKind;

namespace Application.Rooms.Services;

/// <summary>Maps room layouts between Domain, command input and the solver's layout model.</summary>
public static class RoomLayoutMapping
{
    public static RoomLayout ToDomain(this RoomLayoutInput input)
        => RoomLayout.Create(
            input.Rows,
            input.Columns,
            input.SeatsPerDesk,
            input.AislesAfterColumns ?? [],
            input.DisabledSeats ?? [],
            input.AccessibleSeats ?? []);

    public static RoomLayoutModel ToSolverModel(this RoomLayout layout)
        => new(
            layout.Rows,
            layout.Columns,
            layout.SeatsPerDesk,
            layout.AislesAfterColumns.ToList(),
            layout.DisabledSeats.ToHashSet(StringComparer.Ordinal),
            layout.AccessibleSeats.ToHashSet(StringComparer.Ordinal));

    /// <summary>Seats of <paramref name="layout"/> as produced by the solver's seat generator.</summary>
    public static IReadOnlyList<(SeatKey Key, bool IsAccessible)> GenerateSeats(this ISeatGenerator seatGenerator, RoomLayout layout)
        => seatGenerator.Generate(layout.ToSolverModel())
            .Select(x => (SeatKey.Parse(x.Key), x.IsAccessible))
            .ToList();

    public static SolverAdjacencyKind ToSolver(this DomainAdjacencyKind kind) => kind switch
    {
        DomainAdjacencyKind.SameDesk => SolverAdjacencyKind.SameDesk,
        DomainAdjacencyKind.Side => SolverAdjacencyKind.Side,
        DomainAdjacencyKind.SideAcrossAisle => SolverAdjacencyKind.SideAcrossAisle,
        DomainAdjacencyKind.FrontBack => SolverAdjacencyKind.FrontBack,
        DomainAdjacencyKind.Diagonal => SolverAdjacencyKind.Diagonal,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported adjacency kind"),
    };
}