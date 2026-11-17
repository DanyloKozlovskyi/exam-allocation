using System.Net;
using Api.Constants;
using Api.Dtos;
using Api.Tests.Integration.SolveRuns;
using Application.Common.Errors;
using Domain.Allocations;
using Domain.SolveRuns;
using FluentAssertions;
using Tests.Common;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;

namespace Api.Tests.Integration.Allocations;

/// <summary>Allocation endpoints that change data: apply-moves (409 path), manual versions and pin-all with a room filter.</summary>
public class AllocationsControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string AllocationParam = "allocationId";
    private const string RunParam = "runId";
    private const int TwoRoomGroups = 4;
    private const int TwoRoomGroupSize = 4;
    private SolveScenario scenario = null!;
    private SolveRunDto run = null!;

    public async Task InitializeAsync()
    {
        scenario = await SolveScenario.Create(Client);
        run = await scenario.WaitForCompletion((await scenario.Start(alternatives: 1)).Id);
        run.Status.Should().Be(SolveRunStatus.Succeeded, run.ErrorMessage);
    }

    public async Task DisposeAsync()
    {
        await scenario.DeleteSession();
    }

    [Fact]
    public async Task ShouldRejectMoveThatBreaksAPinUnlessForced()
    {
        // Arrange — pin a seated participant to the seat they have, then try to move them away
        var allocationId = run.AllocationIds[0];
        var (room, seating) = await FirstUsedRoom(allocationId);
        var occupied = seating.Seats.First(x => x.Participant is not null);
        var free = seating.Seats.First(x => x.Participant is null);
        var pin = await Client.PostJson(
            Route(ApiRoutes.Pins.Create, (SessionParam, scenario.SessionId)),
            new PinDto(null, occupied.Participant!.Id, room, occupied.SeatKey));
        pin.StatusCode.Should().Be(HttpStatusCode.OK, await pin.Content.ReadAsStringAsync());
        var moves = new MovesDto([new MoveDto(occupied.Participant.Id, room, free.SeatKey)]);

        // Act
        var rejected = await Client.PostJson(Route(ApiRoutes.Allocations.ApplyMoves, (AllocationParam, allocationId)), moves);
        var forced = await Client.PostJson(Route(ApiRoutes.Allocations.ApplyMoves, (AllocationParam, allocationId)), moves with { Force = true });

        // Assert
        rejected.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await rejected.ReadErrorCode()).Should().Be(ErrorCode.MoveViolatesHardRule);
        forced.StatusCode.Should().Be(HttpStatusCode.OK, await forced.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ShouldKeepBestAllocationFirstAfterManualVersion()
    {
        // Arrange
        var best = run.AllocationIds[0];
        var (room, seating) = await FirstUsedRoom(best);
        var occupied = seating.Seats.First(x => x.Participant is not null);
        var free = seating.Seats.First(x => x.Participant is null);
        var moves = new MovesDto([new MoveDto(occupied.Participant!.Id, room, free.SeatKey)], Force: true);

        // Act
        var manual = await (await Client.PostJson(Route(ApiRoutes.Allocations.ApplyMoves, (AllocationParam, best)), moves)).ReadJson<AllocationDto>();
        var updated = await (await Client.GetAsync(Route(ApiRoutes.SolveRuns.ById, (RunParam, run.Id)))).ReadJson<SolveRunDto>();
        var summaries = await (await Client.GetAsync(Route(ApiRoutes.Allocations.ByRun, (RunParam, run.Id)))).ReadJson<List<AllocationSummaryDto>>();

        // Assert — solver allocations by index first (best = 0), the manual version (index -1) last
        updated.AllocationIds[0].Should().Be(best);
        updated.AllocationIds[^1].Should().Be(manual.Id);
        summaries.Select(x => x.Index).Should().Equal(0, 1, AllocationConstraints.ManualIndex);
    }

    [Fact]
    public async Task ShouldPinRoomWithoutDoublePinningASeat()
    {
        // Arrange — more participants than one room holds; participant X (outside room B) is pinned to the
        // seat that the allocation gives Y in room B
        var twoRooms = await SolveScenario.Create(Client, groups: TwoRoomGroups, groupSize: TwoRoomGroupSize);
        var solved = await twoRooms.WaitForCompletion((await twoRooms.Start(alternatives: 0)).Id);
        solved.Status.Should().Be(SolveRunStatus.Succeeded, solved.ErrorMessage);
        var allocationId = solved.AllocationIds[0];
        var allocation = await (await Client.GetAsync(Route(ApiRoutes.Allocations.ById, (AllocationParam, allocationId)))).ReadJson<AllocationDto>();
        var usedRooms = allocation.Centers.SelectMany(x => x.Rooms).Where(x => x.Used).Select(x => x.RoomId).ToList();
        usedRooms.Should().HaveCount(SolveScenario.Rooms);
        var roomB = usedRooms[1];
        var seatingB = await Seating(allocationId, roomB);
        var seatOfY = seatingB.Seats.First(x => x.Participant is not null);
        var seatingA = await Seating(allocationId, usedRooms[0]);
        var participantX = seatingA.Seats.First(x => x.Participant is not null).Participant!.Id;
        var pinX = await Client.PostJson(Route(ApiRoutes.Pins.Create, (SessionParam, twoRooms.SessionId)), new PinDto(null, participantX, roomB, seatOfY.SeatKey));
        pinX.StatusCode.Should().Be(HttpStatusCode.OK, await pinX.Content.ReadAsStringAsync());

        // Act
        var pinned = await Client.PostJson(Route(ApiRoutes.Pins.PinAll, (AllocationParam, allocationId)), new PinAllDto([roomB]));
        var pins = await (await Client.GetAsync(Route(ApiRoutes.Pins.List, (SessionParam, twoRooms.SessionId)))).ReadJson<List<PinDto>>();
        await twoRooms.DeleteSession();

        // Assert — Y holds the seat; X's conflicting pin is gone; no seat is pinned twice
        pinned.StatusCode.Should().Be(HttpStatusCode.OK, await pinned.Content.ReadAsStringAsync());
        pins.Should().Contain(x => x.ParticipantId == seatOfY.Participant!.Id && x.RoomId == roomB && x.SeatKey == seatOfY.SeatKey);
        pins.Should().NotContain(x => x.ParticipantId == participantX);
        pins.Where(x => x.SeatKey != null).GroupBy(x => (x.RoomId, x.SeatKey)).Should().OnlyContain(g => g.Count() == 1);
        pins.Should().HaveCount(seatingB.Seats.Count(x => x.Participant is not null));
    }

    [Fact]
    public async Task ShouldReturnNotFoundForRowsOfUnknownAllocation()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Allocations.Participants, (AllocationParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(Guid Room, RoomSeatingDto Seating)> FirstUsedRoom(Guid allocationId)
    {
        var allocation = await (await Client.GetAsync(Route(ApiRoutes.Allocations.ById, (AllocationParam, allocationId)))).ReadJson<AllocationDto>();
        var room = allocation.Centers.SelectMany(x => x.Rooms).First(x => x.Used).RoomId;
        return (room, await Seating(allocationId, room));
    }

    private async Task<RoomSeatingDto> Seating(Guid allocationId, Guid roomId)
        => await (await Client.GetAsync(Route(ApiRoutes.Allocations.RoomSeating, (AllocationParam, allocationId), (RoomParam, roomId)))).ReadJson<RoomSeatingDto>();
}