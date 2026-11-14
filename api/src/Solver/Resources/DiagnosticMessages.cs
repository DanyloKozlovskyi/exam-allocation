using System.Globalization;
using System.Resources;
using Solver.Model;

namespace Solver.Resources;

/// <summary>Renders Ukrainian diagnostic texts; resource names equal <see cref="DiagnosticCode"/> member names.</summary>
/// <remarks>An argument value that is an enum member name (e.g. <c>Reason</c>) is translated when a resource
/// <c>{Argument}_{Value}</c> exists.</remarks>
public static class DiagnosticMessages
{
    private const string ValueResourceSeparator = "_";

    private static readonly ResourceManager ResourceManager = new(typeof(DiagnosticMessages).Namespace + ".Diagnostics", typeof(DiagnosticMessages).Assembly);

    public static string Render(Diagnostic diagnostic)
    {
        var template = ResourceManager.GetString(diagnostic.Code.ToString(), CultureInfo.InvariantCulture) ?? diagnostic.Code.ToString();
        foreach (var (argument, value) in diagnostic.Arguments)
        {
            var text = ResourceManager.GetString(argument + ValueResourceSeparator + value, CultureInfo.InvariantCulture) ?? value;
            template = template.Replace("{" + argument + "}", text, StringComparison.Ordinal);
        }

        return template;
    }
}