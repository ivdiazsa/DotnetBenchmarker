// File: src/components/AssembliesWorkshop.cs
using System.Collections.Generic;
using System.IO;

namespace DotnetBenchmarker;

// Class: AssembliesWorkshop
public class AssembliesWorkshop
{
    private readonly Dictionary<string, AssembliesCollection> _assemblies;
    private readonly List<Configuration> _configurations;
    private readonly MultiIOLogger _logger;

    public AssembliesWorkshop(Dictionary<string, AssembliesCollection> asms,
                              List<Configuration> configs)
    {
        _assemblies = asms;
        _configurations = configs;
        _logger = new MultiIOLogger($"{Constants.Paths.Logs}/build-log-{Constants.Timestamp}.txt");
    }

    public void Run()
    {
        var retriever = new MaterialsRetriever();
        retriever.SearchAndFetch(_assemblies, _configurations, _logger);
    }
}
