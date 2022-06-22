// File: src/Components/CompositesBuilder.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Class: CompositesBuilder
public partial class CompositesBuilder
{
    private Dictionary<string, Runtime> _runtimes { get; }
    private Dictionary<string, Crossgen2> _crossgen2s { get; }
    private List<Configuration> _configurations { get; }
    private MultiIOLogger _logger { get; }

    public CompositesBuilder(Dictionary<string, Runtime> runsies,
                             Dictionary<string, Crossgen2> cg2s,
                             List<Configuration> configs)
    {
        _runtimes = runsies;
        _crossgen2s = cg2s;
        _configurations = configs;
        _logger = new MultiIOLogger($"{Constants.LogsPath}/build-log-{Constants.Timestamp}.txt");
    }

    public void Run()
    {
        string[] osRequired = _configurations.Select(cfg => cfg.Os)
                                             .Distinct()
                                             .ToArray();

        // BUG: This gets triggered even when the configuration(s) don't
        //      require any sort of rebuilding.
        if (!HasEnoughBuildResources(osRequired))
        {
            _logger.Write("\nMissing Crossgen2 platforms to generate the"
                        + " necessary components later...\n");
            System.Environment.Exit(-1);
        }

        var binRetriever = new BinariesRetriever();
        binRetriever.SearchAndFetchRuntimes(_runtimes, _logger);
        binRetriever.SearchAndFetchCrossgen2s(_crossgen2s, _logger);
        BuildComposites();
    }

    private bool HasEnoughBuildResources(string[] osRequired)
    {
        string[] crossgen2OsProvided = _crossgen2s.Keys.Distinct().ToArray();
        return !osRequired.Except(crossgen2OsProvided).Any();
    }

    private void BuildComposites()
    {
        for (int i = 0, total = _configurations.Count; i < total; i++)
        {
            var config = _configurations[i];
            var buildParams = config.BuildPhase;

            _logger.Write($"\nSetting up for {config.Name} ({i+1}/{total})...\n");

            // To ensure we are comparing apples to apples, if any given
            // configuration did not require any sort of rebuilding or processing,
            // then we upload the binaries we had. This will ensure that whenever
            // we compare runs of clean vs processed, we are using the same basis.
            if (!buildParams.NeedsRecompilation())
            {
                string defaultRuntimePath = _runtimes[config.Os].BinariesPath;

                string coreLibPath = Directory.GetFiles(defaultRuntimePath,
                                                        "System.Private.CoreLib.dll",
                                                        SearchOption.AllDirectories)
                                              .FirstOrDefault(string.Empty);

                string aspNetPath = Directory.GetFiles(defaultRuntimePath,
                                                       "Microsoft.AspNetCore.dll",
                                                       SearchOption.AllDirectories)
                                             .FirstOrDefault(string.Empty);

                config.ProcessedAssembliesPath = $"{Path.GetDirectoryName(coreLibPath)}"
                                        + $";{Path.GetDirectoryName(aspNetPath)}";
                continue;
            }

            string osCode = config.Os.Substring(0, 3);
            string destPath = $"{Constants.ResourcesPath}/"
                            + $"{osCode}-output-{config.BuildResultsName}";

            if (HasValidProcessedAssemblies(destPath))
            {
                config.ProcessedAssembliesPath = destPath;
                _logger.Write("\nFound assemblies ready for this configuration in"
                            + $" {destPath}...\n");
                continue;
            }

            if (buildParams.IsPartialComposites()
                && config.Os.Equals("linux", StringComparison.OrdinalIgnoreCase))
            {
                string fxAssembliesFile = Path.GetFileName(buildParams.PartialFxComposites);
                string aspAssembliesFile = Path.GetFileName(buildParams.PartialAspComposites);
                string copyDest = string.Empty;

                if (File.Exists(buildParams.PartialFxComposites))
                {
                    copyDest = Path.Combine(Constants.ResourcesPath, fxAssembliesFile);
                    File.Copy(buildParams.PartialFxComposites, copyDest, true);
                    buildParams.PartialFxComposites = fxAssembliesFile;
                }

                if (File.Exists(buildParams.PartialAspComposites))
                {
                    copyDest = Path.Combine(Constants.ResourcesPath, aspAssembliesFile);
                    File.Copy(buildParams.PartialAspComposites, copyDest, true);
                    buildParams.PartialAspComposites = aspAssembliesFile;
                }
            }

            if (config.Os.Equals("linux", StringComparison.OrdinalIgnoreCase))
                LinuxCrossgen2er.Apply(config, _logger);

            else if (config.Os.Equals("windows", StringComparison.OrdinalIgnoreCase))
                WindowsCrossgen2er.Apply(config, _logger);

            else
                throw new PlatformNotSupportedException($"Invalid OS {config.Os}."
                                                   + " How did this get here?");
            config.ProcessedAssembliesPath = destPath;
        }
    }

    private bool HasValidProcessedAssemblies(string path)
    {
        if (!Directory.Exists(path))
            return false;

        return (File.Exists($"{path}/System.Private.CoreLib.dll")
             && File.Exists($"{path}/System.Runtime.dll"));
    }
}
