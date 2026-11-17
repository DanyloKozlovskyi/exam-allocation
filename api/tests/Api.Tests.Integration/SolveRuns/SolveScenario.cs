using System.Net;
using Api.Constants;
using Api.Dtos;
using Domain.Rules;
using Domain.SolveRuns;
using Domain.ValueObjects;
using FluentAssertions;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;

namespace Api.Tests.Integration.SolveRuns;

/// <summary>Creates a small, solvable session through the public API (real seat generator and solver).</summary>
public sealed class SolveScenario
{
    public const int Groups = 3;
    public const int GroupSize = 3;
    public const int Rows = 2;
    public const int Columns = 3;
    public const int SeatsPerDesk = 2;
    public const int Rooms = 2;
    private const int PollIntervalMs = 200;
    private static readonly TimeSpan RunTimeout = TimeSpan.FromSeconds(90);

    private SolveScenario(HttpClient client, Guid sessionId, Guid centerId, IReadOnlyList<Guid> roomIds)
    {
        Client = client;
        SessionId = sessionId;
        CenterId = centerId;
        RoomIds = roomIds;
    }

    public static int ParticipantCount => Groups * GroupSize;

    public HttpClient Client { get; }

    public Guid SessionId { get; }

    public Guid CenterId { get; }

    public IReadOnlyList<Guid> RoomIds { get; }

    public static async Task<SolveScenario> Create(HttpClient client, int groups = Groups, int groupSize = GroupSize, int rooms = Rooms)
    {
        var session = await (await client.PostJson(Route(ApiRoutes.Sessions.Create), new SessionDto(null, "Сесія " + Guid.NewGuid(), null, null))).ReadJson<SessionDto>();
        var sessionId = session.Id!.Value;
        var centerResponse = await client.PostJson(
            Route(ApiRoutes.Centers.Create, (SessionParam, sessionId)),
            new TestCenterDto(null, sessionId, "Ліцей №1", "Київ, вул. Хрещатик, 1", 50.45, 30.52, null, 1m, true));
        centerResponse.StatusCode.Should().Be(HttpStatusCode.OK, await centerResponse.Content.ReadAsStringAsync());
        var centerId = (await centerResponse.ReadJson<TestCenterDto>()).Id!.Value;

        var roomIds = new List<Guid>();
        for (var i = 0; i < rooms; i++)
        {
            var layout = new RoomLayoutDto(Rows, Columns, SeatsPerDesk, [], [], []);
            var room = await client.PostJson(Route(ApiRoutes.Rooms.Create, (CenterParam, centerId)), new RoomDto(null, centerId, $"Ауд. {101 + i}", layout, null, true));
            room.StatusCode.Should().Be(HttpStatusCode.OK, await room.Content.ReadAsStringAsync());
            roomIds.Add((await room.ReadJson<RoomDto>()).Id!.Value);
        }

        for (var g = 0; g < groups; g++)
        {
            for (var p = 0; p < groupSize; p++)
            {
                await AddParticipant(client, sessionId, $"Учасник{g}{p}", $"КН-{g + 1}");
            }
        }

        var parameters = new NoAdjacentByAttributeParametersDto(AttributeKeys.Group, [AdjacencyKind.SameDesk, AdjacencyKind.Side], false);
        var rule = new RuleDto(null, sessionId, RuleType.NoAdjacentByAttribute, true, RuleConstraints.DefaultWeight, true, null, parameters);
        var ruleResponse = await client.PostJson(Route(ApiRoutes.Rules.Create, (SessionParam, sessionId)), rule);
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.OK, await ruleResponse.Content.ReadAsStringAsync());

        return new SolveScenario(client, sessionId, centerId, roomIds);
    }

    public static async Task AddParticipant(HttpClient client, Guid sessionId, string lastName, string group)
    {
        var response = await client.PostJson(
            Route(ApiRoutes.Participants.Create, (SessionParam, sessionId)),
            new ParticipantDto(null, sessionId, null, lastName, "Іван", null, group, null, false, new Dictionary<string, string>(), null));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    public async Task<SolveRunDto> Start(int alternatives = 1)
    {
        var response = await Client.PostJson(
            Route(ApiRoutes.SolveRuns.Create, (SessionParam, SessionId)),
            new SolveOptionsDto(alternatives, null, null, null, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.ReadJson<SolveRunDto>();
    }

    public async Task<SolveRunDto> WaitForCompletion(Guid runId)
    {
        var deadline = DateTime.UtcNow + RunTimeout;
        while (DateTime.UtcNow < deadline)
        {
            var run = await (await Client.GetAsync(Route(ApiRoutes.SolveRuns.ById, ("runId", runId)))).ReadJson<SolveRunDto>();
            if (run.Status is not (SolveRunStatus.Queued or SolveRunStatus.Running))
            {
                return run;
            }

            await Task.Delay(PollIntervalMs);
        }

        throw new TimeoutException($"Solve run {runId} did not finish within {RunTimeout}");
    }

    public Task<HttpResponseMessage> DeleteSession() => Client.DeleteAsync(Route(ApiRoutes.Sessions.ById, (SessionParam, SessionId)));
}