using Application.Rooms.Exceptions;
using Application.Rooms.Models;
using Domain.Rooms;
using LanguageExt;
using MediatR;

namespace Application.Rooms.Commands;

public record UpdateRoomCommand : IRequest<Either<RoomException, Room>>
{
    public Guid CenterId { get; init; }

    public Guid Id { get; init; }

    public string? Name { get; init; }

    public RoomLayoutInput? Layout { get; init; }

    public decimal? CostOverride { get; init; }

    public bool IsActive { get; init; }

    public uint Version { get; init; }
}

public class UpdateRoomCommandHandler
    : IRequestHandler<UpdateRoomCommand, Either<RoomException, Room>>
{
    public Task<Either<RoomException, Room>> Handle(
        UpdateRoomCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}