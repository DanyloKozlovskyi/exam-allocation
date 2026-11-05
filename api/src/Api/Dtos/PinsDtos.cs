using Application.Pins.Commands;
using Domain.Pins;

namespace Api.Dtos;

public record PinDto(Guid? Id, Guid ParticipantId, Guid RoomId, string? SeatKey)
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