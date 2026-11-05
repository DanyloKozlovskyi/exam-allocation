using Application.Rooms.Commands;
using Application.Rooms.Models;
using Domain.Rooms;
using Domain.Rules;

namespace Api.Dtos;

public record RoomLayoutDto(
    int Rows,
    int Columns,
    int SeatsPerDesk,
    IReadOnlyList<int> AislesAfterColumns,
    IReadOnlyList<string> DisabledSeats,
    IReadOnlyList<string> AccessibleSeats)
{
    public static RoomLayoutDto FromDomain(RoomLayout layout)
        => new(layout.Rows, layout.Columns, layout.SeatsPerDesk, layout.AislesAfterColumns, layout.DisabledSeats, layout.AccessibleSeats);

    public RoomLayoutInput ToInput() => new(Rows, Columns, SeatsPerDesk, AislesAfterColumns, DisabledSeats, AccessibleSeats);
}

public record SeatDto(Guid? Id, string SeatKey, int Row, int Column, int SeatIndex, bool IsAccessible)
{
    public static SeatDto FromDomain(Seat seat) => new(seat.Id.Value, seat.Key.Value, seat.Row, seat.Column, seat.SeatIndex, seat.IsAccessible);
}

public record RoomDto(
    Guid? Id,
    Guid? CenterId,
    string Name,
    RoomLayoutDto Layout,
    decimal? CostOverride,
    bool IsActive,
    int Capacity = 0,
    int AccessibleCapacity = 0,
    IReadOnlyList<SeatDto>? Seats = null,
    uint? Version = null)
{
    public static RoomDto FromDomain(Room room, bool includeSeats) => new(
        room.Id.Value,
        room.CenterId.Value,
        room.Name,
        RoomLayoutDto.FromDomain(room.Layout),
        room.CostOverride,
        room.IsActive,
        room.Capacity,
        room.AccessibleCapacity,
        includeSeats ? room.Seats.OrderBy(x => x.Row).ThenBy(x => x.Column).ThenBy(x => x.SeatIndex).Select(SeatDto.FromDomain).ToList() : null,
        room.Version);

    public CreateRoomCommand ToCreateCommand(Guid centerId) => new()
    {
        CenterId = centerId,
        Name = Name,
        Layout = Layout.ToInput(),
        CostOverride = CostOverride,
        IsActive = IsActive,
    };

    public UpdateRoomCommand ToUpdateCommand(Guid centerId) => new()
    {
        CenterId = centerId,
        Id = Id ?? Guid.Empty,
        Name = Name,
        Layout = Layout.ToInput(),
        CostOverride = CostOverride,
        IsActive = IsActive,
        Version = Version ?? default,
    };
}

public record CreateRoomsBulkDto(RoomDto Template, IReadOnlyList<string> Names)
{
    public CreateRoomsBulkCommand ToCommand(Guid centerId) => new()
    {
        CenterId = centerId,
        Names = Names,
        Layout = Template.Layout.ToInput(),
        CostOverride = Template.CostOverride,
        IsActive = Template.IsActive,
    };
}

public record SeatEdgeDto(int A, int B);

public record AdjacencyEdgesDto(AdjacencyKind Kind, IReadOnlyList<SeatEdgeDto> Edges);

public record RoomLayoutPreviewDto(
    IReadOnlyList<SeatDto> Seats,
    int Capacity,
    int AccessibleCapacity,
    IReadOnlyList<AdjacencyEdgesDto> Adjacency);