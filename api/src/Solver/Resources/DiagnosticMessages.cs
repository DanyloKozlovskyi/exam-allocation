using System.Globalization;
using System.Resources;
using Solver.Model;

namespace Solver.Resources;

/// <summary>Renders Ukrainian diagnostic texts; resource names equal <see cref="DiagnosticCode"/> member names.</summary>
public static class DiagnosticMessages
{
    private static readonly ResourceManager ResourceManager = new(typeof(DiagnosticMessages).Namespace + ".Diagnostics", typeof(DiagnosticMessages).Assembly);

    public static string Render(Diagnostic diagnostic)
    {
        var template = ResourceManager.GetString(diagnostic.Code.ToString(), CultureInfo.InvariantCulture) ?? diagnostic.Code.ToString();
        foreach (var (argument, value) in diagnostic.Arguments)
        {
            template = template.Replace("{" + argument + "}", value, StringComparison.Ordinal);
        }

        return template;
    }
}