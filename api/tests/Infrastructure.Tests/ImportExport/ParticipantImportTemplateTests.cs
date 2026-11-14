using Application.Imports.Models;
using FluentAssertions;
using Infrastructure.ImportExport.Constants;
using Infrastructure.ImportExport.Mapping;
using Infrastructure.ImportExport.Readers;
using Infrastructure.ImportExport.Templates;
using Xunit;

namespace Infrastructure.Tests.ImportExport;

public class ParticipantImportTemplateTests
{
    private const string TemplateSample = "participants-template.xlsx";

    public static TheoryData<string> Sources() => new() { "generated", "sample" };

    [Theory]
    [MemberData(nameof(Sources))]
    public void ShouldContainTemplateHeadersMappedToAllFields(string source)
    {
        // Arrange
        var content = source == "sample" ? File.ReadAllBytes(RepositoryPaths.Sample(TemplateSample)) : ParticipantImportTemplate.Build();
        var reader = new ParticipantFileReader(ImportExportTestData.Settings());

        // Act
        var workbook = reader.Read(new MemoryStream(content), ExportFileNames.TemplateFileName);
        var headers = workbook.Sheets[0].Rows[ImportLimits.DefaultHeaderRow - 1].Select(x => x ?? string.Empty).ToList();
        var mapping = new ColumnMappingSuggester().Suggest(headers);

        // Assert
        workbook.Sheets[0].Name.Should().Be(ExportSheets.Template);
        headers.Should().Equal(ImportHeaders.TemplateColumns.Select(x => x.Header));
        mapping.Fields.Keys.Should().BeEquivalentTo(ImportHeaders.TemplateColumns.Select(x => x.Field));
    }
}