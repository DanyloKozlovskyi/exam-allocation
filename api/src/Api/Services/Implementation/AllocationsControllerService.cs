using Api.Dtos;
using Api.Services.Abstract;
using Application.Allocations.Services;
using Application.Common.Interfaces.Queries;
using Application.Common.Models;
using Domain.Allocations;
using Domain.Participants;
using Domain.Rooms;
using LanguageExt;

namespace Api.Services.Implementation;

public class AllocationsControllerService(
    IAllocationQueries allocationQueries,
    ISolveRunQueries runQueries,
    ITestCenterQueries centerQueries,
    IRoomQueries roomQueries,
    IParticipantQueries participantQueries,
    IAllocationEvaluator evaluator) : IAllocationsControllerService
{
    public async Task<IReadOnlyList<AllocationSummaryDto>> GetByRun(Guid runId, CancellationToken cancellationToken)
    {
        var allocations = await allocationQueries.GetByRun(new Domain.SolveRuns.SolveRunId(runId), cancellationToken);
        return allocations.Select(AllocationSummaryDto.FromDomain).ToList();
    }

    public async Task<Option<AllocationDto>> Get(Guid allocationId, CancellationToken cancellationToken)
    {
        var allocationOption = await allocationQueries.GetById(new AllocationId(allocationId), cancellationToken);
        if (allocationOption.IsNone)
        {
            return Option<AllocationDto>.None;
        }

        var allocation = allocationOption.Match(x => x, () => throw new InvalidOperationException());
        var runOption = await runQueries.GetById(allocation.RunId, cancellationToken);
        if (runOption.IsNone)
        {
            return Option<AllocationDto>.None;
        }

        var run = runOption.Match(x => x, () => throw new InvalidOperationException());
        var centers = await centerQueries.GetAll(run.SessionId, cancellationToken);
        var rooms = await roomQueries.GetAllBySession(run.SessionId, cancellationToken);
        var filledByRoom = allocation.Items.GroupBy(x => x.RoomId).ToDictionary(x => x.Key, x => x.Count());

        var centerDtos = centers
            .Select(center => new AllocationCenterDto(
                center.Id.Value,
                center.Name,
                center.Location.Latitude,
                center.Location.Longitude,
                rooms
                    .Where(room => room.CenterId == center.Id && (room.IsActive || filledByRoom.ContainsKey(room.Id)))
                    .Select(room =>
                    {
                        var filled = filledByRoom.GetValueOrDefault(room.Id);
                        return new AllocationRoomDto(room.Id.Value, room.Name, filled != 0, filled, room.Capacity);
                    })
                    .ToList()))
            .ToList();

        return new AllocationDto(allocation.Id.Value, allocation.RunId.Value, allocation.Index, ObjectiveDto.FromDomain(allocation.Objective), centerDtos);
    }

    public async Task<Option<RoomSeatingDto>> RoomSeating(Guid allocationId, Guid roomId, CancellationToken cancellationToken)
    {
        var contextOption = await evaluator.Load(new AllocationId(allocationId), cancellationToken);
        if (contextOption.IsNone)
        {
            return Option<RoomSeatingDto>.None;
        }

        var context = contextOption.Match(x => x, () => throw new InvalidOperationException());
        var room = context.Rooms.FirstOrDefault(x => x.Id.Value == roomId);
        if (room is null)
        {
            return Option<RoomSeatingDto>.None;
        }

        var report = evaluator.Validate(context, context.ToAssignments());
        var participants = context.Participants.ToDictionary(x => x.Id);
        var occupants = context.Allocation.Items.Where(x => x.RoomId == room.Id).ToDictionary(x => x.SeatId, x => x.ParticipantId);

        var seats = room.Seats
            .OrderBy(x => x.Row)
            .ThenBy(x => x.Column)
            .ThenBy(x => x.SeatIndex)
            .Select(seat =>
            {
                var participant = occupants.TryGetValue(seat.Id, out var participantId) ? participants.GetValueOrDefault(participantId) : null;
                var violations = report.Violations
                    .Where(v => v.RoomId == roomId && (v.SeatKey == seat.Key.Value || (participant is not null && v.ParticipantId == participant.Id.Value)))
                    .Select(v => new ViolationDto(Application.Solving.Mapping.SolverModelMapper.ToDomainViolationKind(v.Kind), v.IsHard, v.RuleId, v.ParticipantId, v.RoomId, v.SeatKey))
                    .ToList();
                return new SeatOccupancyDto(seat.Key.Value, participant is null ? null : ToBrief(participant), violations);
            })
            .ToList();

        return new RoomSeatingDto(RoomDto.FromDomain(room, includeSeats: true), seats);
    }

    public async Task<Option<PagedDto<AllocationRowDto>>> Participants(Guid allocationId, PageRequest page, CancellationToken cancellationToken)
    {
        var allocationOption = await allocationQueries.GetById(new AllocationId(allocationId), cancellationToken);
        var pageNumber = Math.Max(page.Page, PageRequest.FirstPage);
        var pageSize = Math.Clamp(page.PageSize, 1, PageRequest.MaxPageSize);
        if (allocationOption.IsNone)
        {
            return Option<PagedDto<AllocationRowDto>>.None;
        }

        var allocation = allocationOption.Match(x => x, () => throw new InvalidOperationException());
        var run = (await runQueries.GetById(allocation.RunId, cancellationToken)).Match(x => x, () => throw new InvalidOperationException());
        var rooms = (await roomQueries.GetAllBySession(run.SessionId, cancellationToken)).ToDictionary(x => x.Id);
        var seats = rooms.Values.SelectMany(room => room.Seats).ToDictionary(x => x.Id);
        var participants = (await participantQueries.GetAllBySession(run.SessionId, cancellationToken)).ToDictionary(x => x.Id);

        var rows = allocation.Items
            .Where(item => participants.ContainsKey(item.ParticipantId) && rooms.ContainsKey(item.RoomId) && seats.ContainsKey(item.SeatId))
            .Select(item => (Participant: participants[item.ParticipantId], Room: rooms[item.RoomId], Seat: seats[item.SeatId]))
            .Where(row => string.IsNullOrWhiteSpace(page.Search) || Matches(row.Participant, page.Search))
            .OrderBy(row => row.Participant.Name.SortKey, StringComparer.CurrentCulture)
            .ThenBy(row => row.Participant.Id.Value)
            .ToList();

        var ordered = page.SortDirection == SortDirection.Descending ? Enumerable.Reverse(rows).ToList() : rows;
        var items = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new AllocationRowDto(
                ToBrief(row.Participant),
                row.Room.CenterId.Value,
                row.Room.Center?.Name ?? string.Empty,
                row.Room.Id.Value,
                row.Room.Name,
                row.Seat.Key.Value))
            .ToList();
        return new PagedDto<AllocationRowDto>(items, pageNumber, pageSize, rows.Count);
    }

    private static bool Matches(Participant participant, string search)
    {
        var term = search.Trim();
        return participant.Name.SortKey.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || (participant.Group?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || (participant.ExternalId?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false);
    }

    private static ParticipantBriefDto ToBrief(Participant participant) => new(
        participant.Id.Value,
        participant.Name.LastName,
        participant.Name.FirstName,
        participant.Name.MiddleName,
        participant.Group,
        participant.School);
}