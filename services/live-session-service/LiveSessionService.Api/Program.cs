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
{
    Console.WriteLine($"[INFO] Loading .env from: {envFile}");
    DotNetEnv.Env.Load(envFile);
    
    // Debug: Print loaded environment variables
    Console.WriteLine("[DEBUG] Environment Variables:");
    Console.WriteLine($"  POSTGRES_HOST: {Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "(not set)"}");
    Console.WriteLine($"  POSTGRES_DATABASE: {Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ?? "(not set)"}");
    Console.WriteLine($"  AZURACAST_BASE_URL: {Environment.GetEnvironmentVariable("AZURACAST_BASE_URL") ?? "(not set)"}");
    Console.WriteLine($"  JWT_KEY: {(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_KEY")) ? "(not set)" : "***SET***")}");
    Console.WriteLine($"  RABBITMQ_HOST: {Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "(not set)"}");
}
else
{
    Console.WriteLine("[WARNING] No .env file found. Relying on appsettings.json or environment variables.");
}

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

// Auto-apply pending EF Core migrations on startup
await app.MigrateDatabaseAsync();

// Configure HTTP Pipeline
app.UseHttpPipeline();

app.Run();
