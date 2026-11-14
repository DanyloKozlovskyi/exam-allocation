using System.Globalization;
using Solver.Model;

namespace Tools.Shared.SyntheticData;

/// <summary>Deterministic (per seed) synthetic exam-session data, see docs/08 "Synthetic data".</summary>
public static class SyntheticDataGenerator
{
    public const string GroupKey = "group";
    public const string SchoolKey = "school";

    private const double KyivLatitude = 50.4501;
    private const double KyivLongitude = 30.5234;
    private const double LatitudeSpread = 0.12;
    private const double LongitudeSpread = 0.18;
    private const decimal BaseCostPerRoom = 1000m;
    private const decimal CostStep = 100m;
    private const int CostLevels = 5;
    private const int GroupsPerSchool = 4;
    private const int MaxCourse = 4;
    private const int MaxHouseNumber = 120;
    private const int RoomsPerFloor = 10;
    private const int FirstRoomNumber = 101;
    private const int FloorNumberStep = 100;
    private const int DisableSeatEveryNthRoom = 3;
    private const double FemaleShare = 0.5;
    private const double GenderedLastNameShare = 0.2;
    private const int SeparationSlack = 1;
    private const double DefaultMinFillRatio = 0.5;
    private const int DefaultSoftWeight = 10;
    private const int HardRuleWeight = 1;
    private const int GuidByteLength = 16;
    private const int GuidVersionByte = 7;
    private const int GuidVariantByte = 8;
    private const byte GuidVersionMask = 0x0F;
    private const byte GuidVersion4 = 0x40;
    private const byte GuidVariantMask = 0x3F;
    private const byte GuidVariantRfc4122 = 0x80;
    private const string ExternalIdFormat = "P{0:D5}";
    private const string CenterNameFormat = "Центр тестування №{0}";
    private const string AddressFormat = "м. Київ, {0}, {1}";
    private const string RoomNameFormat = "Аудиторія {0}";
    private const string GroupNameFormat = "{0}-{1}{2}";

    /// <summary>Mixed layouts from docs/08: 6×5×2, 5×4×2, 8×6×1 with aisles.</summary>
    private static readonly LayoutTemplate[] Layouts =
    [
        new(6, 5, 2, [1]),
        new(5, 4, 2, [1]),
        new(8, 6, 1, [1, 3]),
    ];

    public static Dataset Generate(GeneratorSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var random = new Random(settings.Seed);

        var (centers, rooms) = GenerateCentersAndRooms(settings, random);
        var participants = GenerateParticipants(settings, random);
        var rules = GenerateRules(participants, rooms, random);
        var input = BuildSolverInput(settings, centers, rooms, participants, rules);
        return new Dataset(settings, centers, rooms, participants, rules, input);
    }

    private static (List<CenterRecord> Centers, List<RoomRecord> Rooms) GenerateCentersAndRooms(GeneratorSettings settings, Random random)
    {
        var centers = new List<CenterRecord>(settings.Centers);
        var rooms = new List<RoomRecord>(settings.Centers * settings.RoomsPerCenter);
        var roomIndex = 0;

        for (var c = 0; c < settings.Centers; c++)
        {
            var centerId = NewGuid(random);
            var centerRooms = new List<RoomRecord>(settings.RoomsPerCenter);
            for (var r = 0; r < settings.RoomsPerCenter; r++, roomIndex++)
            {
                centerRooms.Add(BuildRoom(NewGuid(random), centerId, r, roomIndex));
            }

            var latitude = KyivLatitude + (((random.NextDouble() * 2) - 1) * LatitudeSpread);
            var longitude = KyivLongitude + (((random.NextDouble() * 2) - 1) * LongitudeSpread);
            var street = NameLists.Streets[random.Next(NameLists.Streets.Length)];
            var house = random.Next(1, MaxHouseNumber);

            centers.Add(new CenterRecord(
                centerId,
                Format(CenterNameFormat, c + 1),
                Format(AddressFormat, street, house),
                Math.Round(latitude, 6),
                Math.Round(longitude, 6),
                null,
                BaseCostPerRoom + (CostStep * (c % CostLevels)),
                true,
                centerRooms.Count,
                centerRooms.Sum(x => x.Capacity)));
            rooms.AddRange(centerRooms);
        }

        return (centers, rooms);
    }

    private static RoomRecord BuildRoom(Guid id, Guid centerId, int indexInCenter, int globalIndex)
    {
        var template = Layouts[globalIndex % Layouts.Length];
        var lastSeat = template.SeatsPerDesk - 1;
        var lastColumn = template.Columns - 1;

        // Front row, outer desks, first seat: close to the door.
        string[] accessible = [SeatKeyFormat.Format(0, 0, 0), SeatKeyFormat.Format(0, lastColumn, 0)];
        string[] disabled = globalIndex % DisableSeatEveryNthRoom == DisableSeatEveryNthRoom - 1
            ? [SeatKeyFormat.Format(template.Rows - 1, lastColumn, lastSeat)]
            : [];

        var layout = new RoomLayoutRecord(template.Rows, template.Columns, template.SeatsPerDesk, template.Aisles, disabled, accessible);
        var capacity = (template.Rows * template.Columns * template.SeatsPerDesk) - disabled.Length;
        var number = FirstRoomNumber + ((indexInCenter / RoomsPerFloor) * FloorNumberStep) + (indexInCenter % RoomsPerFloor);
        return new RoomRecord(id, centerId, Format(RoomNameFormat, number), layout, null, true, capacity, accessible.Length);
    }

    private static List<ParticipantRecord> GenerateParticipants(GeneratorSettings settings, Random random)
    {
        var participants = new List<ParticipantRecord>(settings.Participants);
        var needs = PickNeeds(settings, random);
        var groupIndex = 0;
        var school = string.Empty;

        while (participants.Count < settings.Participants)
        {
            if (groupIndex % GroupsPerSchool == 0)
            {
                var template = NameLists.SchoolTemplates[random.Next(NameLists.SchoolTemplates.Length)];
                school = Format(template, (groupIndex / GroupsPerSchool) + 1);
            }

            var prefix = NameLists.GroupPrefixes[groupIndex % NameLists.GroupPrefixes.Length];
            var course = random.Next(1, MaxCourse + 1);
            var group = Format(GroupNameFormat, prefix, course, (groupIndex / NameLists.GroupPrefixes.Length) + 1);
            var size = Math.Min(random.Next(settings.MinGroupSize, settings.MaxGroupSize + 1), settings.Participants - participants.Count);

            for (var i = 0; i < size; i++)
            {
                var index = participants.Count;
                var (lastName, firstName, middleName) = NextName(random);
                participants.Add(new ParticipantRecord(
                    NewGuid(random),
                    Format(ExternalIdFormat, index + 1),
                    lastName,
                    firstName,
                    middleName,
                    group,
                    school,
                    needs.Contains(index),
                    new Dictionary<string, string>(),
                    null));
            }

            groupIndex++;
        }

        return participants;
    }

    private static HashSet<int> PickNeeds(GeneratorSettings settings, Random random)
    {
        var count = (int)Math.Round(settings.Participants * settings.NeedsRatio, MidpointRounding.AwayFromZero);
        var picked = new HashSet<int>();
        while (picked.Count < Math.Min(count, settings.Participants))
        {
            picked.Add(random.Next(settings.Participants));
        }

        return picked;
    }

    private static (string LastName, string FirstName, string MiddleName) NextName(Random random)
    {
        var female = random.NextDouble() < FemaleShare;
        var firstNames = female ? NameLists.FemaleFirstNames : NameLists.MaleFirstNames;
        var firstName = firstNames[random.Next(firstNames.Length)];
        var patronymic = NameLists.Patronymics[random.Next(NameLists.Patronymics.Length)];

        string lastName;
        if (random.NextDouble() < GenderedLastNameShare)
        {
            var pair = NameLists.GenderedLastNames[random.Next(NameLists.GenderedLastNames.Length)];
            lastName = female ? pair.Female : pair.Male;
        }
        else
        {
            lastName = NameLists.NeutralLastNames[random.Next(NameLists.NeutralLastNames.Length)];
        }

        return (lastName, firstName, female ? patronymic.Female : patronymic.Male);
    }

    /// <summary>Default rule set from docs/08: R2 group/Room hard, R3 group hard, R4 (when needed), R5 soft 0.5.</summary>
    private static List<RuleRecord> GenerateRules(List<ParticipantRecord> participants, List<RoomRecord> rooms, Random random)
    {
        var largestGroup = participants.GroupBy(x => x.Group).Max(x => x.Count());
        var maxPerRoom = (int)Math.Ceiling((double)largestGroup / Math.Max(rooms.Count, 1)) + SeparationSlack;

        var rules = new List<RuleRecord>
        {
            new(NewGuid(random), RuleType.SeparateByAttribute, true, HardRuleWeight, true, "Розподіл груп по аудиторіях", new SeparateByAttributeParameters(GroupKey, RuleScope.Room, maxPerRoom)),
            new(NewGuid(random), RuleType.NoAdjacentByAttribute, true, HardRuleWeight, true, "Не саджати одногрупників поруч", new NoAdjacentByAttributeParameters(GroupKey, [AdjacencyKind.SameDesk, AdjacencyKind.Side, AdjacencyKind.FrontBack], false)),
        };

        if (participants.Exists(x => x.NeedsAccessibleSeat))
        {
            rules.Add(new(NewGuid(random), RuleType.AccessibleSeating, true, HardRuleWeight, true, "Доступні місця", new AccessibleSeatingParameters()));
        }

        rules.Add(new(NewGuid(random), RuleType.MinRoomFill, false, DefaultSoftWeight, true, "Мінімальне заповнення аудиторій", new MinRoomFillParameters(DefaultMinFillRatio)));
        return rules;
    }

    private static SolverInput BuildSolverInput(
        GeneratorSettings settings,
        List<CenterRecord> centers,
        List<RoomRecord> rooms,
        List<ParticipantRecord> participants,
        List<RuleRecord> rules)
    {
        var costByCenter = centers.ToDictionary(x => x.Id, x => x.CostPerRoom);
        var solverRooms = rooms
            .Select(x => new SolverRoom(
                x.Id,
                x.CenterId,
                x.CostOverride ?? costByCenter[x.CenterId],
                new RoomLayoutModel(
                    x.Layout.Rows,
                    x.Layout.Columns,
                    x.Layout.SeatsPerDesk,
                    x.Layout.AislesAfterColumns,
                    x.Layout.DisabledSeats.ToHashSet(StringComparer.Ordinal),
                    x.Layout.AccessibleSeats.ToHashSet(StringComparer.Ordinal))))
            .ToList();

        var solverParticipants = participants
            .Select(x => new SolverParticipant(
                x.Id,
                new Dictionary<string, string> { [GroupKey] = x.Group, [SchoolKey] = x.School },
                x.NeedsAccessibleSeat,
                string.Join(' ', x.LastName, x.FirstName, x.MiddleName)))
            .ToList();

        var solverRules = rules.Where(x => x.IsEnabled).Select(x => new SolverRule(x.Id, x.Type, x.IsHard, x.Weight, x.Parameters)).ToList();
        var options = new SolveOptions { RandomSeed = settings.Seed };
        return new SolverInput(solverRooms, solverParticipants, solverRules, [], options);
    }

    /// <summary>Random-based (hence seed-deterministic) RFC 4122 version-4 GUID.</summary>
    private static Guid NewGuid(Random random)
    {
        var bytes = new byte[GuidByteLength];
        random.NextBytes(bytes);
        bytes[GuidVersionByte] = (byte)((bytes[GuidVersionByte] & GuidVersionMask) | GuidVersion4);
        bytes[GuidVariantByte] = (byte)((bytes[GuidVariantByte] & GuidVariantMask) | GuidVariantRfc4122);
        return new Guid(bytes);
    }

    private static string Format(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

    private sealed record LayoutTemplate(int Rows, int Columns, int SeatsPerDesk, IReadOnlyList<int> Aisles);
}