namespace Infrastructure.Tests.ImportExport;

public static class RepositoryPaths
{
    private const string SamplesFolder = "samples";
    private const string MarkerFile = "allocation.xsd";

    public static string Samples { get; } = FindSamples();

    public static string Sample(string fileName) => Path.Combine(Samples, fileName);

    private static string FindSamples()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, SamplesFolder);
            if (File.Exists(Path.Combine(candidate, MarkerFile)))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException($"'{SamplesFolder}/{MarkerFile}' not found above {AppContext.BaseDirectory}");
    }
}