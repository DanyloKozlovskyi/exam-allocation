using Api.Dtos;
using Application.Common.Errors;
using Application.Imports.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Solver.Model;
using Swashbuckle.AspNetCore.SwaggerGen;
using DiagnosticArgument = Domain.SolveRuns.DiagnosticArgument;

namespace Api.Modules.Swagger;

/// <summary>Adds contract types that no action exposes directly (SignalR payloads, error codes, dictionary-key enums) so the client can generate them.</summary>
internal sealed class ContractSchemasDocumentFilter : IDocumentFilter
{
    private static readonly Type[] AdditionalTypes =
    [
        typeof(ErrorCode),
        typeof(SolveStage),
        typeof(ParticipantField),
        typeof(DiagnosticArgument),
        typeof(SolveProgressDto),
        typeof(ProblemDetails),
        typeof(ValidationProblemDetails),
    ];

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var type in AdditionalTypes)
        {
            context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
        }
    }
}