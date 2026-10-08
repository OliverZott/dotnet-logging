using dotnet_logging_sample1.Infrastructure;
using Microsoft.AspNetCore.HttpLogging;
using dotnet_logging_sample1.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Logging: enable trace/span correlation so every log line can be tied back
// to the request/activity that produced it (this is what Seq / the Aspire
// dashboard / any OTLP backend use to correlate logs with traces later).
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.ParentId;
});

if (builder.Environment.IsDevelopment())
{
    // Human-readable console output while developing. Scopes (trace/span id,
    // request path, etc.) are still attached to each log entry internally -
    // they're just not printed here to keep the console readable. They *do*
    // show up when exporting to Seq/Aspire/OTLP, where they become
    // separate, filterable/searchable fields instead of inline text.
    builder.Logging.AddSimpleConsole(options =>
    {
        options.IncludeScopes = false;
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss.fff ";
    });
}
else
{
    // Structured JSON output in non-dev environments: easy to ship to
    // Seq/Aspire dashboard/any log aggregator without a custom parser.
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.UseUtcTimestamp = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    });
}

// OpenTelemetry: ship logs/traces/metrics over OTLP to whatever backend is
// configured via the standard OTEL_EXPORTER_OTLP_ENDPOINT env var (e.g. the
// Aspire dashboard on :4317, or Seq's OTLP ingestion endpoint). Console
// logging above keeps working independently - this is an additional sink.
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: builder.Environment.ApplicationName);

// Second OTLP target: the Aspire dashboard (gRPC). The parameterless
// AddOtlpExporter() calls below keep using the OTEL_EXPORTER_OTLP_* env vars (Seq).
var aspireEndpoint = new Uri(builder.Configuration["ASPIRE_OTLP_ENDPOINT"] ?? "http://localhost:4317");
void ConfigureAspire(OpenTelemetry.Exporter.OtlpExporterOptions o)
{
    o.Endpoint = aspireEndpoint;
    o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
}

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter();
    options.AddOtlpExporter(ConfigureAspire);
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(resourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter()
        .AddOtlpExporter(ConfigureAspire))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(resourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter()
        .AddOtlpExporter(ConfigureAspire));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IWeatherService, WeatherService>();

// One structured log entry per request (method, path, status, duration).
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// Last-resort logging for exceptions outside the HTTP pipeline (background
// threads, unobserved tasks). These can't be recovered - only recorded.
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    app.Logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception, process terminating: {IsTerminating}", e.IsTerminating);
    // Process dies right after this - flush batched OTLP logs first.
    app.Services.GetService<LoggerProvider>()?.ForceFlush(5000);
};
TaskScheduler.UnobservedTaskException += (_, e) =>
    app.Logger.LogError(e.Exception, "Unobserved task exception");

app.UseHttpLogging();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "dotnet-logging-sample1 v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();