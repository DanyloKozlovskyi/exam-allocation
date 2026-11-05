using Api.Constants;
using Api.Hubs;
using Api.Modules;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.SetupServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();
if (await app.TryExportOpenApi(args))
{
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.InitializeDb();
await app.SeedData();
app.UseExceptionHandler();
app.UseCors();
app.UseRouting();
app.MapControllers();
app.MapHub<SolveProgressHub>(HubRoutes.SolveProgress);
app.MapApiEndpoints();

app.Run();

public partial class Program
{
}