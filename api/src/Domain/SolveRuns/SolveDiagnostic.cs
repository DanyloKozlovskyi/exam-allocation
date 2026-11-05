using Domain.Rules;

namespace Domain.SolveRuns;

public sealed record SolveDiagnostic(
    DiagnosticCode Code,
    DiagnosticSeverity Severity,
    RuleId? RuleId,
    IReadOnlyDictionary<DiagnosticArgument, string> Arguments);