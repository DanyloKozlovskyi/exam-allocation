using Application.Solving.Services;
using Domain.Centers;
using Domain.Participants;
using Domain.Rooms;
using Domain.Rules;
using Domain.Sessions;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using Tests.Data;

namespace Api.Tests.Integration.Sessions;

public class SessionFingerprintTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const int Sha256HexLength = 64;
    private readonly IntegrationTestWebFactory factory = factory;
    private readonly ExamSession session = ExamSessionsData.MainSession(DateTimeOffset.UtcNow);
    private TestCenter center = null!;
    private Room room = null!;
    private Participant participant = null!;
    private Rule disabledRule = null!;

    [Fact]
    public async Task ShouldBeStableForUnchangedData()
    {
        // Act
        var first = await Compute();
        var second = await Compute();

        // Assert
        first.Should().HaveLength(Sha256HexLength).And.Be(second);
    }

    [Fact]
    public async Task ShouldIgnoreInformationalChanges()
    {
        // Arrange
        var before = await Compute();

        // Act
        await Update<TestCenter>(center.Id, x => x.UpdateDetails("Інша назва", "Інша адреса", x.Location, "place", x.CostPerRoom, x.IsActive));
        await Update<Room>(room.Id, x => x.UpdateDetails("Ауд. 999", x.CostOverride, x.IsActive));
        await Update<Participant>(participant.Id, x => x.UpdateDetails("ext-999", x.Name, x.Group, x.School, x.NeedsAccessibleSeat, Attributes(x), "нотатка"));
        await Update<Rule>(disabledRule.Id, x => x.UpdateDetails(x.Parameters, x.IsHard, RuleConstraints.MaxWeight, false, "вимкнене"));

        // Assert
        (await Compute()).Should().Be(before);
    }

    [Fact]
    public async Task ShouldChangeWhenParticipantAttributeChanges()
    {
        // Arrange
        var before = await Compute();

        // Act
        await Update<Participant>(participant.Id, x => x.UpdateDetails(x.ExternalId, x.Name, ParticipantsData.GroupB, x.School, x.NeedsAccessibleSeat, Attributes(x), x.Notes));

        // Assert
        (await Compute()).Should().NotBe(before);
    }

    [Fact]
    public async Task ShouldChangeWhenRoomLayoutOrCostChanges()
    {
        // Arrange
        var before = await Compute();

        // Act
        await Update<Room>(room.Id, x => x.UpdateDetails(x.Name, 5m, x.IsActive));
        var afterCost = await Compute();
        var layout = RoomLayout.Create(RoomsData.Rows, RoomsData.Columns, RoomsData.SeatsPerDesk, [0], [], []);
        await Update<Room>(room.Id, x => x.ChangeLayout(layout, RoomsData.Seats(layout)));

        // Assert
        afterCost.Should().NotBe(before);
        (await Compute()).Should().NotBe(afterCost);
    }

    [Fact]
    public async Task ShouldChangeWhenRuleIsEnabledOrPinIsAdded()
    {
        // Arrange
        var before = await Compute();

        // Act
        await Update<Rule>(disabledRule.Id, x => x.UpdateDetails(x.Parameters, x.IsHard, x.Weight, true, x.DisplayName));
        var afterRule = await Compute();
        Context.Pins.Add(PinsData.RoomPin(session.Id, participant.Id, room.Id));
        await SaveChangesAsync();

        // Assert
        afterRule.Should().NotBe(before);
        (await Compute()).Should().NotBe(afterRule);
    }

    [Fact]
    public async Task ShouldChangeWhenCenterIsDeactivated()
    {
        // Arrange
        var before = await Compute();

        // Act
        await Update<TestCenter>(center.Id, x => x.UpdateDetails(x.Name, x.Address, x.Location, x.GooglePlaceId, x.CostPerRoom, false));

        // Assert
        (await Compute()).Should().NotBe(before);
    }

    public async Task InitializeAsync()
    {
        center = TestCentersData.MainCenter(session.Id);
        room = RoomsData.MainRoom(center.Id);
        participant = ParticipantsData.Ivanenko(session.Id);
        disabledRule = RulesData.FillFromFront(session.Id);
        disabledRule.UpdateDetails(disabledRule.Parameters, false, RuleConstraints.DefaultWeight, false, null);
        Context.ExamSessions.Add(session);
        Context.TestCenters.Add(center);
        Context.Rooms.Add(room);
        Context.Participants.AddRange(participant, ParticipantsData.Petrenko(session.Id));
        Context.Rules.AddRange(RulesData.SeparateGroups(session.Id), disabledRule);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.ChangeTracker.Clear();
        await Context.ExamSessions.ExecuteDeleteAsync();
    }

    private static IReadOnlyDictionary<AttributeKey, string> Attributes(Participant participant)
        => participant.Attributes.ToDictionary(x => AttributeKey.From(x.Key), x => x.Value);

    private async Task<string> Compute()
    {
        using var scope = factory.Services.CreateScope();
        var fingerprint = scope.ServiceProvider.GetRequiredService<ISessionFingerprint>();
        return await fingerprint.Compute(session.Id, CancellationToken.None);
    }

    private async Task Update<T>(object id, Action<T> change)
        where T : class
    {
        var entity = await Context.Set<T>().FindAsync(id);
        if (entity is Room tracked)
        {
            await Context.Entry(tracked).Collection(x => x.Seats).LoadAsync();
        }

        change(entity!);
        await SaveChangesAsync();
    }
}