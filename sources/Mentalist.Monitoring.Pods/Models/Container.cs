namespace Mentalist.Monitoring.Pods.Models;

public class Container
{
    public Container(string name, string pod, DateTime? startTime, string image)
    {
        Name = name;
        Pod = pod;
        StartTime = startTime;
        Image = image;

        var version = Image.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (version.Length > 1)
            Version = version.Last();
    }

    public string Name { get; }
    public string Pod { get; }
    public DateTime? StartTime { get; }
    public string Image { get; }
    public string Version { get; } = "n/a";
}

public class Namespace
{
    public Namespace(string name, Dictionary<string, List<Container>> containers)
    {
        Name = name;
        Containers = containers;
    }

    public string Name { get; }

    public Dictionary<string, List<Container>> Containers { get; }
}

public class NamespaceList
{
    public NamespaceList(List<Namespace> namespaces)
    {
        Namespaces = namespaces;
    }

    public List<Namespace> Namespaces { get; }
}

public static class Namespaces
{
    public static NamespaceList All { get; set; } = new (new List<Namespace>());
}