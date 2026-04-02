using LiveSessionService.Application;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Infrastructure;
using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.AddEnvironmentConfig();

// Add Web API (Controllers, JSON, Validation, Routing)
builder.AddWebApi();
builder.AddSwaggerWithJwt();
builder.AddJwtAuthentication();
builder.AddCorsPolicy();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR notifier for broadcasting session lifecycle events
builder.Services.AddScoped<ILiveSessionNotifier, LiveSessionNotifier>();

var app = builder.Build();

await app.ApplyMigrationsAsync();

// Configure HTTP request pipeline
app.UseHttpPipeline();

app.Run();
