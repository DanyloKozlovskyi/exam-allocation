using System.Text;
using FluentAssertions;
using Infrastructure.ImportExport.Readers;
using Xunit;

namespace Infrastructure.Tests.ImportExport;

public class ParticipantFileReaderTests
{
    private const string CyrillicCsv = "Прізвище;Ім'я;Група\nІваненко;Ївга;11-Б\nҐудзь;Єлизавета;10-А\n";

    static ParticipantFileReaderTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private readonly ParticipantFileReader reader = new(ImportExportTestData.Settings());

    [Fact]
    public void ShouldReadAllXlsxSheetsKeepingRowPositions()
    {
        // Arrange
        var content = ImportExportTestData.Xlsx(
            ("Учасники", [["Прізвище", "Ім'я"], [null, null], ["Іваненко", "Іван"]]),
            ("Інше", [["A"]]));

        // Act
        var workbook = reader.Read(new MemoryStream(content), "people.XLSX");

        // Assert
        workbook.Sheets.Select(x => x.Name).Should().Equal("Учасники", "Інше");
        var rows = workbook.Sheets[0].Rows;
        rows.Should().HaveCount(3);
        rows[0].Should().Equal("Прізвище", "Ім'я");
        rows[1].Should().Equal(null, null);
        rows[2].Should().Equal("Іваненко", "Іван");
    }

    [Theory]
    [InlineData(';')]
    [InlineData(',')]
    [InlineData('\t')]
    public void ShouldDetectCsvDelimiter(char delimiter)
    {
        // Arrange
        var text = CyrillicCsv.Replace(';', delimiter);

        // Act
        var workbook = reader.Read(new MemoryStream(Encoding.UTF8.GetBytes(text)), "people.csv");

        // Assert
        var sheet = workbook.Sheets.Should().ContainSingle().Subject;
        sheet.Name.Should().Be("people");
        sheet.Rows.Should().HaveCount(3);
        sheet.Rows[1].Should().Equal("Іваненко", "Ївга", "11-Б");
    }

    [Fact]
    public void ShouldReadUtf8CsvWithBom()
    {
        // Arrange
        var content = ImportExportTestData.Text(CyrillicCsv, Encoding.UTF8, withPreamble: true);

        // Act
        var sheet = reader.Read(new MemoryStream(content), "people.csv").Sheets[0];

        // Assert
        sheet.Rows[0][0].Should().Be("Прізвище");
        sheet.Rows[2].Should().Equal("Ґудзь", "Єлизавета", "10-А");
    }

    [Fact]
    public void ShouldFallBackToWindows1251ForNonUtf8Csv()
    {
        // Arrange
        var content = ImportExportTestData.Text(CyrillicCsv, Encoding.GetEncoding(1251));

        // Act
        var sheet = reader.Read(new MemoryStream(content), "people.csv").Sheets[0];

        // Assert
        sheet.Rows[0].Should().Equal("Прізвище", "Ім'я", "Група");
        sheet.Rows[1].Should().Equal("Іваненко", "Ївга", "11-Б");
        sheet.Rows[2].Should().Equal("Ґудзь", "Єлизавета", "10-А");
    }

    [Fact]
    public void ShouldIgnoreDelimitersInsideQuotes()
    {
        // Arrange
        const string text = "\"Іваненко, Іван\";\"a,b,c\"\nПетренко;x\n";

        // Act
        var sheet = reader.Read(new MemoryStream(Encoding.UTF8.GetBytes(text)), "q.csv").Sheets[0];

        // Assert
        sheet.Rows[0].Should().Equal("Іваненко, Іван", "a,b,c");
    }

    [Fact]
    public void ShouldReturnNullForEmptyCsvCells()
    {
        // Act
        var sheet = reader.Read(new MemoryStream(Encoding.UTF8.GetBytes("a;;c\n")), "e.csv").Sheets[0];

        // Assert
        sheet.Rows[0].Should().Equal("a", null, "c");
    }

    [Fact]
    public void ShouldRejectFilesWithTooManyRows()
    {
        // Arrange
        var limited = new ParticipantFileReader(ImportExportTestData.Settings(maxRows: 2));
        var csv = Encoding.UTF8.GetBytes(CyrillicCsv);
        var xlsx = ImportExportTestData.Xlsx(("S", [["a"], ["b"], ["c"]]));

        // Act
        var readCsv = () => limited.Read(new MemoryStream(csv), "big.csv");
        var readXlsx = () => limited.Read(new MemoryStream(xlsx), "big.xlsx");

        // Assert
        readCsv.Should().Throw<InvalidDataException>();
        readXlsx.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData("people.txt")]
    [InlineData("people")]
    public void ShouldRejectUnsupportedExtensions(string fileName)
    {
        // Act
        var read = () => reader.Read(new MemoryStream([1, 2, 3]), fileName);

        // Assert
        read.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ShouldRejectCorruptXlsx()
    {
        // Act
        var read = () => reader.Read(new MemoryStream(Encoding.UTF8.GetBytes("not a zip")), "broken.xlsx");

        // Assert
        read.Should().Throw<InvalidDataException>();
    }
}