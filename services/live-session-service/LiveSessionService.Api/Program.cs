using LiveSessionService.Application;
using LiveSessionService.Infrastructure;
using LiveSessionService.Api.Extensions;

// Load .env — search from the API project directory, then CWD, then the binary output dir.
// In production, skip the file and rely on real environment variables.
var envCandidates = new[]
{
    Path.Combine(AppContext.BaseDirectory, "../../..", ".env"),  // bin/Debug/net10.0 -> project root
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),        // CWD (dotnet run from project dir)
    Path.Combine(AppContext.BaseDirectory, ".env"),               // published output dir
};
var envFile = envCandidates.FirstOrDefault(File.Exists);
if (envFile is not null)
    DotNetEnv.Env.Load(envFile);

var builder = WebApplication.CreateBuilder(args);

// Add Web API (Controllers, JSON, Validation, Routing)
builder.AddWebApi();

// Add Swagger with JWT
builder.AddSwaggerWithJwt();

// Add JWT Authentication
builder.AddJwtAuthentication();

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
