// File: src/Components/CompositesBuilder.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Class: CompositesBuilder
public class CompositesBuilder
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

            if (!string.IsNullOrEmpty(runtimeDesc.Value.BinariesPath))
            {
                srcPath = runtimeDesc.Value.BinariesPath;
                destPath += "/shared";

                _logger.Write($"Copying runtime binaries from {srcPath}"
                            + $" to {destPath}...\n");

                CopyBinariesFromPath(srcPath, destPath);
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

            // TODO: Add check to skip if we already have the runtime binaries
            //       for this OS :)

            _logger.Write($"Copying crossgen2 binaries from {srcPath}"
                        + $" to {destPath}...\n");

            CopyBinariesFromPath(srcPath, destPath);
            crossgen2Desc.Value.Path = destPath;
        }
    }

    // This function does a recursive copy by design.
    private void CopyBinariesFromPath(string srcPath, string destPath)
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

        foreach (DirectoryInfo d in dirsToCopy)
        {
            string destSubDirPath = Path.Combine(destPath, d.Name);
            CopyBinariesFromPath(d.FullName, destSubDirPath);
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

            // TODO: Add check for recompilation needed, and skip in that case :)

            string osCode = config.Os.Substring(0, 3);
            // TODO: Replace 'results' with the name of the builds generated :)
            string destPath = $"{Constants.ResourcesPath}/{osCode}-output-results";

            // TODO: Add check to skip if we already have the composite images
            //       for this configuration :)

            // Apply Crossgen2 here. Also, don't forget to add a property to
            // record the composites resulting path in the configuration object.
        }
        return ;
    }
}
