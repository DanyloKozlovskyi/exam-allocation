using Application.Imports.Models;
using FluentAssertions;
using Infrastructure.ImportExport.Constants;
using Infrastructure.ImportExport.Mapping;
using Xunit;

namespace Infrastructure.Tests.ImportExport;

public class ColumnMappingSuggesterTests
{
    private readonly ColumnMappingSuggester suggester = new();

    [Fact]
    public void ShouldMapEveryTemplateHeader()
    {
        // Arrange
        var headers = ImportHeaders.TemplateColumns.Select(x => x.Header).ToList();

        // Act
        var mapping = suggester.Suggest(headers);

        // Assert
        mapping.Fields.Should().HaveCount(ImportHeaders.TemplateColumns.Count);
        foreach (var (header, field) in ImportHeaders.TemplateColumns)
        {
            mapping.Fields[field].Should().Be(header);
        }

        mapping.Attributes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("  ПРІЗВИЩЕ ", ParticipantField.LastName)]
    [InlineData("Surname", ParticipantField.LastName)]
    [InlineData("Фамилия", ParticipantField.LastName)]
    [InlineData("Ім’я", ParticipantField.FirstName)]
    [InlineData("ІМʼЯ", ParticipantField.FirstName)]
    [InlineData("First   Name", ParticipantField.FirstName)]
    [InlineData("Отчество", ParticipantField.MiddleName)]
    [InlineData("Клас", ParticipantField.Group)]
    [InlineData("Школа", ParticipantField.School)]
    [InlineData("Інклюзія", ParticipantField.NeedsAccessibleSeat)]
    [InlineData("№", ParticipantField.ExternalId)]
    [InlineData("Код", ParticipantField.ExternalId)]
    [InlineData("ПІБ", ParticipantField.FullName)]
    [InlineData("Примітка", ParticipantField.Notes)]
    public void ShouldMatchSynonyms(string header, ParticipantField expected)
    {
        // Act
        var mapping = suggester.Suggest([header]);

        // Assert
        mapping.Fields.Should().ContainKey(expected).WhoseValue.Should().Be(header);
    }

    [Fact]
    public void ShouldSuggestFullNameOnlyWithoutSeparateNameColumns()
    {
        // Act
        var withParts = suggester.Suggest(["ПІБ", "Прізвище", "Ім'я"]);
        var withoutParts = suggester.Suggest(["ПІБ", "Група"]);

        // Assert
        withParts.Fields.Should().NotContainKey(ParticipantField.FullName);
        withoutParts.Fields[ParticipantField.FullName].Should().Be("ПІБ");
    }

    [Fact]
    public void ShouldMapUnknownHeadersToAttributes()
    {
        // Act
        var mapping = suggester.Suggest(["Прізвище", "Ім'я", "Профіль", "Мова навчання", "Прізвище", "   ", "!!!"]);

        // Assert
        mapping.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["профіль"] = "Профіль",
            ["мова_навчання"] = "Мова навчання",
        });
    }

    [Fact]
    public void ShouldUseFirstHeaderForField()
    {
        // Act
        var mapping = suggester.Suggest(["Surname", "Прізвище"]);

        // Assert
        mapping.Fields[ParticipantField.LastName].Should().Be("Surname");
    }
}