// File: src/components/AssembliesWorkshop.cs
using System.Collections.Generic;

namespace DotnetBenchmarker;

// Class: AssembliesWorkshop
public partial class AssembliesWorkshop
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
        BuildReadyToRunImages();
    }

    private void BuildReadyToRunImages()
    {
        for (int i = 0, total = _configurations.Count; i < total; i++)
        {
            var config = _configurations[i];
            var buildParams = config.BuildPhase;

            _logger.Write($"\nSetting up for configuration {config.Name} ({i+1}/{total})...\n");

            if (buildParams is null)
            {
                _logger.Write("No Build Phase required for this configuration."
                            + " Moving on to the next...\n");
                continue;
            }
        }

        return ;
    }
}
