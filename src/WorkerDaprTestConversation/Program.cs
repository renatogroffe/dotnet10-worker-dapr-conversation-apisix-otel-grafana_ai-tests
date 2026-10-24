using Dapr.AI.Conversation.Extensions;
using Grafana.OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WorkerConsumer.Tracing;
using WorkerDaprTestConversation;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

#pragma warning disable DAPR_CONVERSATION // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddDaprConversationClient();
#pragma warning restore DAPR_CONVERSATION // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var grafanaCloudZone = builder.Configuration["GrafanaCloud:Zone"];
var grafanaCloudInstanceId = builder.Configuration["GrafanaCloud:InstanceId"];
var grafanaCloudApiKey = builder.Configuration["GrafanaCloud:ApiKey"];
string? grafanaCloudAuthorization = null;
if (!string.IsNullOrWhiteSpace(grafanaCloudZone) &&
    !string.IsNullOrWhiteSpace(grafanaCloudInstanceId) && 
    !string.IsNullOrWhiteSpace(grafanaCloudApiKey))
{
    grafanaCloudAuthorization = Convert.ToBase64String(
        System.Text.Encoding.UTF8.GetBytes($"{grafanaCloudInstanceId}:{grafanaCloudApiKey}"));
}
OpenTelemetryExtensions.Initialize(builder.Configuration);
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: OpenTelemetryExtensions.ServiceName!,
        serviceVersion: OpenTelemetryExtensions.ServiceVersion!);
builder.Services.AddOpenTelemetry()
    .WithTracing((traceBuilder) =>
    {
        traceBuilder
            .AddSource(OpenTelemetryExtensions.ServiceName!)
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddGrpcClientInstrumentation()
            .UseGrafana(settings =>
            {
                if (!string.IsNullOrWhiteSpace(grafanaCloudAuthorization))
                {
                    settings.ExporterSettings = new OtlpExporter
                    {
                        Endpoint = new Uri($"https://otlp-gateway-prod-{grafanaCloudZone}.grafana.net/otlp"),
                        Headers = $"Authorization=Basic {grafanaCloudAuthorization}",
                        Protocol = OtlpExportProtocol.HttpProtobuf
                    };
                }
            });
    });
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AttachLogsToActivityEvent();
    options.UseGrafana(settings =>
    {
        if (!string.IsNullOrWhiteSpace(grafanaCloudAuthorization))
        {
            settings.ExporterSettings = new OtlpExporter
            {
                Endpoint = new Uri($"https://otlp-gateway-prod-{grafanaCloudZone}.grafana.net/otlp"),
                Headers = $"Authorization=Basic {grafanaCloudAuthorization}",
                Protocol = OtlpExportProtocol.HttpProtobuf
            };
        }
    });
});

var host = builder.Build();
host.Run();