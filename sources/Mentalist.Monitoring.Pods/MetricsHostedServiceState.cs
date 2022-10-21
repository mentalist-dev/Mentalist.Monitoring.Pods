namespace Mentalist.Monitoring.Pods;

public static class MetricsHostedServiceState
{
    private static DateTime _lastPing = DateTime.UtcNow;

    public static void Ping()
    {
        _lastPing = DateTime.UtcNow;
    }

    public static bool IsAlive()
    {
        var duration = DateTime.UtcNow - _lastPing;
        return duration < TimeSpan.FromMinutes(1);
    }

    public static void Fail()
    {
        _lastPing = DateTime.MinValue;
    }
}