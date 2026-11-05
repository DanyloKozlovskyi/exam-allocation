using Application.Rooms.Exceptions;
using Application.Rooms.Models;
using Domain.Rooms;
using LanguageExt;
using MediatR;

namespace Application.Rooms.Commands;

public record CreateRoomsBulkCommand : IRequest<Either<RoomException, IReadOnlyList<Room>>>
{
    public Guid CenterId { get; init; }

    public IReadOnlyList<string> Names { get; init; } = [];

    public RoomLayoutInput? Layout { get; init; }

    public decimal? CostOverride { get; init; }

    public bool IsActive { get; init; }
}

public class CreateRoomsBulkCommandHandler
    : IRequestHandler<CreateRoomsBulkCommand, Either<RoomException, IReadOnlyList<Room>>>
{
    public Task<Either<RoomException, IReadOnlyList<Room>>> Handle(
        CreateRoomsBulkCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}