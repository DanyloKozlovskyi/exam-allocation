using Application.Common.Interfaces.Repositories;
using Application.Rooms.Exceptions;
using Application.Rooms.Models;
using Application.Rooms.Services;
using Domain.Centers;
using Domain.Rooms;
using Domain.Sessions;
using LanguageExt;
using MediatR;
using Solver.Layout;
using Unit = LanguageExt.Unit;

namespace Application.Rooms.Commands;

public record CreateRoomsBulkCommand : IRequest<Either<RoomException, IReadOnlyList<Room>>>
{
    public Guid CenterId { get; init; }

    public IReadOnlyList<string> Names { get; init; } = [];

    public RoomLayoutInput? Layout { get; init; }

    public decimal? CostOverride { get; init; }

    public bool IsActive { get; init; }
}

public class CreateRoomsBulkCommandHandler(
    ITestCenterRepository centerRepository,
    IRoomRepository roomRepository,
    ISeatGenerator seatGenerator)
    : IRequestHandler<CreateRoomsBulkCommand, Either<RoomException, IReadOnlyList<Room>>>
{
    public Task<Either<RoomException, IReadOnlyList<Room>>> Handle(
        CreateRoomsBulkCommand request,
        CancellationToken cancellationToken)
    {
        var centerId = new TestCenterId(request.CenterId);
        var names = request.Names.Select(x => x.Trim()).ToList();

        return EnsureCenterExists(centerId, cancellationToken)
            .BindAsync(_ => EnsureNamesAreUnique(centerId, names, cancellationToken))
            .BindAsync(_ => CreateEntities(centerId, names, request, cancellationToken));
    }

    private async Task<Either<RoomException, TestCenter>> EnsureCenterExists(TestCenterId centerId, CancellationToken cancellationToken)
    {
        var center = await centerRepository.GetById(ExamSessionId.Empty(), centerId, cancellationToken);
        return center.Match<Either<RoomException, TestCenter>>(
            c => c,
            () => new RoomCenterNotFoundException(RoomId.Empty(), centerId.Value));
    }

    private async Task<Either<RoomException, Unit>> EnsureNamesAreUnique(TestCenterId centerId, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        foreach (var name in names)
        {
            var existing = await roomRepository.FindByName(centerId, name, Option<RoomId>.None, cancellationToken);
            if (existing.IsSome)
            {
                return new RoomAlreadyExistsException(RoomId.Empty(), name);
            }
        }

        return Unit.Default;
    }

    private async Task<Either<RoomException, IReadOnlyList<Room>>> CreateEntities(
        TestCenterId centerId,
        IReadOnlyList<string> names,
        CreateRoomsBulkCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var layout = request.Layout!.ToDomain();
            var seats = seatGenerator.GenerateSeats(layout);
            var rooms = names
                .Select(name => Room.New(RoomId.New(), centerId, name, layout, seats, request.CostOverride, request.IsActive))
                .ToList();
            await roomRepository.AddRangeAsync(rooms, true, cancellationToken);
            return rooms;
        }
        catch (ArgumentException exception)
        {
            return new RoomArgumentException(RoomId.Empty(), exception.Message);
        }
        catch (Exception exception)
        {
            return new RoomUnknownException(RoomId.Empty(), exception);
        }
    }
}