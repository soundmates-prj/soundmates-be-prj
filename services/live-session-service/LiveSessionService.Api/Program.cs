using LiveSessionService.Application;
using LiveSessionService.Infrastructure;
using LiveSessionService.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddEnvironmentConfig();

// Add Web API (Controllers, JSON, Validation, Routing)
builder.AddWebApi();
builder.AddSwaggerWithJwt();
builder.AddJwtAuthentication();
builder.AddCorsPolicy();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.MigrateDatabaseAsync();

// Configure HTTP request pipeline
app.UseHttpPipeline();

app.Run();
