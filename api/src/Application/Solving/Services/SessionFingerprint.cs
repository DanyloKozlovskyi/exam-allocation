using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Interfaces.Queries;
using Domain.Rules.Parameters;
using Domain.Sessions;

namespace Application.Solving.Services;

/// <summary>
/// SHA-256 over a normalized, ordered snapshot of the solver-relevant session data: active rooms of
/// active centers (layout, cost), participants (attributes, accessibility, sort key), enabled rules
/// and pins. Labels, addresses, notes and other informational fields do not affect the hash.
/// </summary>
public class SessionFingerprint(
    IRoomQueries roomQueries,
    IParticipantQueries participantQueries,
    IRuleQueries ruleQueries,
    IPinQueries pinQueries) : ISessionFingerprint
{
    private const char FieldSeparator = '|';
    private const char ListSeparator = ',';
    private const char PairSeparator = '=';
    private const string RoomTag = "room";
    private const string ParticipantTag = "participant";
    private const string RuleTag = "rule";
    private const string PinTag = "pin";
    private const string NullValue = "~";

    public async Task<string> Compute(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        var rooms = await roomQueries.GetActiveBySession(sessionId, cancellationToken);
        var participants = await participantQueries.GetAllBySession(sessionId, cancellationToken);
        var rules = await ruleQueries.GetAll(sessionId, cancellationToken);
        var pins = await pinQueries.GetAll(sessionId, cancellationToken);

        var lines = new List<string>();

        lines.AddRange(rooms
            .OrderBy(x => x.Id.Value)
            .Select(room => Join(
                RoomTag,
                room.Id.Value.ToString(),
                room.CenterId.Value.ToString(),
                Number(room.CostOverride ?? room.Center?.CostPerRoom ?? 0m),
                Number(room.Layout.Rows),
                Number(room.Layout.Columns),
                Number(room.Layout.SeatsPerDesk),
                List(room.Layout.AislesAfterColumns.Order().Select(Number)),
                List(room.Layout.DisabledSeats.Order(StringComparer.Ordinal)),
                List(room.Layout.AccessibleSeats.Order(StringComparer.Ordinal)))));

        lines.AddRange(participants
            .OrderBy(x => x.Id.Value)
            .Select(participant => Join(
                ParticipantTag,
                participant.Id.Value.ToString(),
                participant.Name.SortKey,
                participant.Group ?? NullValue,
                participant.School ?? NullValue,
                participant.NeedsAccessibleSeat.ToString(),
                List(participant.Attributes
                    .OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(x => x.Key + PairSeparator + x.Value)))));

        lines.AddRange(rules
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Id.Value)
            .Select(rule => Join(
                RuleTag,
                rule.Id.Value.ToString(),
                rule.Type.ToString(),
                rule.IsHard.ToString(),
                Number(rule.Weight),
                JsonSerializer.Serialize<RuleParameters>(rule.Parameters))));

        lines.AddRange(pins
            .OrderBy(x => x.ParticipantId.Value)
            .Select(pin => Join(
                PinTag,
                pin.ParticipantId.Value.ToString(),
                pin.RoomId.Value.ToString(),
                pin.Seat?.Key.Value ?? pin.SeatId?.Value.ToString() ?? NullValue)));

        var payload = Encoding.UTF8.GetBytes(string.Join('\n', lines));
        return Convert.ToHexStringLower(SHA256.HashData(payload));
    }

    private static string Join(params string[] fields) => string.Join(FieldSeparator, fields.Select(Escape));

    private static string List(IEnumerable<string> values) => string.Join(ListSeparator, values);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Escape(string value) => value.Replace(FieldSeparator.ToString(), "\\" + FieldSeparator, StringComparison.Ordinal);
}