using Api.Constants;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace Api.Modules;

/// <summary>`dotnet run --project api/src/Api -- --export-openapi contracts/openapi.json` writes the Swagger document and exits (no database needed).</summary>
internal static class OpenApiExportModule
{
    public const string ExportArgument = "--export-openapi";

    internal static async Task<bool> TryExportOpenApi(this WebApplication app, string[] args)
    {
        var index = Array.IndexOf(args, ExportArgument);
        if (index < 0)
        {
            return false;
        }

        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{ExportArgument} requires an output path");
        }

        var path = Path.GetFullPath(args[index + 1]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var document = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger(SwaggerDocument.Name);
        await using var stream = File.Create(path);
        await using var writer = new StreamWriter(stream);
        document.SerializeAsV3(new OpenApiJsonWriter(writer));
        await writer.FlushAsync();
        return true;
    }
}