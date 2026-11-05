using Application.Rooms.Exceptions;
using Domain.Rooms;
using LanguageExt;
using MediatR;

namespace Application.Rooms.Commands;

public record DeleteRoomCommand : IRequest<Either<RoomException, Room>>
{
    public Guid CenterId { get; init; }

    public Guid Id { get; init; }
}

public class DeleteRoomCommandHandler
    : IRequestHandler<DeleteRoomCommand, Either<RoomException, Room>>
{
    public Task<Either<RoomException, Room>> Handle(
        DeleteRoomCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}