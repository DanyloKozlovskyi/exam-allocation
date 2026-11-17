using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Domain.Sessions;

namespace Api.Services.Implementation;

public class PinsControllerService(IPinQueries pinQueries, IParticipantQueries participantQueries, IRoomQueries roomQueries) : IPinsControllerService
{
    public async Task<IReadOnlyList<PinDto>> GetAll(Guid sessionId, CancellationToken cancellationToken)
    {
        var id = new ExamSessionId(sessionId);
        var pins = await pinQueries.GetAll(id, cancellationToken);
        if (pins.Count == 0)
        {
            return [];
        }

        // Names for the pins page: only the pinned participants and rooms (one query each, not per pin).
        var participants = (await participantQueries.GetByIds(id, pins.Select(x => x.ParticipantId).Distinct().ToList(), cancellationToken)).ToDictionary(x => x.Id);
        var rooms = (await roomQueries.GetByIds(pins.Select(x => x.RoomId).Distinct().ToList(), cancellationToken)).ToDictionary(x => x.Id);
        return pins
            .Select(pin => pin.ToDto(pin.Seat?.Key.Value) with
            {
                ParticipantName = participants.TryGetValue(pin.ParticipantId, out var participant) ? participant.Name.SortKey : null,
                RoomName = rooms.TryGetValue(pin.RoomId, out var room) ? room.Name : null,
                CenterId = rooms.TryGetValue(pin.RoomId, out var pinRoom) ? pinRoom.CenterId.Value : null,
            })
            .ToList();
    }
}