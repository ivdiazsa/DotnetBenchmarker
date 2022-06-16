// File: src/Components/CompositesBuilder.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Class: CompositesBuilder
// TODO: Move all the binaries getting stuff to a class of its own.
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

        if (!HasEnoughBuildResources(osRequired))
        {
            _logger.Write(@"\nMissing Crossgen2 platforms to generate the 
                            necessary components later...\n");
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

            if (!buildParams.NeedsRecompilation())
                continue;

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

            if (!string.IsNullOrEmpty(config.PartialComposites))
            {
                string partialCompositesFile = Path.GetFileName(config.PartialComposites);
                string copyPath = $"{Constants.ResourcesPath}/{partialCompositesFile}";

                if (!File.Exists(copyPath))
                {
                    File.Copy(config.PartialComposites, copyPath, true);
                }
                config.PartialComposites = copyPath;
            }

            if (config.Os.Equals("linux", StringComparison.OrdinalIgnoreCase))
                LinuxCrossgen2er.Apply(config, _logger);

            else if (config.Os.Equals("windows", StringComparison.OrdinalIgnoreCase))
                WindowsCrossgen2er.Apply(config, _logger);

            else
                throw new NotSupportedException($"Invalid OS {config.Os}."
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
