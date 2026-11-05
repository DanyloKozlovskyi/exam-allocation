namespace Api.Constants;

public static class HubRoutes
{
    public const string SolveProgress = "/hubs/solve-progress";
}

public static class SolveProgressHubMethods
{
    public const string SubscribeToRun = nameof(SubscribeToRun);
    public const string UnsubscribeFromRun = nameof(UnsubscribeFromRun);
    public const string ProgressUpdated = nameof(ProgressUpdated);
    public const string RunCompleted = nameof(RunCompleted);
}

public static class HubGroups
{
    private const string RunPrefix = "run:";

    public static string Run(Guid runId) => RunPrefix + runId;
}