using AuthService.Api.Extensions;

// ============================================================
// CLEAN ARCHITECTURE - COMPOSITION ROOT
// Program.cs acts as Composition Root - orchestration only
// All implementation details are in Extensions folder
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.AddEnvironmentConfig();    // Load .env and map environment variables
builder.AddWebApi();                // Controllers, JSON, validation, routing
builder.AddSwaggerWithJwt();        // Swagger with JWT Bearer auth
builder.AddAuth();                  // JWT authentication + Application/Infrastructure layers
builder.AddCorsPolicy();            // CORS configuration

var app = builder.Build();

// Database migration and seeding (must complete before app starts)
await app.ApplyMigrationsAsync();   // Apply EF Core migrations with retry logic
await app.SeedDataAsync();          // Seed default roles and data

// Configure HTTP request pipeline
app.UseHttpPipeline();              // Middleware, error handling, routing

app.Run();
