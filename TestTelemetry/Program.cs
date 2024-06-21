using Microsoft.AspNetCore.HttpLogging;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Sinks.OpenTelemetry;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.AddHttpLogging(x =>
{
    x.CombineLogs = true;
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
        x.AddMeter(
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel");
        x.AddPrometheusExporter();
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