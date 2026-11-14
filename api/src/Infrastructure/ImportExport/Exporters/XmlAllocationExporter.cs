using System.Globalization;
using System.Text;
using System.Xml;
using Application.Allocations.Models;
using Application.Common.Interfaces;
using Infrastructure.ImportExport.Constants;
using Attr = Infrastructure.ImportExport.Constants.XmlNames.Attributes;

namespace Infrastructure.ImportExport.Exporters;

/// <summary>Streams the allocation as XML in namespace <see cref="XmlNames.Namespace"/> (schema: <c>samples/allocation.xsd</c>).</summary>
public class XmlAllocationExporter : IAllocationExporter
{
    private static readonly XmlWriterSettings WriterSettings = new()
    {
        Async = true,
        Indent = true,
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
    };

    public ExportFormat Format => ExportFormat.Xml;

    public async Task<ExportFile> Export(AllocationExportModel model, Guid? roomId, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await using (var writer = XmlWriter.Create(stream, WriterSettings))
        {
            await Write(writer, model, cancellationToken);
        }

        return new ExportFile(ExportFileNames.ForAllocation(model, FileExtensions.Xml), ContentTypes.Xml, stream.ToArray());
    }

    private static async Task Write(XmlWriter writer, AllocationExportModel model, CancellationToken cancellationToken)
    {
        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, XmlNames.Allocation, XmlNames.Namespace);
        await Attribute(writer, Attr.Session, model.SessionName);
        if (model.ExamDate is { } date)
        {
            await Attribute(writer, Attr.ExamDate, date.ToString(XmlNames.Formats.Date, CultureInfo.InvariantCulture));
        }

        await Attribute(writer, Attr.GeneratedAt, model.GeneratedAt.ToString(XmlNames.Formats.DateTime, CultureInfo.InvariantCulture));
        await Attribute(writer, Attr.Index, Invariant(model.AllocationIndex));

        await writer.WriteStartElementAsync(null, XmlNames.Objective, XmlNames.Namespace);
        await Attribute(writer, Attr.RoomsUsed, Invariant(model.Objective.RoomsUsed));
        await Attribute(writer, Attr.Cost, model.Objective.Cost.ToString(XmlNames.Formats.Cost, CultureInfo.InvariantCulture));
        await Attribute(writer, Attr.SoftPenalty, Invariant(model.Objective.SoftPenalty));
        await writer.WriteEndElementAsync();

        foreach (var center in model.Centers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteCenter(writer, center);
        }

        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
    }

    private static async Task WriteCenter(XmlWriter writer, ExportCenter center)
    {
        await writer.WriteStartElementAsync(null, XmlNames.Center, XmlNames.Namespace);
        await Attribute(writer, Attr.Id, center.Id.ToString());
        await Attribute(writer, Attr.Name, center.Name);
        await Attribute(writer, Attr.Address, center.Address);
        await Attribute(writer, Attr.Latitude, center.Latitude.ToString(XmlNames.Formats.Coordinate, CultureInfo.InvariantCulture));
        await Attribute(writer, Attr.Longitude, center.Longitude.ToString(XmlNames.Formats.Coordinate, CultureInfo.InvariantCulture));

        foreach (var room in center.Rooms)
        {
            await writer.WriteStartElementAsync(null, XmlNames.Room, XmlNames.Namespace);
            await Attribute(writer, Attr.Id, room.Id.ToString());
            await Attribute(writer, Attr.Name, room.Name);
            await Attribute(writer, Attr.Capacity, Invariant(room.Capacity));
            await Attribute(writer, Attr.Filled, Invariant(room.Seats.Count(x => x.Participant is not null)));
            foreach (var seat in room.Seats.OrderBy(x => x.Row).ThenBy(x => x.Column).ThenBy(x => x.SeatIndex))
            {
                await WriteSeat(writer, seat);
            }

            await writer.WriteEndElementAsync();
        }

        await writer.WriteEndElementAsync();
    }

    private static async Task WriteSeat(XmlWriter writer, ExportSeat seat)
    {
        await writer.WriteStartElementAsync(null, XmlNames.Seat, XmlNames.Namespace);
        await Attribute(writer, Attr.Key, seat.SeatKey);
        await Attribute(writer, Attr.Row, Invariant(seat.Row + ExportHeaders.DisplayOffset));
        await Attribute(writer, Attr.Desk, Invariant(seat.Column + ExportHeaders.DisplayOffset));
        await Attribute(writer, Attr.Place, Invariant(seat.SeatIndex + ExportHeaders.DisplayOffset));
        if (seat.Participant is { } participant)
        {
            await writer.WriteStartElementAsync(null, XmlNames.Participant, XmlNames.Namespace);
            await Attribute(writer, Attr.Id, participant.Id.ToString());
            await Attribute(writer, Attr.ExternalId, participant.ExternalId);
            await Attribute(writer, Attr.LastName, participant.LastName);
            await Attribute(writer, Attr.FirstName, participant.FirstName);
            await Attribute(writer, Attr.MiddleName, participant.MiddleName);
            await Attribute(writer, Attr.Group, participant.Group);
            await Attribute(writer, Attr.School, participant.School);
            await writer.WriteEndElementAsync();
        }

        await writer.WriteEndElementAsync();
    }

    /// <summary>Writes an unqualified attribute; <c>null</c>/empty values are omitted (all such attributes are optional in the XSD).</summary>
    private static Task Attribute(XmlWriter writer, string name, string? value)
        => string.IsNullOrEmpty(value) ? Task.CompletedTask : writer.WriteAttributeStringAsync(null, name, null, value);

    private static string Invariant(long value) => value.ToString(CultureInfo.InvariantCulture);
}