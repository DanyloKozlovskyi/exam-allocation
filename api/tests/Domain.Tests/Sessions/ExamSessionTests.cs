using Domain.Sessions;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Sessions;

public class ExamSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ShouldCreateAndUpdateSession()
    {
        // Arrange
        var session = ExamSession.New(ExamSessionId.New(), " НМТ 2026 ", null, null, Now);
        var later = Now.AddHours(1);

        // Act
        session.UpdateDetails("НМТ", DateOnly.FromDateTime(later.UtcDateTime), "Опис", later);

        // Assert
        session.Name.Should().Be("НМТ");
        session.Description.Should().Be("Опис");
        session.CreatedAt.Should().Be(Now);
        session.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void ShouldRejectInvalidName()
    {
        // Act
        var empty = () => ExamSession.New(ExamSessionId.New(), " ", null, null, Now);
        var tooLong = () => ExamSession.New(ExamSessionId.New(), new string('a', ExamSessionConstraints.NameMaxLength + 1), null, null, Now);

        // Assert
        empty.Should().Throw<ArgumentException>();
        tooLong.Should().Throw<ArgumentOutOfRangeException>();
    }
}