using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Rooms.Commands;
using Application.Rooms.Services;
using Domain.Centers;
using Domain.Rooms;
using Domain.Rules;
using FluentValidation;
using LanguageExt;
using Solver.Layout;

namespace Api.Services.Implementation;

public class RoomsControllerService(
    IRoomQueries roomQueries,
    ISeatGenerator seatGenerator,
    IAdjacencyBuilder adjacencyBuilder) : IRoomsControllerService
{
    public async Task<IReadOnlyList<RoomDto>> GetAll(Guid centerId, CancellationToken cancellationToken)
    {
        var rooms = await roomQueries.GetAll(new TestCenterId(centerId), cancellationToken);
        return rooms.Select(x => RoomDto.FromDomain(x, false)).ToList();
    }

    public async Task<Option<RoomDto>> Get(Guid centerId, Guid roomId, CancellationToken cancellationToken)
    {
        var room = await roomQueries.GetById(new TestCenterId(centerId), new RoomId(roomId), cancellationToken);
        return room.Map(x => RoomDto.FromDomain(x, true));
    }

    /// <summary>Seats, capacity and adjacency per kind for a layout (no persistence).</summary>
    /// <exception cref="ValidationException">The layout is invalid.</exception>
    public RoomLayoutPreviewDto PreviewLayout(RoomLayoutDto layout)
    {
        var input = layout.ToInput();
        new RoomLayoutInputValidator().ValidateAndThrow(input);

        var model = input.ToDomain().ToSolverModel();
        var seats = seatGenerator.Generate(model);
        var adjacency = Enum.GetValues<AdjacencyKind>()
            .Select(kind => new AdjacencyEdgesDto(
                kind,
                adjacencyBuilder.Build(model, seats, [kind.ToSolver()], false)
                    .Select(edge => new SeatEdgeDto(edge.A, edge.B))
                    .ToList()))
            .ToList();

        return new RoomLayoutPreviewDto(
            seats.Select(x => new SeatDto(null, x.Key, x.Row, x.Column, x.SeatIndex, x.IsAccessible)).ToList(),
            seats.Count,
            seats.Count(x => x.IsAccessible),
            adjacency);
    }
}