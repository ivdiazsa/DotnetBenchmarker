// File: src/components/AssembliesWorkshop.cs
using System.Collections.Generic;
using System.Text;

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

            // string crossgenCmd = GenerateCrossgenCommand();
        }

        return ;
    }

    private string GenerateCrossgenCommand(BuildPhaseDescription buildParams,
                                           string targetOs)
    {
        var cmdSb = new StringBuilder();
        string compositeResultName = string.Empty;

        cmdSb.AppendFormat("--targetos={0}", targetOs);
        cmdSb.Append(" --targetarch=x64");

        if (buildParams.UseAvx2)
        {
            _logger.Write("Will apply AVX2 instruction set...\n");
            cmdSb.Append(" --instruction-set=avx2");
        }

        // TODO: Need to deal with finding the StandardOptimizationData.mibc
        // file in the same directory as the crossgen2 executable is.

        if (buildParams.IsComposite())
            cmdSb.Append(" --composite");

        if (buildParams.FrameworkComposite)
        {
            compositeResultName += "framework";
            _logger.Write("Compiling Framework Composites...\n");
            // Need the paths to the configuration's unprocessed runtime binaries.
        }

        return cmdSb.ToString();
    }
}
