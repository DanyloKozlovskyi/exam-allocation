using Application.Common.Interfaces.Repositories;
using Application.Rooms.Exceptions;
using Domain.Centers;
using Domain.Rooms;
using LanguageExt;
using MediatR;

namespace Application.Rooms.Commands;

public record DeleteRoomCommand : IRequest<Either<RoomException, Room>>
{
    public Guid CenterId { get; init; }

    public Guid Id { get; init; }
}

public class DeleteRoomCommandHandler(IRoomRepository roomRepository)
    : IRequestHandler<DeleteRoomCommand, Either<RoomException, Room>>
{
    public Task<Either<RoomException, Room>> Handle(
        DeleteRoomCommand request,
        CancellationToken cancellationToken)
    {
        var id = new RoomId(request.Id);
        var centerId = new TestCenterId(request.CenterId);

        return GetRoom(centerId, id, cancellationToken)
            .BindAsync(room => DeleteEntity(room, cancellationToken));
    }

    private async Task<Either<RoomException, Room>> GetRoom(TestCenterId centerId, RoomId id, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetById(centerId, id, cancellationToken);
        return room.Match<Either<RoomException, Room>>(
            r => r,
            () => new RoomNotFoundException(id));
    }

    private async Task<Either<RoomException, Room>> DeleteEntity(Room room, CancellationToken cancellationToken)
    {
        try
        {
            return await roomRepository.RemoveAsync(room, cancellationToken);
        }
        catch (Exception exception)
        {
            return new RoomUnknownException(room.Id, exception);
        }
    }
}