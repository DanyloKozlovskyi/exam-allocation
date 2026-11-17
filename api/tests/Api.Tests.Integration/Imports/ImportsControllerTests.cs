using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Constants;
using Api.Dtos;
using Application.Common.Errors;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Settings;
using Application.Imports.Models;
using ClosedXML.Excel;
using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.ImportExport.Constants;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;

namespace Api.Tests.Integration.Imports;

/// <summary>
/// Import flow through HTTP. Participant/session persistence is owned by WS-C, so the repository and query
/// interfaces are replaced by in-memory fakes here.
/// </summary>
public class ImportsControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string FileField = "file";
    private const string TemplateFile = "participants.xlsx";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IntegrationTestWebFactory baseFactory = factory;
    private readonly FakeParticipantStore store = new();
    private readonly ExamSession session = ExamSession.New(ExamSessionId.New(), "Сесія імпорту", null, null, DateTimeOffset.UtcNow);
    private HttpClient client = null!;

    public Task InitializeAsync()
    {
        client = CreateClient(new ApplicationSettings());
        store.Seed(Participant.New(
            ParticipantId.New(), session.Id, "A-2", PersonName.From("Старе", "Ім'я", null), null, null, false, new Dictionary<AttributeKey, string>(), null));
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ShouldDownloadTemplate()
    {
        // Act
        var response = await client.GetAsync(ApiRoutes.Imports.Template);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ContentTypes.Xlsx);
        response.Content.Headers.ContentDisposition!.FileName.Should().Be(ExportFileNames.TemplateFileName);
        using var workbook = new XLWorkbook(await response.Content.ReadAsStreamAsync());
        workbook.Worksheet(ExportSheets.Template).Cell(1, 2).GetString().Should().Be(ImportHeaders.LastName);
    }

    [Fact]
    public async Task ShouldUploadValidateAndCommitTemplateFile()
    {
        // Arrange
        var file = await FilledTemplate(
            ["A-1", "Іваненко", "Іван", "Іванович", "11-А", "Ліцей №1", "так", null],
            ["A-2", "Петренко", "Петро", null, "11-Б", null, "ні", "оновлено"],
            ["A-3", "Сидоренко", "Олег", null, null, null, "можливо", null]);

        // Act
        var preview = await Upload(file);
        var mapping = Mapping(preview, skipInvalid: false);
        var validation = await (await client.PostAsJsonAsync(Route(ApiRoutes.Imports.Validate, preview.ImportId), mapping, JsonOptions)).ToResponseModel<ImportValidationDto>();
        var rejected = await client.PostAsJsonAsync(Route(ApiRoutes.Imports.Commit, preview.ImportId), mapping, JsonOptions);
        var committed = await client.PostAsJsonAsync(Route(ApiRoutes.Imports.Commit, preview.ImportId), mapping with { SkipInvalid = true }, JsonOptions);
        var again = await client.PostAsJsonAsync(Route(ApiRoutes.Imports.Commit, preview.ImportId), mapping with { SkipInvalid = true }, JsonOptions);

        // Assert
        preview.Sheets.Should().Equal(ExportSheets.Template);
        preview.Headers.Should().Equal(ImportHeaders.TemplateColumns.Select(x => x.Header));
        preview.SampleRows.Should().HaveCount(3);
        preview.SuggestedMapping.Fields.Keys.Should().BeEquivalentTo(ImportHeaders.TemplateColumns.Select(x => x.Field));

        validation.ValidRows.Should().Be(2);
        validation.ErrorRows.Should().Be(1);
        validation.Errors.Should().ContainEquivalentOf(new ImportErrorDto(4, ImportHeaders.NeedsAccessibleSeat, ImportErrorCode.BooleanUnparseable));
        validation.Errors.Should().ContainEquivalentOf(new ImportErrorDto(3, null, ImportErrorCode.DuplicateExisting));
        validation.Duplicates.Should().Equal(3);

        await ShouldBeProblem(rejected, HttpStatusCode.UnprocessableEntity, ErrorCode.ImportRowsInvalid);

        committed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await committed.ToResponseModel<ImportResultDto>()).Should().Be(new ImportResultDto(1, 1, 1));
        store.All.Select(x => (x.ExternalId, x.Name.LastName)).Should().BeEquivalentTo(new[] { ("A-1", "Іваненко"), ("A-2", "Петренко") });
        store.All.Single(x => x.ExternalId == "A-1").NeedsAccessibleSeat.Should().BeTrue();
        store.All.Single(x => x.ExternalId == "A-2").Notes.Should().Be("оновлено");

        await ShouldBeProblem(again, HttpStatusCode.NotFound, ErrorCode.ImportNotFound);
    }

    [Fact]
    public async Task ShouldReplaceParticipants()
    {
        // Arrange
        var preview = await Upload(await FilledTemplate([null, "Новий", "Учасник"]));
        var mapping = Mapping(preview, skipInvalid: false) with { Mode = ImportMode.Replace };

        // Act
        var response = await client.PostAsJsonAsync(Route(ApiRoutes.Imports.Commit, preview.ImportId), mapping, JsonOptions);

        // Assert
        (await response.ToResponseModel<ImportResultDto>()).Should().Be(new ImportResultDto(1, 0, 0));
        store.All.Should().ContainSingle().Which.Name.LastName.Should().Be("Новий");
    }

    [Fact]
    public async Task ShouldUploadCsv()
    {
        // Arrange
        var csv = System.Text.Encoding.UTF8.GetBytes("ПІБ,Клас\nІваненко Іван,11\n");

        // Act
        var preview = await Upload(csv, "people.csv");

        // Assert
        preview.Sheets.Should().Equal("people");
        preview.SuggestedMapping.Fields.Should().ContainKey(ParticipantField.FullName).And.ContainKey(ParticipantField.Group);
    }

    [Fact]
    public async Task ShouldRePreviewWithChosenHeaderRow()
    {
        // Arrange
        var csv = System.Text.Encoding.UTF8.GetBytes("Список учасників,\nПІБ,Клас\nІваненко Іван,11\n");
        var preview = await Upload(csv, "people.csv");

        // Act
        var response = await client.PostAsJsonAsync(
            Route(ApiRoutes.Imports.Preview, preview.ImportId), new ImportPreviewRequestDto("people", 2), JsonOptions);
        var unknownSheet = await client.PostAsJsonAsync(
            Route(ApiRoutes.Imports.Preview, preview.ImportId), new ImportPreviewRequestDto("Немає", 2), JsonOptions);
        var invalidRow = await client.PostAsJsonAsync(
            Route(ApiRoutes.Imports.Preview, preview.ImportId), new ImportPreviewRequestDto("people", 0), JsonOptions);

        // Assert
        preview.SuggestedMapping.Fields.Should().NotContainKey(ParticipantField.FullName);
        var rePreview = await response.ToResponseModel<ImportPreviewDto>();
        rePreview.ImportId.Should().Be(preview.ImportId);
        rePreview.Headers.Should().Equal("ПІБ", "Клас");
        rePreview.SampleRows.Should().ContainSingle().Which.Should().Equal("Іваненко Іван", "11");
        rePreview.SuggestedMapping.Fields.Should().ContainKey(ParticipantField.FullName);
        await ShouldBeProblem(unknownSheet, HttpStatusCode.BadRequest, ErrorCode.InvalidArgument);
        await ShouldBeProblem(invalidRow, HttpStatusCode.BadRequest, ErrorCode.ValidationFailed);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownSession()
    {
        // Act
        var response = await PostFile(await FilledTemplate(), TemplateFile, Guid.NewGuid());

        // Assert
        await ShouldBeProblem(response, HttpStatusCode.NotFound, ErrorCode.SessionNotFound);
    }

    [Fact]
    public async Task ShouldRejectInvalidFile()
    {
        // Act
        var response = await PostFile([1, 2, 3], "people.txt", session.Id.Value);

        // Assert
        await ShouldBeProblem(response, HttpStatusCode.BadRequest, ErrorCode.ImportFileInvalid);
    }

    [Fact]
    public async Task ShouldRejectTooLargeFile()
    {
        // Arrange
        using var limited = CreateClient(new ApplicationSettings { Import = new ImportSettings { MaxFileSizeMb = 0 } });
        using var content = FileContent(await FilledTemplate(), TemplateFile);

        // Act
        var response = await limited.PostAsync(Route(ApiRoutes.Imports.Upload, session.Id.Value), content);

        // Assert
        await ShouldBeProblem(response, HttpStatusCode.RequestEntityTooLarge, ErrorCode.ImportFileTooLarge);
    }

    [Fact]
    public async Task ShouldRejectUnknownSheet()
    {
        // Arrange
        var preview = await Upload(await FilledTemplate([null, "Іваненко", "Іван"]));
        var mapping = Mapping(preview, skipInvalid: false) with { Sheet = "Немає" };

        // Act
        var response = await client.PostAsJsonAsync(Route(ApiRoutes.Imports.Validate, preview.ImportId), mapping, JsonOptions);

        // Assert
        await ShouldBeProblem(response, HttpStatusCode.BadRequest, ErrorCode.InvalidArgument);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownImport()
    {
        // Arrange
        var mapping = new ImportMappingDto(ExportSheets.Template, 1, new ColumnMappingDto(new Dictionary<ParticipantField, string> { [ParticipantField.FullName] = "ПІБ" }, new Dictionary<string, string>()), ImportMode.Append, DedupeStrategy.ExternalId);

        // Act
        var response = await client.PostAsJsonAsync(Route(ApiRoutes.Imports.Validate, Guid.NewGuid()), mapping, JsonOptions);

        // Assert
        await ShouldBeProblem(response, HttpStatusCode.NotFound, ErrorCode.ImportNotFound);
    }

    private static string Route(string template, Guid id)
        => template.Replace("{sessionId:guid}", id.ToString()).Replace("{importId:guid}", id.ToString());

    private static ImportMappingDto Mapping(ImportPreviewDto preview, bool skipInvalid)
        => new(preview.Sheets[0], 1, preview.SuggestedMapping, ImportMode.Append, DedupeStrategy.ExternalId, skipInvalid);

    private static async Task ShouldBeProblem(HttpResponseMessage response, HttpStatusCode status, ErrorCode code)
    {
        response.StatusCode.Should().Be(status);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty(ErrorExtensionKeys.Code).GetString().Should().Be(code.ToString());
    }

    private static MultipartFormDataContent FileContent(byte[] bytes, string fileName)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(ContentTypes.Xlsx);
        return new MultipartFormDataContent { { file, FileField, fileName } };
    }

    private HttpClient CreateClient(ApplicationSettings settings)
        => baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveServiceByType(typeof(IParticipantRepository));
            services.RemoveServiceByType(typeof(IParticipantQueries));
            services.RemoveServiceByType(typeof(IExamSessionQueries));
            services.RemoveServiceByType(typeof(ApplicationSettings));
            services.AddSingleton<IParticipantRepository>(store);
            services.AddSingleton<IParticipantQueries>(store);
            services.AddSingleton<IExamSessionQueries>(new FakeExamSessionQueries(session));
            services.AddSingleton(settings);
        })).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<byte[]> FilledTemplate(params string?[][] rows)
    {
        var template = await (await client.GetAsync(ApiRoutes.Imports.Template)).Content.ReadAsByteArrayAsync();
        using var workbook = new XLWorkbook(new MemoryStream(template));
        var sheet = workbook.Worksheet(ExportSheets.Template);
        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                if (rows[r][c] is { } value)
                {
                    sheet.Cell(r + 2, c + 1).Value = value;
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private Task<HttpResponseMessage> PostFile(byte[] bytes, string fileName, Guid sessionId)
    {
        var content = FileContent(bytes, fileName);
        return client.PostAsync(Route(ApiRoutes.Imports.Upload, sessionId), content);
    }

    private async Task<ImportPreviewDto> Upload(byte[] bytes, string fileName = TemplateFile)
    {
        var response = await PostFile(bytes, fileName, session.Id.Value);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<ImportPreviewDto>();
    }
}