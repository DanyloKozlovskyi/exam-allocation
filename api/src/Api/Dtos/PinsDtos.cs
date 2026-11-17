using Application.Pins.Commands;
using Domain.Pins;

namespace Api.Dtos;

/// <param name="ParticipantName">Read-only (filled by <c>GET pins</c>, ignored on create).</param>
/// <param name="RoomName">Read-only (filled by <c>GET pins</c>, ignored on create).</param>
/// <param name="CenterId">Read-only (filled by <c>GET pins</c>, ignored on create).</param>
public record PinDto(
    Guid? Id,
    Guid ParticipantId,
    Guid RoomId,
    string? SeatKey,
    string? ParticipantName = null,
    string? RoomName = null,
    Guid? CenterId = null)
{
    public UpsertPinCommand ToUpsertCommand(Guid sessionId) => new()
    {
        SessionId = sessionId,
        ParticipantId = ParticipantId,
        RoomId = RoomId,
        SeatKey = SeatKey,
    };
}

public record PinAllDto(IReadOnlyList<Guid>? RoomIds);

public static class PinDtoMapping
{
    public static PinDto ToDto(this Pin pin, string? seatKey) => new(pin.Id.Value, pin.ParticipantId.Value, pin.RoomId.Value, seatKey);
}