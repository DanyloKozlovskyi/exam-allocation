namespace Infrastructure.ImportExport.Constants;

public static class XmlNames
{
    public const string Namespace = "urn:exam-allocation:v1";
    public const string Allocation = "allocation";
    public const string Objective = "objective";
    public const string Center = "center";
    public const string Room = "room";
    public const string Seat = "seat";
    public const string Participant = "participant";

    public static class Attributes
    {
        public const string Session = "session";
        public const string ExamDate = "examDate";
        public const string GeneratedAt = "generatedAt";
        public const string Index = "index";
        public const string RoomsUsed = "roomsUsed";
        public const string Cost = "cost";
        public const string SoftPenalty = "softPenalty";
        public const string Id = "id";
        public const string Name = "name";
        public const string Address = "address";
        public const string Latitude = "lat";
        public const string Longitude = "lng";
        public const string Capacity = "capacity";
        public const string Filled = "filled";
        public const string Key = "key";
        public const string Row = "row";
        public const string Desk = "desk";
        public const string Place = "place";
        public const string ExternalId = "externalId";
        public const string LastName = "lastName";
        public const string FirstName = "firstName";
        public const string MiddleName = "middleName";
        public const string Group = "group";
        public const string School = "school";
    }

    public static class Formats
    {
        public const string Date = "yyyy-MM-dd";
        public const string DateTime = "o";
        public const string Cost = "0.00";
        public const string Coordinate = "0.######";
    }
}