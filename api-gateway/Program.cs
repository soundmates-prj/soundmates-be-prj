using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

static void AddOrigin(HashSet<string> origins, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return;

    var trimmed = value.Trim().TrimEnd('/');
    if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        return;

    // CORS WithOrigins accepts scheme + host + optional port only.
    origins.Add(uri.GetLeftPart(UriPartial.Authority));
}

static void AddOriginsFromCsv(HashSet<string> origins, string? csv)
{
    if (string.IsNullOrWhiteSpace(csv))
        return;

    foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        AddOrigin(origins, part);
    }
}

var allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

// Keep localhost defaults for local development.
AddOrigin(allowedOrigins, "http://localhost:3000");
AddOrigin(allowedOrigins, "http://localhost:5173");

// Support production values from environment/config.
AddOrigin(allowedOrigins, builder.Configuration["FRONTEND_URL"]);
AddOriginsFromCsv(allowedOrigins, builder.Configuration["CORS_ALLOWED_ORIGINS"]);

Console.WriteLine($"[Gateway] Allowed CORS origins: {string.Join(", ", allowedOrigins)}");

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Register Ocelot + Polly (circuit breaker & timeout per route via QoSOptions in ocelot.json)
builder.Services.AddOcelot(builder.Configuration)
                .AddPolly();

var app = builder.Build();

// IMPORTANT: UseCors MUST be called BEFORE UseOcelot
app.UseCors("AllowFrontend");

// IMPORTANT: keep gateway on plain HTTP in local Docker setup.
// Enabling HTTPS redirection here can cause browser requests to hang/pending
// when no HTTPS endpoint/certificate is configured for the gateway container.
app.UseWebSockets();

// THIS LINE IS REQUIRED
await app.UseOcelot();

app.Run();
