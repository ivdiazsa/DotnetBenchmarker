// File: src/components/AssembliesWorkshop.cs
using System.Collections.Generic;
using System.IO;

namespace DotnetBenchmarker;

// Class: AssembliesWorkshop
public class AssembliesWorkshop
{
    private Dictionary<string, AssembliesCollection> _assemblies;
    private List<Configuration> _configurations;
    private MultiIOLogger _logger;

    public AssembliesWorkshop(Dictionary<string, AssembliesCollection> asms,
                              List<Configuration> configs)
    {
        _assemblies = asms;
        _configurations = configs;
        _logger = new MultiIOLogger($"{Constants.Paths.Logs}/build-log-{Constants.Timestamp}.txt");
    }

    public void Run()
    {
        var lol = new HashSet<string>();

        foreach (Configuration config in _configurations)
        {
            AssembliesNameLinks asmsLinks = config.AssembliesToUse;

            // GetProcessedAssemblies()
            // Find and copy the processed assemblies (if any) for this configuration.
            if (!string.IsNullOrEmpty(asmsLinks.Processed))
            {
                FetchProcessedAssemblies(_assemblies[config.Os].Processed,
                                         asmsLinks.Processed, config.Os, lol);
            }

            // GetRuntimeAssemblies()

            // GetCrossgen2Assemblies()
        }
        return ;
    }

    private void FetchProcessedAssemblies(List<AssembliesDescription> allProcessed,
                                          string procAsmsLink,
                                          string os,
                                          HashSet<string> lol)
    {
        // It is guaranteed we will find a match here. If not, then that
        // would mean a bug in our validation process that we would have
        // to take a look at.
        AssembliesDescription procAsms = allProcessed.Find(
            asmDesc => asmDesc.Name.Equals(procAsmsLink)
        )!;

        // Check if we already have these processed assemblies. If not,
        // then we copy them. Otherwise, we skip them.
        string srcPath = procAsms.Path;
        string dstPath = Path.Combine(Constants.Paths.Resources, os, "processed",
                                      procAsms.Name);

        if (Directory.Exists(dstPath))
        {
            // If we haven't informed the user about these assemblies
            // already present, then do so now. Otherwise, just skip
            // and continue processing.
            if (!lol.Contains(dstPath))
            {
                _logger.Write($"'{procAsms.Name}' processed assemblies"
                            + $" found in {dstPath}. Skipping...\n");
                lol.Add(dstPath);
            }

            return ;
        }

        // Bring those processed assemblies to our resources folder tree.
        _logger.Write($"Copying processed assemblies from '{srcPath}' to"
                    + $" '{dstPath}'...\n");
        CopyContents(srcPath, dstPath);
    }

    private void CopyContents(string sourceDir, string destinationDir)
    {
        // Get information about the source directory.
        var srcDirInfo = new DirectoryInfo(sourceDir);

        // Check if the source directory exists.
        if (!srcDirInfo.Exists)
            throw new DirectoryNotFoundException($"Directory {srcDirInfo.FullName}"
                                               + " was not found.");

        // Create the destination directory.
        Directory.CreateDirectory(destinationDir);

        // Get all info on the next level of subdirectories.
        DirectoryInfo[] innerDirsInfos = srcDirInfo.GetDirectories();

        // Copy all files.
        foreach (FileInfo fInfo in srcDirInfo.GetFiles())
        {
            string fileDestinationPath = Path.Combine(destinationDir, fInfo.Name);
            fInfo.CopyTo(fileDestinationPath);
        }

        // No more subdirectories, then we're finished.
        if (innerDirsInfos.IsEmpty())
            return ;

        // We still have subfolders to process, so we recurse with a deep copy.
        foreach (DirectoryInfo dInfo in innerDirsInfos)
        {
            string dirDestinationPath = Path.Combine(destinationDir, dInfo.Name);
            CopyContents(dInfo.FullName, dirDestinationPath);
        }
    }
}
