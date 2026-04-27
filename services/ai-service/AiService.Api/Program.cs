using AiService.Api.Extensions;

// ============================================================
// CLEAN ARCHITECTURE - COMPOSITION ROOT
// ============================================================

var builder = WebApplication.CreateBuilder(args);

builder.AddEnvironmentConfig();
builder.AddWebApi();
builder.AddSwaggerWithJwt();
builder.AddAuth();
builder.AddCorsPolicy();

var app = builder.Build();

await app.ApplyMigrationsAsync();
await app.SeedDataAsync();

app.UseHttpPipeline();

app.Run();
