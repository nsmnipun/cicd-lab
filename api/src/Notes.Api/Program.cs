using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Notes.Api.Notes;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logs to stdout only.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});

// OpenTelemetry. All exporter settings come from the standard OTEL_* environment variables.
var otel = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        // OTEL_SERVICE_NAME, when set, is picked up by the default resource detectors.
        if (string.IsNullOrWhiteSpace(builder.Configuration["OTEL_SERVICE_NAME"]))
        {
            resource.AddService("notes-api");
        }
    })
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("Microsoft.EntityFrameworkCore"))
    .WithLogging();

if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    otel.UseOtlpExporter();
}

builder.Services.AddDbContext<NotesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Notes")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotesDbContext>(tags: ["ready"]);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

if (args is ["migrate"])
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
    app.Logger.LogInformation("Applying database migrations");
    await db.Database.MigrateAsync();
    app.Logger.LogInformation("Database migrations applied");
    return 0;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapNoteEndpoints();

await app.RunAsync();
return 0;

public partial class Program;
