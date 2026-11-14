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

public record CreateRoomCommand : IRequest<Either<RoomException, Room>>
{
    public Guid CenterId { get; init; }

    public string? Name { get; init; }

    public RoomLayoutInput? Layout { get; init; }

    public decimal? CostOverride { get; init; }

    public bool IsActive { get; init; }
}

public class CreateRoomCommandHandler(
    ITestCenterRepository centerRepository,
    IRoomRepository roomRepository,
    ISeatGenerator seatGenerator)
    : IRequestHandler<CreateRoomCommand, Either<RoomException, Room>>
{
    public Task<Either<RoomException, Room>> Handle(
        CreateRoomCommand request,
        CancellationToken cancellationToken)
    {
        var id = RoomId.New();
        var centerId = new TestCenterId(request.CenterId);
        var name = request.Name!.Trim();

        return EnsureCenterExists(id, centerId, cancellationToken)
            .BindAsync(_ => EnsureNameIsUnique(id, centerId, name, cancellationToken))
            .BindAsync(_ => CreateEntity(id, centerId, name, request, cancellationToken));
    }

    private async Task<Either<RoomException, TestCenter>> EnsureCenterExists(RoomId id, TestCenterId centerId, CancellationToken cancellationToken)
    {
        var center = await centerRepository.GetById(ExamSessionId.Empty(), centerId, cancellationToken);
        return center.Match<Either<RoomException, TestCenter>>(
            c => c,
            () => new RoomCenterNotFoundException(id, centerId.Value));
    }

    private async Task<Either<RoomException, Unit>> EnsureNameIsUnique(RoomId id, TestCenterId centerId, string name, CancellationToken cancellationToken)
    {
        var existing = await roomRepository.FindByName(centerId, name, Option<RoomId>.None, cancellationToken);
        return existing.Match<Either<RoomException, Unit>>(
            _ => new RoomAlreadyExistsException(id, name),
            () => Unit.Default);
    }

    private async Task<Either<RoomException, Room>> CreateEntity(
        RoomId id,
        TestCenterId centerId,
        string name,
        CreateRoomCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var layout = request.Layout!.ToDomain();
            var seats = seatGenerator.GenerateSeats(layout);
            var entity = Room.New(id, centerId, name, layout, seats, request.CostOverride, request.IsActive);
            return await roomRepository.AddAsync(entity, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new RoomArgumentException(id, exception.Message);
        }
        catch (Exception exception)
        {
            return new RoomUnknownException(id, exception);
        }
    }
}