using Domain.Common;
using Domain.Sessions;

namespace Domain.SolveRuns;

public class SolveRun : IEntity<SolveRunId>, IVersioned
{
    private SolveRun(SolveRunId id, ExamSessionId sessionId, SolveRunOptions options, string inputHash, DateTimeOffset createdAt)
    {
        Id = id;
        SessionId = sessionId;
        Options = options;
        InputHash = inputHash;
        CreatedAt = createdAt;
        Status = SolveRunStatus.Queued;
    }

    private SolveRun()
    {
        Id = SolveRunId.Empty();
        SessionId = ExamSessionId.Empty();
        Options = new SolveRunOptions();
        InputHash = string.Empty;
    }

    public SolveRunId Id { get; private set; }

    public ExamSessionId SessionId { get; private set; }

    public SolveRunStatus Status { get; private set; }

    public SolveRunOptions Options { get; private set; }

    public string InputHash { get; private set; }

    public List<SolveDiagnostic> Diagnostics { get; private set; } = [];

    public SolveRunStats? Stats { get; private set; }

    public string? ErrorMessage { get; private set; }

    /// <summary>Set only for <see cref="SolveRunStatus.Failed"/>.</summary>
    public SolveRunFailureReason? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    /// <summary>xmin: the executor and cancel must not overwrite each other's status change.</summary>
    public uint Version { get; private set; }

    public bool IsActive => Status is SolveRunStatus.Queued or SolveRunStatus.Running;

    public static SolveRun New(SolveRunId id, ExamSessionId sessionId, SolveRunOptions options, string inputHash, DateTimeOffset now)
    {
        Guard.NotNull(options, nameof(options));
        Guard.NotNullOrEmptyOrWhiteSpace(inputHash, nameof(inputHash));
        Guard.MaxLength(inputHash, SolveRunConstraints.InputHashLength, nameof(inputHash));
        return new SolveRun(id, sessionId, options, inputHash, now);
    }

    public void MarkRunning(DateTimeOffset now)
    {
        Guard.IsTrue(Status == SolveRunStatus.Queued, nameof(Status));
        Status = SolveRunStatus.Running;
        StartedAt = now;
    }

    /// <summary>Starts the run with the fingerprint of the inputs it will actually solve (taken when execution begins).</summary>
    public void MarkRunning(DateTimeOffset now, string inputHash)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(inputHash, nameof(inputHash));
        Guard.MaxLength(inputHash, SolveRunConstraints.InputHashLength, nameof(inputHash));
        MarkRunning(now);
        InputHash = inputHash;
    }

    public void Complete(
        SolveRunStatus status,
        IReadOnlyList<SolveDiagnostic> diagnostics,
        SolveRunStats? stats,
        string? errorMessage,
        DateTimeOffset now,
        SolveRunFailureReason? failureReason = null)
    {
        Guard.IsTrue(IsActive, nameof(Status));
        Guard.IsTrue(status is not (SolveRunStatus.Queued or SolveRunStatus.Running), nameof(status));
        Status = status;
        Diagnostics = diagnostics.ToList();
        Stats = stats;
        ErrorMessage = errorMessage is null ? null : errorMessage[..Math.Min(errorMessage.Length, SolveRunConstraints.ErrorMessageMaxLength)];
        FinishedAt = now;
        FailureReason = status == SolveRunStatus.Failed ? failureReason ?? SolveRunFailureReason.SolverError : null;
    }
}