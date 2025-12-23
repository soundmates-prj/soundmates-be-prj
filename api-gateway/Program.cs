using Ocelot.DependencyInjection;
using Ocelot.Middleware;

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

// Register Ocelot services
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// IMPORTANT: UseCors MUST be called BEFORE UseOcelot
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// THIS LINE IS REQUIRED
await app.UseOcelot();

app.Run();
