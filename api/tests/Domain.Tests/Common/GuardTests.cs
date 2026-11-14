using Domain.Common;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Common;

public class GuardTests
{
    private const string ParameterName = "value";
    private const int Limit = 3;

    [Fact]
    public void ShouldThrowWhenNull()
    {
        // Act
        var act = () => Guard.NotNull<string>(null, ParameterName);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName(ParameterName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldThrowWhenEmptyOrWhiteSpace(string? value)
    {
        // Act
        var act = () => Guard.NotNullOrEmptyOrWhiteSpace(value, ParameterName);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName(ParameterName);
    }

    [Fact]
    public void ShouldThrowWhenTooLong()
    {
        // Act
        var act = () => Guard.MaxLength("abcd", Limit, ParameterName);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(ParameterName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public void ShouldAcceptValuesWithinMaxLength(string? value)
    {
        // Act
        var act = () => Guard.MaxLength(value, Limit, ParameterName);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void ShouldCheckRange(int value, bool valid)
    {
        // Act
        var act = () => Guard.InRange(value, 1, Limit, ParameterName);

        // Assert
        if (valid)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    [Fact]
    public void ShouldCheckLowerBound()
    {
        // Act
        var act = () => Guard.GreaterThanOrEqual(-1m, 0m, ParameterName);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ShouldThrowWhenConditionIsFalse()
    {
        // Act
        var act = () => Guard.IsTrue(false, ParameterName);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName(ParameterName);
    }
}