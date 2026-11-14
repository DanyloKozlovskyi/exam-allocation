using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Participants;

public class ParticipantTests
{
    private const string CustomKey = "language";

    [Fact]
    public void ShouldResolveBuiltInAndCustomAttributes()
    {
        // Arrange
        var attributes = new Dictionary<AttributeKey, string> { [AttributeKey.From(CustomKey)] = " англійська " };

        // Act
        var participant = Participant.New(ParticipantId.New(), ExamSessionId.New(), " 42 ", PersonName.From("Іванов", "Іван", null), " 11-А ", "Школа 1", true, attributes, " ");

        // Assert
        participant.ExternalId.Should().Be("42");
        participant.Notes.Should().BeNull();
        participant.GetAttribute(AttributeKey.Group).Should().Be("11-А");
        participant.GetAttribute(AttributeKey.School).Should().Be("Школа 1");
        participant.GetAttribute(AttributeKey.From(CustomKey)).Should().Be("англійська");
        participant.GetAttribute(AttributeKey.From("missing")).Should().BeNull();
        participant.Attributes.Should().ContainKey(CustomKey);
    }

    [Fact]
    public void ShouldRejectTooManyAttributes()
    {
        // Arrange
        var attributes = Enumerable.Range(0, ParticipantConstraints.MaxAttributes + 1)
            .ToDictionary(x => AttributeKey.From($"a{x}"), x => x.ToString());

        // Act
        var act = () => Participant.New(ParticipantId.New(), ExamSessionId.New(), null, PersonName.From("Іванов", "Іван", null), null, null, false, attributes, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldUpdateDetails()
    {
        // Arrange
        var participant = Participant.New(ParticipantId.New(), ExamSessionId.New(), null, PersonName.From("Іванов", "Іван", null), null, null, false, new Dictionary<AttributeKey, string>(), null);

        // Act
        participant.UpdateDetails("7", PersonName.From("Петренко", "Петро", "Петрович"), "10-Б", null, true, new Dictionary<AttributeKey, string>(), "нотатка");

        // Assert
        participant.Name.SortKey.Should().Be("Петренко Петро Петрович");
        participant.Group.Should().Be("10-Б");
        participant.NeedsAccessibleSeat.Should().BeTrue();
        participant.Notes.Should().Be("нотатка");
    }
}