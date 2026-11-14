using Application.Common.Interfaces;
using Application.Imports.Models;
using Domain.Participants;
using Domain.ValueObjects;
using Infrastructure.ImportExport.Common;
using Infrastructure.ImportExport.Constants;

namespace Infrastructure.ImportExport.Mapping;

/// <summary>
/// Matches headers against <see cref="ImportHeaders.Synonyms"/> (case-insensitive, trimmed, apostrophe-normalized).
/// The first header matching a field wins; <see cref="ParticipantField.FullName"/> is suggested only when last and
/// first name are not both present. Remaining headers become attributes keyed by <see cref="AttributeKey"/>.
/// </summary>
public class ColumnMappingSuggester : IColumnMappingSuggester
{
    private static readonly IReadOnlyDictionary<string, ParticipantField> FieldBySynonym = ImportHeaders.Synonyms
        .SelectMany(x => x.Value.Select(synonym => (Key: ImportText.HeaderKey(synonym), Field: x.Key)))
        .ToDictionary(x => x.Key, x => x.Field);

    private static readonly IReadOnlySet<string> ReservedAttributeKeys = new HashSet<string> { AttributeKeys.Group, AttributeKeys.School };

    public ColumnMapping Suggest(IReadOnlyList<string> headers)
    {
        var fields = new Dictionary<ParticipantField, string>();
        var unmatched = new List<string>();

        foreach (var header in headers.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (FieldBySynonym.TryGetValue(ImportText.HeaderKey(header), out var field) && !fields.ContainsKey(field))
            {
                fields[field] = header;
            }
            else
            {
                unmatched.Add(header);
            }
        }

        if (fields.ContainsKey(ParticipantField.LastName) && fields.ContainsKey(ParticipantField.FirstName)
            && fields.Remove(ParticipantField.FullName, out var fullNameHeader))
        {
            unmatched.Add(fullNameHeader);
        }

        return new ColumnMapping(fields, SuggestAttributes(unmatched, fields.Values.ToHashSet()));
    }

    private static Dictionary<string, string> SuggestAttributes(IEnumerable<string> headers, IReadOnlySet<string> mappedHeaders)
    {
        var attributes = new Dictionary<string, string>();
        foreach (var header in headers.Where(x => !mappedHeaders.Contains(x)))
        {
            if (attributes.Count >= ParticipantConstraints.MaxAttributes)
            {
                break;
            }

            if (TryCreateKey(header, out var key) && !ReservedAttributeKeys.Contains(key) && !attributes.ContainsKey(key))
            {
                attributes[key] = header;
            }
        }

        return attributes;
    }

    private static bool TryCreateKey(string header, out string key)
    {
        try
        {
            key = AttributeKey.From(header).Value;
            return true;
        }
        catch (ArgumentException)
        {
            key = string.Empty;
            return false;
        }
    }
}