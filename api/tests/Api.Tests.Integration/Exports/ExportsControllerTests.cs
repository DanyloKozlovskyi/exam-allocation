using System.Net;
using System.Text.Json;
using Api.Constants;
using Application.Allocations.Models;
using Application.Allocations.Services;
using Application.Common.Errors;
using Domain.Allocations;
using Domain.Rooms;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.ImportExport.Constants;
using LanguageExt;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;

namespace Api.Tests.Integration.Exports;

/// <summary>The real export model query arrives in Phase 2; a fake builder supplies a sample model here.</summary>
public class ExportsControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string AllocationIdPlaceholder = "{allocationId:guid}";
    private const string FormatQuery = "?format=";
    private const string RoomQuery = "&roomId=";

    private static readonly Guid AllocationGuid = Guid.NewGuid();
    private static readonly Guid RoomGuid = Guid.NewGuid();
    private readonly IntegrationTestWebFactory baseFactory = factory;
    private HttpClient client = null!;

    public static TheoryData<ExportFormat, string, string> Formats() => new()
    {
        { ExportFormat.Xlsx, ContentTypes.Xlsx, "allocation-2.xlsx" },
        { ExportFormat.Xml, ContentTypes.Xml, "allocation-2.xml" },
        { ExportFormat.Csv, ContentTypes.Csv, "allocation-2.csv" },
        { ExportFormat.Pdf, ContentTypes.Pdf, "allocation-2.pdf" },
    };

    public Task InitializeAsync()
    {
        client = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveServiceByType(typeof(IAllocationExportModelBuilder));
            services.AddSingleton<IAllocationExportModelBuilder>(new FakeExportModelBuilder());
        })).CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        client.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public async Task ShouldExportWithContentTypeAndFileName(ExportFormat format, string contentType, string fileName)
    {
        // Act
        var response = await client.GetAsync(Url(AllocationGuid, format));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(contentType);
        response.Content.Headers.ContentDisposition!.FileName.Should().Be(fileName);
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldExportPdfForOneRoom()
    {
        // Act
        var response = await client.GetAsync(Url(AllocationGuid, ExportFormat.Pdf) + RoomQuery + RoomGuid);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition!.FileName.Should().Be($"allocation-2-room-{RoomGuid:N}.pdf");
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownAllocation()
    {
        // Act
        var response = await client.GetAsync(Url(Guid.NewGuid(), ExportFormat.Xlsx));

        // Assert
        await ShouldBeProblem(response, ErrorCode.AllocationNotFound);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownRoom()
    {
        // Act
        var response = await client.GetAsync(Url(AllocationGuid, ExportFormat.Pdf) + RoomQuery + Guid.NewGuid());

        // Assert
        await ShouldBeProblem(response, ErrorCode.RoomNotFound);
    }

    private static string Url(Guid allocationId, ExportFormat format)
        => ApiRoutes.Exports.Export.Replace(AllocationIdPlaceholder, allocationId.ToString()) + FormatQuery + format;

    private static async Task ShouldBeProblem(HttpResponseMessage response, ErrorCode code)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty(ErrorExtensionKeys.Code).GetString().Should().Be(code.ToString());
    }

    private sealed class FakeExportModelBuilder : IAllocationExportModelBuilder
    {
        public Task<Option<AllocationExportModel>> Build(AllocationId allocationId, CancellationToken cancellationToken)
            => Task.FromResult(allocationId.Value == AllocationGuid ? Option<AllocationExportModel>.Some(Model()) : Option<AllocationExportModel>.None);

        private static AllocationExportModel Model()
        {
            var layout = RoomLayout.Create(2, 2, 1, [0], [], []);
            var seats = new[]
            {
                new ExportSeat(SeatKey.From(0, 0, 0).Value, 0, 0, 0, new ExportParticipant(Guid.NewGuid(), "A-1", "Іваненко", "Іван", null, "11-А", null)),
                new ExportSeat(SeatKey.From(0, 1, 0).Value, 0, 1, 0, null),
                new ExportSeat(SeatKey.From(1, 0, 0).Value, 1, 0, 0, new ExportParticipant(Guid.NewGuid(), null, "Петренко", "Петро", "Петрович", null, "Ліцей")),
                new ExportSeat(SeatKey.From(1, 1, 0).Value, 1, 1, 0, null),
            };
            var room = new ExportRoom(RoomGuid, "Ауд. 101", seats.Length, layout, seats);
            var center = new ExportCenter(Guid.NewGuid(), "Центр", "вул. Тестова, 1", 50.45, 30.52, [room]);
            return new AllocationExportModel("Сесія", null, 2, new AllocationObjective { RoomsUsed = 1, Cost = 1m }, DateTimeOffset.UtcNow, [center]);
        }
    }
}