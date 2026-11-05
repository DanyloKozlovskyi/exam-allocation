namespace Api.Constants;

/// <summary>All HTTP route templates (pal-suite style: create / edit action segments).</summary>
public static class ApiRoutes
{
    public const string Health = "/health";

    public static class Sessions
    {
        public const string List = "sessions";
        public const string ById = List + "/{sessionId:guid}";
        public const string Totals = ById + "/totals";
        public const string Create = List + "/create";
        public const string Edit = List + "/edit";
    }

    public static class Centers
    {
        public const string List = Sessions.ById + "/centers";
        public const string ById = List + "/{centerId:guid}";
        public const string Create = List + "/create";
        public const string Edit = List + "/edit";
    }

    public static class Rooms
    {
        public const string List = "centers/{centerId:guid}/rooms";
        public const string ById = List + "/{roomId:guid}";
        public const string Create = List + "/create";
        public const string CreateBulk = List + "/create-bulk";
        public const string Edit = List + "/edit";
        public const string PreviewLayout = "rooms/preview-layout";
    }

    public static class Participants
    {
        public const string List = Sessions.ById + "/participants";
        public const string ById = List + "/{participantId:guid}";
        public const string Create = List + "/create";
        public const string Edit = List + "/edit";
        public const string DeleteAll = List + "/delete-all";
        public const string Attributes = Sessions.ById + "/attributes";
    }

    public static class Rules
    {
        public const string List = Sessions.ById + "/rules";
        public const string ById = List + "/{ruleId:guid}";
        public const string Create = List + "/create";
        public const string Edit = List + "/edit";
        public const string RuleTypes = "rule-types";
    }

    public static class Pins
    {
        public const string List = Sessions.ById + "/pins";
        public const string Create = List + "/create";
        public const string ByParticipant = List + "/{participantId:guid}";
        public const string PinAll = Allocations.ById + "/pin-all";
    }

    public static class Imports
    {
        public const string Upload = Sessions.ById + "/imports/upload";
        public const string ById = "imports/{importId:guid}";
        public const string Validate = ById + "/validate";
        public const string Commit = ById + "/commit";
        public const string Template = "imports/template";
    }

    public static class SolveRuns
    {
        public const string List = Sessions.ById + "/solve-runs";
        public const string Create = List + "/create";
        public const string ById = "solve-runs/{runId:guid}";
        public const string Cancel = ById + "/cancel";
        public const string Precheck = Sessions.ById + "/precheck";
    }

    public static class Allocations
    {
        public const string ByRun = SolveRuns.ById + "/allocations";
        public const string ById = "allocations/{allocationId:guid}";
        public const string RoomSeating = ById + "/rooms/{roomId:guid}";
        public const string Participants = ById + "/participants";
        public const string ValidateMoves = ById + "/validate-moves";
        public const string ApplyMoves = ById + "/apply-moves";
    }

    public static class Exports
    {
        public const string Export = Allocations.ById + "/export";
    }
}