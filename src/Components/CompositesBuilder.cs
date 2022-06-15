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

        if (!HasEnoughBuildResources(osRequired))
        {
            _logger.Write(@"\nMissing Crossgen2 platforms to generate the 
                            necessary components later...\n");
            System.Environment.Exit(-1);
        }

        // Might be beneficial to try to generalize this. Similarly to the
        // Phases classes, these two methods are really similar :)
        SearchAndFetchRuntimes();
        SearchAndFetchCrossgen2s();
        BuildComposites();
    }

    private bool HasEnoughBuildResources(string[] osRequired)
    {
        string[] crossgen2OsProvided = _crossgen2s.Keys.Distinct().ToArray();
        return !osRequired.Except(crossgen2OsProvided).Any();
    }

    private void SearchAndFetchRuntimes()
    {
        _logger.Write("\nBeginning search and copy of the runtime binaries...\n");

        foreach (KeyValuePair<string, Runtime> runtimeDesc in _runtimes)
        {
            string os = runtimeDesc.Key;
            string srcPath = string.Empty;
            string destPath = $"{Constants.ResourcesPath}/Dotnet{os.Capitalize()}/dotnet7.0";
        
            // TODO: Add check to skip if we already have the runtime binaries
            //       for this OS :)
            if (Directory.Exists(destPath))
            {
                _logger.Write("\nFound a ready to use .NET runtime for"
                            + $" {os.Capitalize()}. Continuing...\n");
                continue;
            }

            if (!string.IsNullOrEmpty(runtimeDesc.Value.BinariesPath))
            {
                srcPath = runtimeDesc.Value.BinariesPath;
                destPath += "/shared";

                _logger.Write($"Copying runtime binaries from {srcPath}"
                            + $" to {destPath}...\n");

                CopyBinariesFromPath(srcPath, destPath, true);
            }

            // TODO: Handle getting the binaries from the runtime repo, or a
            //       nightly build.
            runtimeDesc.Value.BinariesPath = destPath;
        }
    }

    private void SearchAndFetchCrossgen2s()
    {
        _logger.Write("\nBeginning search and copy of the crossgen2 binaries...\n");

        foreach (KeyValuePair<string, Crossgen2> crossgen2Desc in _crossgen2s)
        {
            string os = crossgen2Desc.Key;
            string srcPath = crossgen2Desc.Value.Path;
            string destPath = $"{Constants.ResourcesPath}/Crossgen2{os.Capitalize()}";

            // TODO: Add check to skip if we already have the crossgen2 binaries
            //       for this OS :)
            if (Directory.Exists(destPath))
            {
                _logger.Write("\nFound a ready to use crossgen2 build for"
                            + $" {os.Capitalize()}. Continuing...\n");
                continue;
            }

            _logger.Write($"Copying crossgen2 binaries from {srcPath}"
                        + $" to {destPath}...\n");

            CopyBinariesFromPath(srcPath, destPath, false);
            crossgen2Desc.Value.Path = destPath;
        }
    }

    private void CopyBinariesFromPath(string srcPath, string destPath, bool recurse)
    {
        var srcDirInfo = new DirectoryInfo(srcPath);
        DirectoryInfo[] dirsToCopy = srcDirInfo.GetDirectories();
        FileInfo[] filesToCopy = srcDirInfo.GetFiles();

        if (!Directory.Exists(destPath))
            _ = Directory.CreateDirectory(destPath);

        foreach (FileInfo f in filesToCopy)
        {
            string destFilepath = Path.Combine(destPath, f.Name);
            f.CopyTo(destFilepath);
        }

        if (recurse)
        {
            foreach (DirectoryInfo d in dirsToCopy)
            {
                string destSubDirPath = Path.Combine(destPath, d.Name);
                CopyBinariesFromPath(d.FullName, destSubDirPath, true);
            }
        }
        return ;
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
            string destPath = $"{Constants.ResourcesPath}/{osCode}-output-{buildParams.FxResultName}";

            if (HasValidProcessedAssemblies(destPath))
            {
                config.ProcessedAssembliesPath = destPath;
                _logger.Write("\nFound assemblies ready for this configuration in"
                            + $" {destPath}...\n");
                continue;
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
        return ;
    }

    private bool HasValidProcessedAssemblies(string path)
    {
        if (!Directory.Exists(path))
            return false;

        return (File.Exists($"{path}/System.Private.CoreLib.dll")
             && File.Exists($"{path}/System.Runtime.dll"));
    }
}
