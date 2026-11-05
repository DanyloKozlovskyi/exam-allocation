using Application.Rooms.Exceptions;
using Application.Rooms.Models;
using Domain.Rooms;
using LanguageExt;
using MediatR;

namespace Application.Rooms.Commands;

public record CreateRoomCommand : IRequest<Either<RoomException, Room>>
{
    public Guid CenterId { get; init; }

    public string? Name { get; init; }

    public RoomLayoutInput? Layout { get; init; }

    public decimal? CostOverride { get; init; }

    public bool IsActive { get; init; }
}

public class CreateRoomCommandHandler
    : IRequestHandler<CreateRoomCommand, Either<RoomException, Room>>
{
    public Task<Either<RoomException, Room>> Handle(
        CreateRoomCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}