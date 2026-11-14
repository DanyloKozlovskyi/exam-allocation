using Solver.Model;

namespace Tools.Shared.SyntheticData;

/// <summary>Shape mirrors <c>TestCenterDto</c> so the files can feed MSW fixtures.</summary>
public sealed record CenterRecord(
    Guid Id,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    string? GooglePlaceId,
    decimal CostPerRoom,
    bool IsActive,
    int RoomCount,
    int Capacity);

/// <summary>Shape mirrors <c>RoomLayoutDto</c>.</summary>
public sealed record RoomLayoutRecord(
    int Rows,
    int Columns,
    int SeatsPerDesk,
    IReadOnlyList<int> AislesAfterColumns,
    IReadOnlyList<string> DisabledSeats,
    IReadOnlyList<string> AccessibleSeats);

/// <summary>Shape mirrors <c>RoomDto</c>.</summary>
public sealed record RoomRecord(
    Guid Id,
    Guid CenterId,
    string Name,
    RoomLayoutRecord Layout,
    decimal? CostOverride,
    bool IsActive,
    int Capacity,
    int AccessibleCapacity);

/// <summary>Shape mirrors <c>ParticipantDto</c>.</summary>
public sealed record ParticipantRecord(
    Guid Id,
    string ExternalId,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Group,
    string School,
    bool NeedsAccessibleSeat,
    IReadOnlyDictionary<string, string> Attributes,
    string? Notes);

/// <summary>Shape mirrors <c>RuleDto</c>; parameters use the polymorphic <c>type</c> discriminator.</summary>
public sealed record RuleRecord(
    Guid Id,
    RuleType Type,
    bool IsHard,
    int Weight,
    bool IsEnabled,
    string DisplayName,
    RuleParameters Parameters);

public sealed record Dataset(
    GeneratorSettings Settings,
    IReadOnlyList<CenterRecord> Centers,
    IReadOnlyList<RoomRecord> Rooms,
    IReadOnlyList<ParticipantRecord> Participants,
    IReadOnlyList<RuleRecord> Rules,
    SolverInput SolverInput);