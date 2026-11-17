using Application.Allocations.Models;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Domain.Allocations;
using LanguageExt;

namespace Application.Allocations.Services;

/// <summary>Builds the flat export read model: centers → used rooms → all seats (0-based coordinates) with their occupants.</summary>
public class AllocationExportModelBuilder(
    IAllocationQueries allocationQueries,
    ISolveRunQueries runQueries,
    IExamSessionQueries sessionQueries,
    ITestCenterQueries centerQueries,
    IRoomQueries roomQueries,
    IParticipantQueries participantQueries,
    IDateTimeProvider dateTimeProvider) : IAllocationExportModelBuilder
{
    public async Task<Option<AllocationExportModel>> Build(AllocationId allocationId, CancellationToken cancellationToken)
    {
        var allocationOption = await allocationQueries.GetById(allocationId, cancellationToken);
        if (allocationOption.IsNone)
        {
            return Option<AllocationExportModel>.None;
        }

        var allocation = allocationOption.Match(x => x, () => throw new InvalidOperationException());
        var runOption = await runQueries.GetById(allocation.RunId, cancellationToken);
        if (runOption.IsNone)
        {
            return Option<AllocationExportModel>.None;
        }

        var run = runOption.Match(x => x, () => throw new InvalidOperationException());
        var sessionOption = await sessionQueries.GetById(run.SessionId, cancellationToken);
        if (sessionOption.IsNone)
        {
            return Option<AllocationExportModel>.None;
        }

        var session = sessionOption.Match(x => x, () => throw new InvalidOperationException());
        var centers = await centerQueries.GetAll(run.SessionId, cancellationToken);
        var rooms = await roomQueries.GetAllBySession(run.SessionId, cancellationToken);
        var participants = (await participantQueries.GetAllBySession(run.SessionId, cancellationToken)).ToDictionary(x => x.Id);
        var occupantBySeat = allocation.Items.ToDictionary(x => x.SeatId, x => x.ParticipantId);
        var usedRooms = allocation.Items.Select(x => x.RoomId).ToHashSet();

        var exportCenters = centers
            .OrderBy(x => x.Name, StringComparer.CurrentCulture)
            .Select(center => new ExportCenter(
                center.Id.Value,
                center.Name,
                center.Address,
                center.Location.Latitude,
                center.Location.Longitude,
                rooms
                    .Where(room => room.CenterId == center.Id && usedRooms.Contains(room.Id))
                    .OrderBy(room => room.Name, StringComparer.CurrentCulture)
                    .Select(room => new ExportRoom(
                        room.Id.Value,
                        room.Name,
                        room.Capacity,
                        room.Layout,
                        room.Seats
                            .OrderBy(seat => seat.Row)
                            .ThenBy(seat => seat.Column)
                            .ThenBy(seat => seat.SeatIndex)
                            .Select(seat => new ExportSeat(
                                seat.Key.Value,
                                seat.Row,
                                seat.Column,
                                seat.SeatIndex,
                                occupantBySeat.TryGetValue(seat.Id, out var participantId) && participants.TryGetValue(participantId, out var participant)
                                    ? new ExportParticipant(
                                        participant.Id.Value,
                                        participant.ExternalId,
                                        participant.Name.LastName,
                                        participant.Name.FirstName,
                                        participant.Name.MiddleName,
                                        participant.Group,
                                        participant.School)
                                    : null))
                            .ToList()))
                    .ToList()))
            .Where(center => center.Rooms.Count != 0)
            .ToList();

        return new AllocationExportModel(session.Name, session.ExamDate, allocation.Index, allocation.Objective, dateTimeProvider.UtcNow, exportCenters);
    }
}