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

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter();
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(resourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(resourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IWeatherService, WeatherService>();

var app = builder.Build();

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