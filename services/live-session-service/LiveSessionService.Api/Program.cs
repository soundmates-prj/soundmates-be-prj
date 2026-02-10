using LiveSessionService.Application;
using LiveSessionService.Infrastructure;
using LiveSessionService.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Web API (Controllers, JSON, Validation, Routing)
builder.AddWebApi();

// Add Swagger with JWT
builder.AddSwaggerWithJwt();

// Add CORS Policy
builder.AddCorsPolicy();

// Add Application Layer
builder.Services.AddApplication();

// Add Infrastructure Layer
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure HTTP Pipeline
app.UseHttpPipeline();

app.Run();
