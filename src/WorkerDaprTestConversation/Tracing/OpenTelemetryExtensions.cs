using System.Diagnostics;

namespace WorkerConsumer.Tracing;

public static class OpenTelemetryExtensions
{
    public static string? ServiceName { get; private set; }
    public static string? ServiceVersion { get; private set; }
    public static ActivitySource? ActivitySource { get; private set; }

    public static void Initialize(IConfiguration configuration)
    {
        ServiceName = configuration["OTEL_SERVICE_NAME"]!;
        ServiceVersion = typeof(OpenTelemetryExtensions).Assembly.GetName().Version!.ToString();
        ActivitySource = new ActivitySource(ServiceName, ServiceVersion);
    }
}