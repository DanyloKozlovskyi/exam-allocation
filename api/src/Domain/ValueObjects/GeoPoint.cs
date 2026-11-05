using Domain.Common;

namespace Domain.ValueObjects;

public sealed class GeoPoint : ValueObject
{
    public const double MinLatitude = -90;
    public const double MaxLatitude = 90;
    public const double MinLongitude = -180;
    public const double MaxLongitude = 180;

    private GeoPoint(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public static GeoPoint From(double latitude, double longitude)
    {
        Guard.InRange(latitude, MinLatitude, MaxLatitude, nameof(latitude));
        Guard.InRange(longitude, MinLongitude, MaxLongitude, nameof(longitude));
        return new GeoPoint(latitude, longitude);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}