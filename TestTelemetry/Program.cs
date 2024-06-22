using Microsoft.AspNetCore.HttpLogging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using TestTelemetry;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.AddHttpLogging(x =>
{
    //x.CombineLogs = true;
    x.LoggingFields = HttpLoggingFields.All;
});

builder.Services.AddSerilog((serviceProvider, loggerConfig) =>
{
    loggerConfig
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(x =>
        {
            x.Endpoint = "http://otel:4317"; 
            x.Protocol = OtlpProtocol.Grpc;
            x.IncludedData = IncludedData.SpanIdField | IncludedData.TraceIdField;
            x.ResourceAttributes = new Dictionary<string, object>()
            {
                ["service.name"] = "otelapp",
                ["service.instance.id"] = "instance1"
            };
        })
        //.WriteTo.GrafanaLoki("http://loki:3100", new LokiLabel[] {new() {Key = "App", Value = "TestApp"}})
        ;
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(x =>
    {
        x.AddAspNetCoreInstrumentation();
        x.AddHttpClientInstrumentation();
        x.AddRuntimeInstrumentation();
        x.AddProcessInstrumentation();
        
        x.AddOtlpExporter(((options, readerOptions) =>
        {
            options.Protocol = OtlpExportProtocol.Grpc;
            options.Endpoint = new Uri("http://otel:4317");
            
            readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
        }));
        x.ConfigureResource(resourceBuilder =>
        {
            resourceBuilder.AddAttributes(new KeyValuePair<string, object>[]
            {
                new("service.name", "otelapp"),
                new("service.instance.id", "instance1"),
            });
        });
        x.AddPrometheusExporter();
    })
    .WithTracing(x =>
    {
        x.AddAspNetCoreInstrumentation();
        x.AddHttpClientInstrumentation();
        x.AddSource(Tracing.ActivitySource.Name);
        x.AddOtlpExporter(((options) =>
        {
            options.Protocol = OtlpExportProtocol.Grpc;
            options.Endpoint = new Uri("http://otel:4317");
        }));
        x.ConfigureResource(resourceBuilder =>
        {
            resourceBuilder.AddAttributes(new KeyValuePair<string, object>[]
            {
                new("service.name", "otelapp"),
                new("service.instance.id", "instance1"),
            });
        });
    });

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpLogging();

app.UseAuthorization();

app.MapControllers();

app.Run();