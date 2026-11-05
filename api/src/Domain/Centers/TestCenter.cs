using Domain.Common;
using Domain.Rooms;
using Domain.Sessions;
using Domain.ValueObjects;

namespace Domain.Centers;

public class TestCenter : IEntity<TestCenterId>, IVersioned
{
    private TestCenter(
        TestCenterId id,
        ExamSessionId sessionId,
        string name,
        string address,
        GeoPoint location,
        string? googlePlaceId,
        decimal costPerRoom,
        bool isActive)
    {
        Id = id;
        SessionId = sessionId;
        Name = name;
        Address = address;
        Location = location;
        GooglePlaceId = googlePlaceId;
        CostPerRoom = costPerRoom;
        IsActive = isActive;
    }

    private TestCenter()
    {
        Id = TestCenterId.Empty();
        SessionId = ExamSessionId.Empty();
        Name = string.Empty;
        Address = string.Empty;
        Location = GeoPoint.From(0, 0);
    }

    public TestCenterId Id { get; private set; }

    public ExamSessionId SessionId { get; private set; }

    public string Name { get; private set; }

    public string Address { get; private set; }

    public GeoPoint Location { get; private set; }

    public string? GooglePlaceId { get; private set; }

    public decimal CostPerRoom { get; private set; }

    public bool IsActive { get; private set; }

    public uint Version { get; private set; }

    public ICollection<Room> Rooms { get; private set; } = [];

    public static TestCenter New(
        TestCenterId id,
        ExamSessionId sessionId,
        string name,
        string address,
        GeoPoint location,
        string? googlePlaceId,
        decimal costPerRoom,
        bool isActive)
    {
        Validate(name, address, googlePlaceId, costPerRoom);
        return new TestCenter(id, sessionId, name.Trim(), address.Trim(), location, googlePlaceId, costPerRoom, isActive);
    }

    public void UpdateDetails(string name, string address, GeoPoint location, string? googlePlaceId, decimal costPerRoom, bool isActive)
    {
        Validate(name, address, googlePlaceId, costPerRoom);
        Name = name.Trim();
        Address = address.Trim();
        Location = location;
        GooglePlaceId = googlePlaceId;
        CostPerRoom = costPerRoom;
        IsActive = isActive;
    }

    private static void Validate(string name, string address, string? googlePlaceId, decimal costPerRoom)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Guard.MaxLength(name, TestCenterConstraints.NameMaxLength, nameof(name));
        Guard.MaxLength(address, TestCenterConstraints.AddressMaxLength, nameof(address));
        Guard.MaxLength(googlePlaceId, TestCenterConstraints.GooglePlaceIdMaxLength, nameof(googlePlaceId));
        Guard.GreaterThanOrEqual(costPerRoom, TestCenterConstraints.MinCostPerRoom, nameof(costPerRoom));
    }
}