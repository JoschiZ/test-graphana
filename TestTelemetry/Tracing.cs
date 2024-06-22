using System.Diagnostics;

namespace TestTelemetry;

internal static class Tracing
{
    public const string ActivitySourceName = "TestTelemetry";
    public static ActivitySource ActivitySource { get; } = new ActivitySource(ActivitySourceName);
}