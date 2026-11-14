using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Application.Allocations.Models;
using ClosedXML.Excel;
using FluentAssertions;
using Infrastructure.ImportExport.Constants;
using Infrastructure.ImportExport.Exporters;
using Xunit;

namespace Infrastructure.Tests.ImportExport;

public class AllocationExporterTests
{
    private const string SampleXml = "allocation.sample.xml";
    private const string Xsd = "allocation.xsd";
    private const string PdfMagic = "%PDF";

    private readonly AllocationExportModel model = ImportExportTestData.ExportModel();

    [Fact]
    public async Task ShouldExportXlsxWithExpectedSheets()
    {
        // Act
        var file = await new XlsxAllocationExporter().Export(model, null, CancellationToken.None);

        // Assert
        file.ContentType.Should().Be(ContentTypes.Xlsx);
        file.FileName.Should().Be("allocation-0.xlsx");
        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        var names = workbook.Worksheets.Select(x => x.Name).ToList();
        names.Should().HaveCount(5);
        names.Take(2).Should().Equal(ExportSheets.Summary, ExportSheets.AllParticipants);
        names.Should().OnlyContain(x => x.Length <= ExportSheets.MaxSheetNameLength);
        names.Should().OnlyHaveUniqueItems();
        names.Skip(2).Should().OnlyContain(x => x.IndexOfAny(ExportSheets.InvalidNameChars.ToArray()) < 0);
    }

    [Fact]
    public async Task ShouldWriteAllParticipantsSortedWithFilterAndFrozenHeader()
    {
        // Act
        var file = await new XlsxAllocationExporter().Export(model, null, CancellationToken.None);

        // Assert
        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        var sheet = workbook.Worksheet(ExportSheets.AllParticipants);
        Enumerable.Range(1, ExportHeaders.ParticipantColumns.Count)
            .Select(c => sheet.Cell(1, c).GetString())
            .Should().Equal(ExportHeaders.ParticipantColumns);
        var total = model.Centers.Sum(c => c.Rooms.Sum(r => r.Seats.Count(s => s.Participant is not null)));
        sheet.LastRowUsed()!.RowNumber().Should().Be(total + 1);
        var lastNames = Enumerable.Range(2, total).Select(r => sheet.Cell(r, 2).GetString()).ToList();
        lastNames.Should().BeInAscendingOrder(ExportRows.NameComparer);
        sheet.AutoFilter.IsEnabled.Should().BeTrue();
        sheet.SheetView.SplitRow.Should().Be(1);
    }

    [Fact]
    public async Task ShouldWriteSeatingGridWithAisleColumn()
    {
        // Act
        var file = await new XlsxAllocationExporter().Export(model, null, CancellationToken.None);

        // Assert
        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        var sheet = workbook.Worksheets.ElementAt(2);
        var board = sheet.CellsUsed().Single(x => x.GetString() == ExportHeaders.BoardLabel);
        var firstGridRow = board.Address.RowNumber + 1;
        sheet.Cell(firstGridRow, 2).GetString().Should().Be($"Прізвище0{ExportHeaders.DeskSeparator}Прізвище1");

        // Layout has an aisle after desk column 1 → sheet column 4 is a narrow empty aisle.
        sheet.Cell(firstGridRow, 4).IsEmpty().Should().BeTrue();
        sheet.Column(4).Width.Should().Be(ExportLayout.AisleColumnWidth);
        sheet.Cell(firstGridRow, 5).GetString().Should().Be($"Прізвище4{ExportHeaders.DeskSeparator}Прізвище5");
    }

    [Fact]
    public async Task ShouldExportXmlValidAgainstXsd()
    {
        // Act
        var file = await new XmlAllocationExporter().Export(model, null, CancellationToken.None);

        // Assert
        file.ContentType.Should().Be(ContentTypes.Xml);
        file.FileName.Should().Be("allocation-0.xml");
        ValidateAgainstXsd(file.Content).Should().BeEmpty();
        var document = XDocument.Load(new MemoryStream(file.Content));
        XNamespace ns = XmlNames.Namespace;
        document.Root!.Name.Should().Be(ns + XmlNames.Allocation);
        document.Descendants(ns + XmlNames.Participant).Should().HaveCount(11);
        document.Descendants(ns + XmlNames.Room).First().Attribute(XmlNames.Attributes.Filled)!.Value.Should().Be("7");
        var seat = document.Descendants(ns + XmlNames.Seat).First();
        seat.Attribute(XmlNames.Attributes.Key)!.Value.Should().Be("r0c0s0");
        seat.Attribute(XmlNames.Attributes.Row)!.Value.Should().Be("1");
    }

    [Fact]
    public void ShouldValidateSampleXmlAgainstXsd()
    {
        // Act
        var errors = ValidateAgainstXsd(File.ReadAllBytes(RepositoryPaths.Sample(SampleXml)));

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ShouldRejectInvalidXmlAgainstXsd()
    {
        // Arrange
        var xml = Encoding.UTF8.GetBytes($"<allocation xmlns=\"{XmlNames.Namespace}\" session=\"x\"/>");

        // Act
        var errors = ValidateAgainstXsd(xml);

        // Assert
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldExportCsvWithBomAndSemicolons()
    {
        // Act
        var file = await new CsvAllocationExporter().Export(model, null, CancellationToken.None);

        // Assert
        file.ContentType.Should().Be(ContentTypes.Csv);
        file.FileName.Should().Be("allocation-0.csv");
        file.Content.Take(3).Should().Equal(Encoding.UTF8.GetPreamble());
        var lines = Encoding.UTF8.GetString(file.Content, 3, file.Content.Length - 3)
            .Split(CsvSettings.ExportNewLine, StringSplitOptions.RemoveEmptyEntries);
        lines[0].Should().Be(string.Join(CsvSettings.DefaultDelimiter, ExportHeaders.ParticipantColumns));
        lines.Should().HaveCount(12);
        lines[1].Split(CsvSettings.DefaultDelimiter).Should().HaveCount(ExportHeaders.ParticipantColumns.Count);
        lines[1].Should().StartWith("1;Прізвище0;Ім'я0;;11-А;Школа №5;");
    }

    [Fact]
    public async Task ShouldExportPdf()
    {
        // Act
        var file = await new PdfAllocationExporter().Export(model, null, CancellationToken.None);

        // Assert
        file.ContentType.Should().Be(ContentTypes.Pdf);
        file.FileName.Should().Be("allocation-0.pdf");
        file.Content.Should().NotBeEmpty();
        Encoding.ASCII.GetString(file.Content, 0, PdfMagic.Length).Should().Be(PdfMagic);
    }

    [Fact]
    public async Task ShouldExportPdfForSingleRoom()
    {
        // Arrange
        var room = model.Centers[0].Rooms[1];

        // Act
        var all = await new PdfAllocationExporter().Export(model, null, CancellationToken.None);
        var single = await new PdfAllocationExporter().Export(model, room.Id, CancellationToken.None);

        // Assert
        single.Content.Should().NotBeEmpty();
        single.Content.Length.Should().BeLessThan(all.Content.Length);
        single.FileName.Should().Be($"allocation-0-room-{room.Id:N}.pdf");
    }

    [Fact]
    public async Task ShouldNameManualAllocationExport()
    {
        // Act
        var file = await new CsvAllocationExporter().Export(model with { AllocationIndex = -1 }, null, CancellationToken.None);

        // Assert
        file.FileName.Should().Be("allocation-manual.csv");
    }

    private static List<string> ValidateAgainstXsd(byte[] xml)
    {
        var errors = new List<string>();
        var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
        settings.Schemas.Add(XmlNames.Namespace, RepositoryPaths.Sample(Xsd));
        settings.ValidationEventHandler += (_, e) => errors.Add(e.Message);
        using var reader = XmlReader.Create(new MemoryStream(xml), settings);
        while (reader.Read())
        {
        }

        return errors;
    }
}