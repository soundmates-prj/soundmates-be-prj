using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
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
