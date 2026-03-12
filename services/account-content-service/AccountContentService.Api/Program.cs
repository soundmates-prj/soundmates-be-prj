using AccountContentService.Api.Extensions;
using AccountContentService.Api.Middleware;
using AccountContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddEnvironmentConfig();

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add DbContext
builder.Services.AddDbContext<AccountContentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// Configure services
builder.AddEnvironmentConfig();    // Load .env and map environment 
//builder.AddWebApi();                // Controllers, JSON, validation, routing
//builder.AddSwaggerWithJwt();        // Swagger with JWT Bearer auth
//builder.AddAuth();                  // JWT authentication + Application/Infrastructure layers
//builder.AddCorsPolicy();            // CORS configuration

var app = builder.Build();

//builder.Services
//    .AddApplicationServices()
//    .AddInfrastructureServices(builder.Configuration)
//    .AddJwtAuthentication(builder.Configuration)
//    .AddSwaggerDocs()
//    .AddHealthChecks();

// AUTO MIGRATION (tạo DB + apply migration khi container start)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountContentDbContext>();

    dbContext.Database.Migrate();

    await DataSeeder.SeedAsync(dbContext);
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();