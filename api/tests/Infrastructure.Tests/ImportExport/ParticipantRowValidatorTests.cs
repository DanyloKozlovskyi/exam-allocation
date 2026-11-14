using Application.Imports.Models;
using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.ImportExport.Validation;
using Xunit;

namespace Infrastructure.Tests.ImportExport;

public class ParticipantRowValidatorTests
{
    private readonly ParticipantRowValidator validator = new();

    [Fact]
    public void ShouldParseValidRowsTrimmingAndCollapsingWhitespace()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(
            ImportExportTestData.TemplateHeader(),
            ["  A-1 ", "  Іваненко  ", "Іван", "Іванович", "11   -А", " Ліцей  №1 ", "так", "  "]);

        // Act
        var result = Validate(sheet);

        // Assert
        result.ErrorRows.Should().Be(0);
        result.Errors.Should().BeEmpty();
        var row = result.ValidRows.Should().ContainSingle().Subject;
        row.Should().BeEquivalentTo(new ParsedParticipantRow(2, "A-1", "Іваненко", "Іван", "Іванович", "11 -А", "Ліцей №1", true, new Dictionary<string, string>(), null));
    }

    [Theory]
    [InlineData("так", true)]
    [InlineData("Ні", false)]
    [InlineData("YES", true)]
    [InlineData("no", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("+", true)]
    [InlineData("-", false)]
    [InlineData("", false)]
    public void ShouldParseBooleans(string value, bool expected)
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(ImportExportTestData.TemplateHeader(), [null, "Іваненко", "Іван", null, null, null, value, null]);

        // Act
        var result = Validate(sheet);

        // Assert
        result.ValidRows.Should().ContainSingle().Which.NeedsAccessibleSeat.Should().Be(expected);
    }

    [Fact]
    public void ShouldReportBooleanUnparseable()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(ImportExportTestData.TemplateHeader(), [null, "Іваненко", "Іван", null, null, null, "можливо", null]);

        // Act
        var result = Validate(sheet);

        // Assert
        result.ErrorRows.Should().Be(1);
        result.ValidRows.Should().BeEmpty();
        result.Errors.Should().ContainSingle().Which.Should().Be(new ImportRowError(2, "Особливі потреби", ImportErrorCode.BooleanUnparseable));
    }

    [Fact]
    public void ShouldReportRequiredMissing()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(ImportExportTestData.TemplateHeader(), ["1", "  ", null, "Іванович", null, null, null, null]);

        // Act
        var result = Validate(sheet);

        // Assert
        result.ErrorRows.Should().Be(1);
        result.Errors.Should().BeEquivalentTo(new[]
        {
            new ImportRowError(2, "Прізвище", ImportErrorCode.RequiredMissing),
            new ImportRowError(2, "Ім'я", ImportErrorCode.RequiredMissing),
        });
    }

    [Fact]
    public void ShouldReportValueTooLong()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(
            [.. ImportExportTestData.TemplateHeader(), "Профіль"],
            [new string('1', ParticipantConstraints.ExternalIdMaxLength + 1), new string('І', PersonName.PartMaxLength + 1), "Іван", null, null, null, null, null, new string('x', ParticipantConstraints.AttributeValueMaxLength + 1)]);
        var mapping = ImportExportTestData.TemplateMapping(new Dictionary<string, string> { ["profile"] = "Профіль" });

        // Act
        var result = Validate(sheet, ImportExportTestData.Options(mapping));

        // Assert
        result.ErrorRows.Should().Be(1);
        result.Errors.Select(x => (x.Column, x.Code)).Should().BeEquivalentTo(new[]
        {
            ("ID", ImportErrorCode.ValueTooLong),
            ("Прізвище", ImportErrorCode.ValueTooLong),
            ("Профіль", ImportErrorCode.ValueTooLong),
        });
    }

    [Fact]
    public void ShouldReportDuplicateExternalIdInFile()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(
            ImportExportTestData.TemplateHeader(),
            ["a-1", "Іваненко", "Іван"],
            ["A-1 ", "Петренко", "Петро"],
            ["a-2", "Іваненко", "Іван"]);

        // Act
        var result = Validate(sheet, ImportExportTestData.Options(ImportExportTestData.TemplateMapping(), DedupeStrategy.None));

        // Assert
        result.ValidRows.Select(x => x.Row).Should().Equal(2, 4);
        result.ErrorRows.Should().Be(1);
        result.Errors.Should().ContainSingle().Which.Should().Be(new ImportRowError(3, "ID", ImportErrorCode.DuplicateInFile));
        result.DuplicateRows.Should().Equal(3);
    }

    [Fact]
    public void ShouldReportDuplicateFullNameInFileWhenDedupingByFullName()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(
            ImportExportTestData.TemplateHeader(),
            [null, "Іваненко", "Іван", "Іванович"],
            [null, "ІВАНЕНКО", " іван ", "іванович"],
            [null, "Іваненко", "Іван"]);

        // Act
        var result = Validate(sheet, ImportExportTestData.Options(ImportExportTestData.TemplateMapping(), DedupeStrategy.FullName));

        // Assert
        result.ValidRows.Select(x => x.Row).Should().Equal(2, 4);
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(ImportErrorCode.DuplicateInFile);
        result.DuplicateRows.Should().Equal(3);
    }

    [Fact]
    public void ShouldReportDuplicateExistingAsUpdateCandidate()
    {
        // Arrange
        var existing = new[] { Existing("A-1", "Старе", "Ім'я") };
        var sheet = ImportExportTestData.Sheet(ImportExportTestData.TemplateHeader(), ["a-1", "Іваненко", "Іван"], ["a-2", "Петренко", "Петро"]);

        // Act
        var result = validator.Validate(sheet, ImportExportTestData.Options(ImportExportTestData.TemplateMapping()), existing);

        // Assert
        result.ErrorRows.Should().Be(0);
        result.ValidRows.Should().HaveCount(2);
        result.Errors.Should().ContainSingle().Which.Should().Be(new ImportRowError(2, null, ImportErrorCode.DuplicateExisting));
        result.DuplicateRows.Should().Equal(2);
    }

    [Fact]
    public void ShouldBlockExternalIdCollidingWithAnotherExistingParticipant()
    {
        // Arrange
        var existing = new[] { Existing("A-1", "Сидоренко", "Олег") };
        var sheet = ImportExportTestData.Sheet(ImportExportTestData.TemplateHeader(), ["A-1", "Іваненко", "Іван"]);

        // Act
        var result = validator.Validate(sheet, ImportExportTestData.Options(ImportExportTestData.TemplateMapping(), DedupeStrategy.FullName), existing);

        // Assert
        result.ErrorRows.Should().Be(1);
        result.ValidRows.Should().BeEmpty();
        result.Errors.Should().ContainSingle().Which.Should().Be(new ImportRowError(2, "ID", ImportErrorCode.DuplicateExisting));
    }

    [Fact]
    public void ShouldSkipEmptyRowsSilently()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(ImportExportTestData.TemplateHeader(), [null, " ", null], [null, "Іваненко", "Іван"], []);

        // Act
        var result = Validate(sheet);

        // Assert
        result.ErrorRows.Should().Be(0);
        result.Errors.Should().NotContain(x => x.Code == ImportErrorCode.EmptyRow);
        result.ValidRows.Should().ContainSingle().Which.Row.Should().Be(3);
    }

    [Fact]
    public void ShouldReportInvalidAttributeKeyAndRejectAllRows()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet([.. ImportExportTestData.TemplateHeader(), "Профіль"], [null, "Іваненко", "Іван", null, null, null, null, null, "фізмат"]);
        var mapping = ImportExportTestData.TemplateMapping(new Dictionary<string, string> { ["!!!"] = "Профіль" });

        // Act
        var result = Validate(sheet, ImportExportTestData.Options(mapping));

        // Assert
        result.ValidRows.Should().BeEmpty();
        result.ErrorRows.Should().Be(1);
        result.Errors.Should().ContainSingle().Which.Should().Be(new ImportRowError(1, "Профіль", ImportErrorCode.AttributeKeyInvalid));
    }

    [Fact]
    public void ShouldReadAttributesWithNormalizedKeys()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet([.. ImportExportTestData.TemplateHeader(), "Профіль"], [null, "Іваненко", "Іван", null, null, null, null, null, " фіз  мат "]);
        var mapping = ImportExportTestData.TemplateMapping(new Dictionary<string, string> { ["Profile Name"] = "профіль" });

        // Act
        var result = Validate(sheet, ImportExportTestData.Options(mapping));

        // Assert
        result.ValidRows.Single().Attributes.Should().Equal(new Dictionary<string, string> { ["profile_name"] = "фіз мат" });
    }

    [Fact]
    public void ShouldSplitFullName()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(["ПІБ", "Клас"], ["  Іваненко   Іван Іванович ", "11"], ["Петренко", "10"], ["Ковальчук Анна Марія Олегівна", "9"]);
        var mapping = new ColumnMapping(
            new Dictionary<ParticipantField, string> { [ParticipantField.FullName] = "ПІБ", [ParticipantField.Group] = "Клас" },
            new Dictionary<string, string>());

        // Act
        var result = Validate(sheet, ImportExportTestData.Options(mapping));

        // Assert
        result.ValidRows.Select(x => (x.LastName, x.FirstName, x.MiddleName)).Should().Equal(
            ("Іваненко", "Іван", "Іванович"),
            ("Ковальчук", "Анна", "Марія Олегівна"));
        result.Errors.Should().ContainSingle().Which.Should().Be(new ImportRowError(3, "ПІБ", ImportErrorCode.RequiredMissing));
    }

    [Fact]
    public void ShouldUseSelectedHeaderRow()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(["Список учасників"], [], ImportExportTestData.TemplateHeader(), [null, "Іваненко", "Іван"]);

        // Act
        var result = Validate(sheet, ImportExportTestData.Options(ImportExportTestData.TemplateMapping(), headerRow: 3));

        // Assert
        result.ValidRows.Should().ContainSingle().Which.Row.Should().Be(4);
    }

    [Fact]
    public void ShouldMatchHeadersIgnoringCaseAndApostropheVariant()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(["прізвище", "ІМ’Я"], ["Іваненко", "Іван"]);
        var mapping = new ColumnMapping(
            new Dictionary<ParticipantField, string> { [ParticipantField.LastName] = "Прізвище", [ParticipantField.FirstName] = "Ім'я" },
            new Dictionary<string, string>());

        // Act
        var result = Validate(sheet, ImportExportTestData.Options(mapping));

        // Assert
        result.ValidRows.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void ShouldThrowForHeaderRowOutsideSheet(int headerRow)
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(ImportExportTestData.TemplateHeader());

        // Act
        var validate = () => Validate(sheet, ImportExportTestData.Options(ImportExportTestData.TemplateMapping(), headerRow: headerRow));

        // Assert
        validate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowForMappedHeaderMissingInFile()
    {
        // Arrange
        var sheet = ImportExportTestData.Sheet(["Прізвище"], ["Іваненко"]);

        // Act
        var validate = () => Validate(sheet);

        // Assert
        validate.Should().Throw<ArgumentException>();
    }

    private static Participant Existing(string externalId, string lastName, string firstName)
        => Participant.New(
            ParticipantId.New(),
            ExamSessionId.New(),
            externalId,
            PersonName.From(lastName, firstName, null),
            null,
            null,
            false,
            new Dictionary<AttributeKey, string>(),
            null);

    private ImportValidation Validate(WorkbookSheet sheet, ImportOptions? options = null)
        => validator.Validate(sheet, options ?? ImportExportTestData.Options(ImportExportTestData.TemplateMapping()), []);
}