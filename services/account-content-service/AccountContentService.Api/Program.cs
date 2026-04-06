using AccountContentService.Api.Extensions;
using AccountContentService.Api.Middleware;
using AccountContentService.Api.Swagger;
using AccountContentService.Application.DependencyInjection;
using AccountContentService.Infrastructure.Extensions;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.AddEnvironmentConfig();


// Controllers
builder.Services.AddControllers();


// Swagger
builder.Services.AddSwaggerDocs();


// Application Layer
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);



// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();


// DbContext
builder.Services.AddDbContext<AccountContentDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    options.UseNpgsql(connectionString);

    // Suppress PendingModelChangesWarning — migration will apply at next rebuild with new migration files
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// PayOS HttpClient (ApiGateway HttpClient removed — user profiles now via local RabbitMQ projection)
IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient("PayOS", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
})
.AddPolicyHandler(CreateCircuitBreakerPolicy());

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

await app.ApplyMigrationsAsync();
await app.SeedDataAsync();


// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();


// COMMENTED OUT — HttpsRedirection causes requests to hang when the gateway proxies over plain HTTP.
// When the gateway runs on plain HTTP (no TLS), redirecting to HTTPS here breaks the connection.
// If you need HTTPS in production, configure TLS at the container/reverse-proxy level instead.
// app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();


app.MapControllers();

//app.MapHealthChecks("/health");


app.Run();
