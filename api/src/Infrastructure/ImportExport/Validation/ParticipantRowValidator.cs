using Application.Common.Interfaces;
using Application.Imports.Models;
using Application.Imports.Services;
using Domain.Participants;
using Domain.ValueObjects;
using Infrastructure.ImportExport.Common;
using Infrastructure.ImportExport.Constants;

namespace Infrastructure.ImportExport.Validation;

/// <summary>
/// Turns raw sheet rows into <see cref="ParsedParticipantRow"/>s and reports <see cref="ImportErrorCode"/>s.
/// <list type="bullet">
/// <item>Row numbers are 1-based sheet rows; <see cref="ImportOptions.HeaderRow"/> is 1-based too.</item>
/// <item>Empty rows are skipped silently (<see cref="ImportErrorCode.EmptyRow"/> is never reported).</item>
/// <item>External ids are always unique in the file; with <see cref="DedupeStrategy.FullName"/> full names are too.</item>
/// <item><see cref="ImportErrorCode.DuplicateExisting"/> for a row matching an existing participant by
/// <see cref="ImportOptions.DedupeBy"/> is informational: the row stays valid and updates that participant on commit.
/// An external id colliding with any other existing participant is a blocking error.</item>
/// <item>Header-level errors (<see cref="ImportErrorCode.AttributeKeyInvalid"/>) invalidate every data row.</item>
/// </list>
/// Throws <see cref="ArgumentException"/> when the header row is out of range or a mapped header is missing.
/// </summary>
public class ParticipantRowValidator : IParticipantRowValidator
{
    public ImportValidation Validate(WorkbookSheet sheet, ImportOptions options, IReadOnlyList<Participant> existing)
    {
        var context = new ValidationContext(sheet, options, existing);
        return context.Run();
    }

    private sealed class ValidationContext(WorkbookSheet sheet, ImportOptions options, IReadOnlyList<Participant> existing)
    {
        private readonly List<ImportRowError> errors = [];
        private readonly List<ParsedParticipantRow> validRows = [];
        private readonly List<int> duplicateRows = [];
        private readonly Dictionary<string, int> externalIdsInFile = [];
        private readonly Dictionary<string, int> fullNamesInFile = [];
        private readonly IReadOnlyDictionary<string, Participant> existingByExternalId = ImportDedupe.Index(existing, DedupeStrategy.ExternalId);
        private readonly IReadOnlyDictionary<string, Participant> existingByKey = ImportDedupe.Index(existing, options.DedupeBy);
        private Dictionary<ParticipantField, int> fieldColumns = [];
        private List<(string Key, string Header, int Column)> attributeColumns = [];

        private static Dictionary<string, int> IndexHeaders(IReadOnlyList<string?> headerCells)
        {
            var columns = new Dictionary<string, int>();
            for (var column = 0; column < headerCells.Count; column++)
            {
                var key = ImportText.HeaderKey(headerCells[column]);
                if (key.Length > 0)
                {
                    columns.TryAdd(key, column);
                }
            }

            return columns;
        }

        private static int ResolveColumn(Dictionary<string, int> columns, string header)
            => columns.TryGetValue(ImportText.HeaderKey(header), out var column)
                ? column
                : throw new ArgumentException($"Mapped header '{header}' does not exist in the header row", nameof(options));

        private static bool TryCreateAttributeKey(string raw, out string key)
        {
            try
            {
                key = AttributeKey.From(raw).Value;
                return key is not AttributeKeys.Group and not AttributeKeys.School;
            }
            catch (ArgumentException)
            {
                key = string.Empty;
                return false;
            }
        }

        private static void RequireAndLimit(string? value, ParticipantField field, int maxLength, Action<ParticipantField, ImportErrorCode> error)
        {
            if (value is null)
            {
                error(field, ImportErrorCode.RequiredMissing);
                return;
            }

            Limit(value, field, maxLength, error);
        }

        private static void Limit(string? value, ParticipantField field, int maxLength, Action<ParticipantField, ImportErrorCode> error)
        {
            if (value is not null && value.Length > maxLength)
            {
                error(field, ImportErrorCode.ValueTooLong);
            }
        }

        private static bool TryParseBoolean(string value, out bool result)
        {
            result = ImportValues.TrueValues.Contains(value);
            return result || ImportValues.FalseValues.Contains(value);
        }

        private bool ResolveAttributes(Dictionary<string, int> columns)
        {
            var valid = true;
            var resolved = new List<(string Key, string Header, int Column)>();
            foreach (var (rawKey, header) in options.Mapping.Attributes)
            {
                var column = ResolveColumn(columns, header);
                if (!TryCreateAttributeKey(rawKey, out var key)
                    || resolved.Any(x => x.Key == key)
                    || resolved.Count >= ParticipantConstraints.MaxAttributes)
                {
                    errors.Add(new ImportRowError(options.HeaderRow, header, ImportErrorCode.AttributeKeyInvalid));
                    valid = false;
                    continue;
                }

                resolved.Add((key, header, column));
            }

            attributeColumns = resolved;
            return valid;
        }

        public ImportValidation Run()
        {
            if (options.HeaderRow < 1 || options.HeaderRow > sheet.Rows.Count)
            {
                throw new ArgumentException($"Header row {options.HeaderRow} is outside the sheet (1..{sheet.Rows.Count})", nameof(options));
            }

            var columns = IndexHeaders(sheet.Rows[options.HeaderRow - 1]);
            fieldColumns = options.Mapping.Fields.ToDictionary(x => x.Key, x => ResolveColumn(columns, x.Value));
            var headerValid = ResolveAttributes(columns);

            var errorRows = 0;
            for (var index = options.HeaderRow; index < sheet.Rows.Count; index++)
            {
                var cells = sheet.Rows[index];
                if (cells.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (!headerValid || !ValidateRow(index + 1, cells))
                {
                    errorRows++;
                }
            }

            return new ImportValidation(validRows, errorRows, errors, duplicateRows);
        }

        private bool ValidateRow(int rowNumber, IReadOnlyList<string?> cells)
        {
            var rowErrors = new List<ImportRowError>();
            string? Value(ParticipantField field)
                => fieldColumns.TryGetValue(field, out var column) && column < cells.Count ? ImportText.Clean(cells[column]) : null;
            string? Header(ParticipantField field) => options.Mapping.Fields.GetValueOrDefault(field);
            void Error(ParticipantField field, ImportErrorCode code) => rowErrors.Add(new ImportRowError(rowNumber, Header(field), code));

            var (lastName, firstName, middleName) = ResolveName(Value);
            RequireAndLimit(lastName, NameSource(ParticipantField.LastName), PersonName.PartMaxLength, Error);
            RequireAndLimit(firstName, NameSource(ParticipantField.FirstName), PersonName.PartMaxLength, Error);
            Limit(middleName, NameSource(ParticipantField.MiddleName), PersonName.PartMaxLength, Error);

            var externalId = Value(ParticipantField.ExternalId);
            Limit(externalId, ParticipantField.ExternalId, ParticipantConstraints.ExternalIdMaxLength, Error);
            var group = Value(ParticipantField.Group);
            Limit(group, ParticipantField.Group, ParticipantConstraints.GroupMaxLength, Error);
            var school = Value(ParticipantField.School);
            Limit(school, ParticipantField.School, ParticipantConstraints.SchoolMaxLength, Error);
            var notes = Value(ParticipantField.Notes);
            Limit(notes, ParticipantField.Notes, ParticipantConstraints.NotesMaxLength, Error);

            var needsAccessibleSeat = false;
            if (Value(ParticipantField.NeedsAccessibleSeat) is { } flag && !TryParseBoolean(flag, out needsAccessibleSeat))
            {
                Error(ParticipantField.NeedsAccessibleSeat, ImportErrorCode.BooleanUnparseable);
            }

            var attributes = new Dictionary<string, string>();
            foreach (var (key, header, column) in attributeColumns)
            {
                var value = column < cells.Count ? ImportText.Clean(cells[column]) : null;
                if (value is null)
                {
                    continue;
                }

                if (value.Length > ParticipantConstraints.AttributeValueMaxLength)
                {
                    rowErrors.Add(new ImportRowError(rowNumber, header, ImportErrorCode.ValueTooLong));
                }

                attributes[key] = value;
            }

            var row = new ParsedParticipantRow(rowNumber, externalId, lastName, firstName, middleName, group, school, needsAccessibleSeat, attributes, notes);
            var blocking = rowErrors.Count > 0;
            blocking |= CheckDuplicates(row, rowErrors);
            errors.AddRange(rowErrors);
            if (!blocking)
            {
                validRows.Add(row);
            }

            return !blocking;
        }

        /// <summary>Field whose header is reported for a name part (the full-name column when the part is split from it).</summary>
        private ParticipantField NameSource(ParticipantField part)
            => fieldColumns.ContainsKey(part) || !fieldColumns.ContainsKey(ParticipantField.FullName) ? part : ParticipantField.FullName;

        private (string? LastName, string? FirstName, string? MiddleName) ResolveName(Func<ParticipantField, string?> value)
        {
            var lastName = value(ParticipantField.LastName);
            var firstName = value(ParticipantField.FirstName);
            var middleName = value(ParticipantField.MiddleName);
            if (lastName is not null && firstName is not null)
            {
                return (lastName, firstName, middleName);
            }

            var parts = value(ParticipantField.FullName)?.Split(ImportValues.Space, StringSplitOptions.RemoveEmptyEntries) ?? [];
            if (parts.Length == 0)
            {
                return (lastName, firstName, middleName);
            }

            return (
                lastName ?? parts[0],
                firstName ?? parts.ElementAtOrDefault(1),
                middleName ?? (parts.Length > 2 ? string.Join(ImportValues.Space, parts.Skip(2)) : null));
        }

        /// <summary>Returns true when a blocking duplicate was found.</summary>
        private bool CheckDuplicates(ParsedParticipantRow row, List<ImportRowError> rowErrors)
        {
            var blocking = false;
            var externalIdKey = ImportDedupe.ExternalIdKey(row.ExternalId);
            var fullNameKey = options.DedupeBy == DedupeStrategy.FullName
                ? ImportDedupe.FullNameKey(row.LastName, row.FirstName, row.MiddleName)
                : null;
            var externalIdHeader = options.Mapping.Fields.GetValueOrDefault(ParticipantField.ExternalId);

            if ((externalIdKey is not null && externalIdsInFile.ContainsKey(externalIdKey))
                || (fullNameKey is not null && fullNamesInFile.ContainsKey(fullNameKey)))
            {
                var column = externalIdKey is not null && externalIdsInFile.ContainsKey(externalIdKey) ? externalIdHeader : null;
                rowErrors.Add(new ImportRowError(row.Row, column, ImportErrorCode.DuplicateInFile));
                duplicateRows.Add(row.Row);
                return true;
            }

            if (externalIdKey is not null)
            {
                externalIdsInFile[externalIdKey] = row.Row;
            }

            if (fullNameKey is not null)
            {
                fullNamesInFile[fullNameKey] = row.Row;
            }

            var match = ImportDedupe.FindMatch(row, existingByKey, options.DedupeBy);
            if (match is not null)
            {
                rowErrors.Add(new ImportRowError(row.Row, null, ImportErrorCode.DuplicateExisting));
                duplicateRows.Add(row.Row);
            }

            if (externalIdKey is not null
                && existingByExternalId.TryGetValue(externalIdKey, out var sameExternalId)
                && !ReferenceEquals(sameExternalId, match))
            {
                rowErrors.Add(new ImportRowError(row.Row, externalIdHeader, ImportErrorCode.DuplicateExisting));
                if (match is null)
                {
                    duplicateRows.Add(row.Row);
                }

                blocking = true;
            }

            return blocking;
        }
    }
}