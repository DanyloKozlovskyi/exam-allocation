using Application.Centers.Commands;
using Domain.Centers;

namespace Api.Dtos;

public record TestCenterDto(
    Guid? Id,
    Guid? SessionId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    string? GooglePlaceId,
    decimal? CostPerRoom,
    bool IsActive,
    int RoomCount = 0,
    int Capacity = 0,
    uint? Version = null)
{
    public static TestCenterDto FromDomain(TestCenter center) => new(
        center.Id.Value,
        center.SessionId.Value,
        center.Name,
        center.Address,
        center.Location.Latitude,
        center.Location.Longitude,
        center.GooglePlaceId,
        center.CostPerRoom,
        center.IsActive,
        center.Rooms.Count,
        center.Rooms.Sum(x => x.Capacity),
        center.Version);

    public CreateTestCenterCommand ToCreateCommand(Guid sessionId) => new()
    {
        SessionId = sessionId,
        Name = Name,
        Address = Address,
        Latitude = Latitude,
        Longitude = Longitude,
        GooglePlaceId = GooglePlaceId,
        CostPerRoom = CostPerRoom,
        IsActive = IsActive,
    };

    public UpdateTestCenterCommand ToUpdateCommand(Guid sessionId) => new()
    {
        SessionId = sessionId,
        Id = Id ?? Guid.Empty,
        Name = Name,
        Address = Address,
        Latitude = Latitude,
        Longitude = Longitude,
        GooglePlaceId = GooglePlaceId,
        CostPerRoom = CostPerRoom,
        IsActive = IsActive,
        Version = Version ?? default,
    };
}