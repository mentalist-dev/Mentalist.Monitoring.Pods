using k8s;
using Mentalist.Monitoring.Pods.Models;
using Prometheus;

namespace Mentalist.Monitoring.Pods;

public class MonitoringHostedService: IHostedService
{
    private readonly Gauge _containersDuration =
        Metrics.CreateGauge(
            "container_started_since_duration_seconds",
            "POD running duration in seconds",
            "namespace", "container", "pod", "image", "version"
        );

    private readonly ILogger<MonitoringHostedService> _logger;
    private readonly CancellationTokenSource _cts = new();

    public MonitoringHostedService(ILogger<MonitoringHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(
            () => MonitorAsync(_cts.Token),
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Current
        );
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }

    /*
    private async Task MonitorTestAsync(CancellationToken cancellationToken)
    {
        var counter = 0;
        var version = 0;
        var timer = Stopwatch.StartNew();

        var published = new Dictionary<string, Gauge.Child>();

        while (!cancellationToken.IsCancellationRequested)
        {
            counter += 1;

            List<KeyValuePair<string, Gauge.Child>> old;
            if (counter % 10 == 0)
            {
                version += 1;
                old = published.ToList();
            }
            else
            {
                old = new List<KeyValuePair<string, Gauge.Child>>();
            }

            var key = $"{version}";

            if (!published.TryGetValue(key, out var gauge))
            {
                gauge = _containersDuration.Labels("dev", "test", "test", "pod image", $"1.0.{version}");
                published[key] = gauge;
            }

            gauge.Set(timer.Elapsed.TotalSeconds);

            foreach (var child in old)
            {
                child.Value.Unpublish();
                published.Remove(child.Key);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
        }
    }
    */

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = KubernetesClientConfiguration.BuildDefaultConfig();
            var client = new Kubernetes(config);

            var published = new Dictionary<string, Gauge.Child>();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    MetricsHostedServiceState.Ping();

                    var count = 0;
                    var result = new List<Namespace>();

                    var namespaces = await client.ListNamespaceAsync(cancellationToken: cancellationToken);
                    foreach (var ns in namespaces)
                    {
                        var namespaceName = ns.Metadata.Name;
                        var containers = await GetContainersAsync(client, namespaceName);
                        result.Add(new Namespace(namespaceName, containers));
                        count += containers.Count;
                    }

                    Namespaces.All = new NamespaceList(result);

                    var latestKeys = new HashSet<string>();
                    foreach (var ns in result)
                    {
                        foreach (var pair in ns.Containers)
                        {
                            var now = DateTime.UtcNow;
                            foreach (var container in pair.Value)
                            {
                                var startTime = container.StartTime.GetValueOrDefault(now).ToUniversalTime();
                                var duration = now - startTime;

                                var key = $"{ns.Name}.{container.Name}.{container.Pod}.{container.Image}.{container.Version}";
                                latestKeys.Add(key);

                                if (!published.TryGetValue(key, out var gauge))
                                {
                                    gauge = _containersDuration.Labels(ns.Name, container.Name, container.Pod, container.Image, container.Version);
                                    published[key] = gauge;
                                }

                                gauge.Set(duration.TotalSeconds);
                            }
                        }
                    }

                    var gauges = published.Select(p => p.Key);
                    foreach (var key in gauges)
                    {
                        if (!latestKeys.Contains(key) && published.Remove(key, out var removed))
                        {
                            removed.Unpublish();
                            removed.Dispose();
                        }
                    }

                    _logger.LogInformation($"Collected {count} containers");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed collecting containers");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), CancellationToken.None);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Monitoring failed");
            MetricsHostedServiceState.Fail();
        }
        finally
        {
            _logger.LogWarning("Monitoring exited");
        }
    }

    private async Task<Dictionary<string, List<Container>>> GetContainersAsync(Kubernetes client, string namespaceName)
    {
        var containerMap = new Dictionary<string, List<Container>>();

        var list = await client.ListNamespacedPodAsync(namespaceName);

        foreach (var item in list)
        {
            foreach (var container in item.Spec.Containers)
            {
                if (!containerMap.TryGetValue(container.Name, out var containers))
                {
                    containers = new List<Container>();
                    containerMap[container.Name] = containers;
                }

                var value = new Container(container.Name, item.Metadata.Name, item.Status.StartTime, container.Image);
                containers.Add(value);
            }
        }

        return containerMap;
    }
}