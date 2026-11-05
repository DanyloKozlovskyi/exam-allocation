using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.ValueObjects;

public class AttributeKeyTests
{
    [Theory]
    [InlineData("Group", AttributeKeys.Group)]
    [InlineData("  Профіль навчання ", "профіль_навчання")]
    [InlineData("Class-Room #2", "class_room_2")]
    public void ShouldNormalizeKeys(string raw, string expected)
    {
        // Act
        var key = AttributeKey.From(raw);

        // Assert
        key.Value.Should().Be(expected);
    }
}