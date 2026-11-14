using System.Text;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>Groups non-pinned participants into interchangeable classes (04 §3.1).</summary>
public sealed class ClassBuilder : IClassBuilder
{
    private const char SignatureSeparator = '\u001f';
    private const char MissingMarker = '\u001e';

    public IReadOnlyList<ParticipantClass> Build(SolverInput input)
    {
        var keys = AttributeValues.ReferencedKeys(input.Rules);
        var pinned = input.Pins.Select(p => p.ParticipantId).ToHashSet();
        var groups = new Dictionary<string, List<SolverParticipant>>(StringComparer.Ordinal);
        var values = new Dictionary<string, (IReadOnlyDictionary<string, string> Values, bool NeedsAccessibleSeat)>(StringComparer.Ordinal);

        foreach (var participant in input.Participants)
        {
            if (pinned.Contains(participant.ParticipantId))
            {
                continue;
            }

            var (signature, classValues) = Signature(participant, keys);
            if (!groups.TryGetValue(signature, out var members))
            {
                members = [];
                groups[signature] = members;
                values[signature] = (classValues, participant.NeedsAccessibleSeat);
            }

            members.Add(participant);
        }

        return groups
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select((g, index) => new ParticipantClass(
                index,
                values[g.Key].Values,
                values[g.Key].NeedsAccessibleSeat,
                g.Value
                    .OrderBy(p => p.SortKey, StringComparer.Ordinal)
                    .ThenBy(p => p.ParticipantId)
                    .Select(p => p.ParticipantId)
                    .ToList()))
            .ToList();
    }

    private static (string Signature, IReadOnlyDictionary<string, string> Values) Signature(SolverParticipant participant, IReadOnlyList<string> keys)
    {
        var builder = new StringBuilder();
        builder.Append(participant.NeedsAccessibleSeat ? '1' : '0');
        var classValues = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            builder.Append(SignatureSeparator);
            if (AttributeValues.TryGet(participant, key, out var value))
            {
                builder.Append(value);
                classValues[key] = value;
            }
            else
            {
                // Missing value is unique: the participant gets its own class.
                builder.Append(MissingMarker).Append(participant.ParticipantId.ToString("N"));
            }
        }

        return (builder.ToString(), classValues);
    }
}