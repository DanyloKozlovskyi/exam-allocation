namespace Application.Participants.Models;

public enum ParticipantSortField
{
    LastName = 1,
    FirstName = 2,
    Group = 3,
    School = 4,
    ExternalId = 5,
}

public sealed record ParticipantFilter(string? Group, string? School, bool? NeedsAccessibleSeat, ParticipantSortField SortBy);

public sealed record AttributeSummary(string Key, int DistinctValues, int MissingCount, IReadOnlyList<string> SampleValues)
{
    public const int SampleSize = 5;
}