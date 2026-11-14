using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Rooms.Exceptions;
using Application.Rooms.Models;
using Application.Rooms.Services;
using Domain.Centers;
using Domain.Rooms;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;
using Solver.Layout;

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

public class UpdateRoomCommandHandler(
    IRoomRepository roomRepository,
    IPinQueries pinQueries,
    ISeatGenerator seatGenerator)
    : IRequestHandler<UpdateRoomCommand, Either<RoomException, Room>>
{
    public Task<Either<RoomException, Room>> Handle(
        UpdateRoomCommand request,
        CancellationToken cancellationToken)
    {
        var id = new RoomId(request.Id);
        var centerId = new TestCenterId(request.CenterId);
        var name = request.Name!.Trim();

        return GetRoom(centerId, id, request.Version, cancellationToken)
            .BindAsync(room => EnsureNameIsUnique(room, name, cancellationToken))
            .BindAsync(room => BuildLayout(room, request.Layout!))
            .BindAsync(change => EnsurePinnedSeatsSurvive(change, cancellationToken))
            .BindAsync(change => UpdateEntity(change, name, request, cancellationToken));
    }

    private async Task<Either<RoomException, Room>> GetRoom(TestCenterId centerId, RoomId id, uint version, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetById(centerId, id, cancellationToken);
        return room.Match<Either<RoomException, Room>>(
            r => r.Version == version ? r : new RoomConcurrencyException(id),
            () => new RoomNotFoundException(id));
    }

    private async Task<Either<RoomException, Room>> EnsureNameIsUnique(Room room, string name, CancellationToken cancellationToken)
    {
        var existing = await roomRepository.FindByName(room.CenterId, name, room.Id, cancellationToken);
        return existing.Match<Either<RoomException, Room>>(
            _ => new RoomAlreadyExistsException(room.Id, name),
            () => room);
    }

    private Task<Either<RoomException, LayoutChange>> BuildLayout(Room room, RoomLayoutInput input)
    {
        try
        {
            var layout = input.ToDomain();
            return Task.FromResult<Either<RoomException, LayoutChange>>(new LayoutChange(room, layout, seatGenerator.GenerateSeats(layout)));
        }
        catch (ArgumentException exception)
        {
            return Task.FromResult<Either<RoomException, LayoutChange>>(new RoomArgumentException(room.Id, exception.Message));
        }
        catch (Exception exception)
        {
            return Task.FromResult<Either<RoomException, LayoutChange>>(new RoomUnknownException(room.Id, exception));
        }
    }

    private async Task<Either<RoomException, LayoutChange>> EnsurePinnedSeatsSurvive(LayoutChange change, CancellationToken cancellationToken)
    {
        var removedSeatIds = change.Room.SeatsRemovedBy(change.Seats).Select(x => x.Id).ToHashSet();
        if (removedSeatIds.Count == 0)
        {
            return change;
        }

        var pins = await pinQueries.GetByRoom(change.Room.Id, cancellationToken);
        var affected = pins
            .Where(x => x.SeatId is not null && removedSeatIds.Contains(x.SeatId))
            .Select(x => x.Id.Value)
            .ToList();

        return affected.Count == 0
            ? change
            : new RoomSeatReferencedByPinException(change.Room.Id, affected);
    }

    private async Task<Either<RoomException, Room>> UpdateEntity(
        LayoutChange change,
        string name,
        UpdateRoomCommand request,
        CancellationToken cancellationToken)
    {
        var room = change.Room;
        try
        {
            room.UpdateDetails(name, request.CostOverride, request.IsActive);
            room.ChangeLayout(change.Layout, change.Seats);
            return await roomRepository.UpdateAsync(room, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new RoomArgumentException(room.Id, exception.Message);
        }
        catch (Exception exception)
        {
            return new RoomUnknownException(room.Id, exception);
        }
    }

    private sealed record LayoutChange(Room Room, RoomLayout Layout, IReadOnlyList<(SeatKey Key, bool IsAccessible)> Seats);
}