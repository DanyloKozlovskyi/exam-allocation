using System.Globalization;
using Solver.Model;

namespace Solver.Precheck;

/// <summary>Builds diagnostics with typed arguments formatted with the invariant culture.</summary>
internal static class DiagnosticFactory
{
    public const string ListSeparator = ",";

    public static Diagnostic Error(DiagnosticCode code, Guid? ruleId = null, params (DiagnosticArgument Argument, object Value)[] arguments)
        => Create(code, DiagnosticSeverity.Error, ruleId, arguments);

    public static Diagnostic Warning(DiagnosticCode code, Guid? ruleId = null, params (DiagnosticArgument Argument, object Value)[] arguments)
        => Create(code, DiagnosticSeverity.Warning, ruleId, arguments);

    public static Diagnostic Create(DiagnosticCode code, DiagnosticSeverity severity, Guid? ruleId, params (DiagnosticArgument Argument, object Value)[] arguments)
    {
        var values = new Dictionary<DiagnosticArgument, string>();
        foreach (var (argument, value) in arguments)
        {
            values[argument] = Format(value);
        }

        return new Diagnostic(code, severity, ruleId, values);
    }

    public static string FormatList<T>(IEnumerable<T> values)
        where T : notnull
        => string.Join(ListSeparator, values.Select(v => Format(v)));

    private static string Format(object value) => value switch
    {
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };
}