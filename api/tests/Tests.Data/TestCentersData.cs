using Domain.Centers;
using Domain.Sessions;
using Domain.ValueObjects;

namespace Tests.Data;

public static class TestCentersData
{
    public const string MainCenterName = "Ліцей №1";
    public const string SecondCenterName = "Гімназія №2";
    public const string Address = "вул. Хрещатик, 1, Київ";
    public const double Latitude = 50.4501;
    public const double Longitude = 30.5234;

    public static TestCenter MainCenter(ExamSessionId sessionId)
        => TestCenter.New(TestCenterId.New(), sessionId, MainCenterName, Address, GeoPoint.From(Latitude, Longitude), null, TestCenterConstraints.DefaultCostPerRoom, true);

    public static TestCenter SecondCenter(ExamSessionId sessionId)
        => TestCenter.New(TestCenterId.New(), sessionId, SecondCenterName, Address, GeoPoint.From(Latitude, Longitude), null, TestCenterConstraints.DefaultCostPerRoom, true);
}