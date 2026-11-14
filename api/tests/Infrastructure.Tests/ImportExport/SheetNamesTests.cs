using FluentAssertions;
using Infrastructure.ImportExport.Constants;
using Infrastructure.ImportExport.Exporters;
using Xunit;

namespace Infrastructure.Tests.ImportExport;

public class SheetNamesTests
{
    [Fact]
    public void ShouldBuildShortUniqueValidNames()
    {
        // Arrange
        var names = new SheetNames([ExportSheets.Summary, ExportSheets.AllParticipants]);
        var longRoom = new string('А', 40);

        // Act
        var result = new[]
        {
            names.ForRoom("Київський політехнічний ліцей", "Ауд. 214"),
            names.ForRoom("Київський політехнічний ліцей", "ауд. 214"),
            names.ForRoom("Центр", longRoom),
            names.ForRoom("Центр", longRoom),
            names.ForRoom("[Центр]", "1/2:3?*"),
            names.ForRoom(string.Empty, string.Empty),
        };

        // Assert
        result[0].Should().Be("Київський по–Ауд. 214");
        result[1].Should().Be("Київський по–ауд. 214 (2)");
        result.Should().OnlyContain(x => x.Length <= ExportSheets.MaxSheetNameLength);
        result.Should().OnlyHaveUniqueItems();
        result.Select(x => x.ToUpperInvariant()).Should().OnlyHaveUniqueItems();
        result[4].Should().Be("Центр–123");
        result[5].Should().Be("–");
    }
}