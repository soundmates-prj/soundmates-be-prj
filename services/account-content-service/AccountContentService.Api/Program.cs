using AccountContentService.Api.Extensions;
using AccountContentService.Api.Middleware;
using AccountContentService.Api.Swagger;
using AccountContentService.Application.DependencyInjection;
using AccountContentService.Infrastructure.Extensions;
using AccountContentService.Infrastructure.Persistence;

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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

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


// AUTO MIGRATION
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountContentDbContext>();

    dbContext.Database.Migrate();

    await DataSeeder.SeedAsync(dbContext);
}


// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();


app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();


app.MapControllers();

//app.MapHealthChecks("/health");


app.Run();
