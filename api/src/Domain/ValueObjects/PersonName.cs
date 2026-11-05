using Domain.Common;

namespace Domain.ValueObjects;

public sealed class PersonName : ValueObject
{
    public const int PartMaxLength = 100;

    private PersonName(string lastName, string firstName, string? middleName)
    {
        LastName = lastName;
        FirstName = firstName;
        MiddleName = middleName;
    }

    public string LastName { get; private set; }

    public string FirstName { get; private set; }

    public string? MiddleName { get; private set; }

    public string SortKey => string.Join(' ', new[] { LastName, FirstName, MiddleName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public static PersonName From(string lastName, string firstName, string? middleName)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(lastName, nameof(lastName));
        Guard.NotNullOrEmptyOrWhiteSpace(firstName, nameof(firstName));
        Guard.MaxLength(lastName, PartMaxLength, nameof(lastName));
        Guard.MaxLength(firstName, PartMaxLength, nameof(firstName));
        Guard.MaxLength(middleName, PartMaxLength, nameof(middleName));
        return new PersonName(lastName.Trim(), firstName.Trim(), string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LastName;
        yield return FirstName;
        yield return MiddleName;
    }
}