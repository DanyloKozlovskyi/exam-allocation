using Application.Imports.Models;
using Domain.Participants;

namespace Application.Imports.Services;

/// <summary>
/// Keys used to detect duplicates of imported rows (in the file and against existing participants).
/// Comparison is case-insensitive and whitespace-insensitive.
/// </summary>
public static class ImportDedupe
{
    private const char Separator = ' ';

    public static string? ExternalIdKey(string? externalId)
        => string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim().ToUpperInvariant();

    public static string? FullNameKey(string? lastName, string? firstName, string? middleName)
    {
        var parts = new[] { lastName, firstName, middleName }
            .SelectMany(x => (x ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToList();
        return parts.Count == 0 ? null : string.Join(Separator, parts).ToUpperInvariant();
    }

    public static string? RowKey(ParsedParticipantRow row, DedupeStrategy strategy) => strategy switch
    {
        DedupeStrategy.ExternalId => ExternalIdKey(row.ExternalId),
        DedupeStrategy.FullName => FullNameKey(row.LastName, row.FirstName, row.MiddleName),
        _ => null,
    };

    public static string? ParticipantKey(Participant participant, DedupeStrategy strategy) => strategy switch
    {
        DedupeStrategy.ExternalId => ExternalIdKey(participant.ExternalId),
        DedupeStrategy.FullName => FullNameKey(participant.Name.LastName, participant.Name.FirstName, participant.Name.MiddleName),
        _ => null,
    };

    /// <summary>Existing participants by dedupe key (first one wins for ambiguous keys).</summary>
    public static IReadOnlyDictionary<string, Participant> Index(IEnumerable<Participant> existing, DedupeStrategy strategy)
    {
        var index = new Dictionary<string, Participant>();
        foreach (var participant in existing)
        {
            if (ParticipantKey(participant, strategy) is { } key)
            {
                index.TryAdd(key, participant);
            }
        }

        return index;
    }

    /// <summary>The existing participant a row updates on commit, if any.</summary>
    public static Participant? FindMatch(ParsedParticipantRow row, IReadOnlyDictionary<string, Participant> index, DedupeStrategy strategy)
        => RowKey(row, strategy) is { } key && index.TryGetValue(key, out var participant) ? participant : null;
}