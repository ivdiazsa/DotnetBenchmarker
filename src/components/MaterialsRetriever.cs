// File: src/components/MaterialsRetriever.cs
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace DotnetBenchmarker;

// Class: MaterialsRetriever
public class MaterialsRetriever
{
    public void SearchAndFetch(Dictionary<string, AssembliesCollection> assemblies,
                               List<Configuration> configurations,
                               MultiIOLogger logger)
    {
        var lol = new HashSet<string>();

        foreach (Configuration config in configurations)
        {
            AssembliesNameLinks asmsLinks = config.AssembliesToUse;

            // GetProcessedAssemblies()
            // Find and copy the processed assemblies (if any) for this configuration.
            if (!string.IsNullOrEmpty(asmsLinks.Processed))
            {
                FetchProcessedAssemblies(assemblies[config.Os].Processed,
                                         asmsLinks.Processed, config.Os, lol,
                                         logger);
            }

            // GetRuntimeAssemblies()
            FetchRuntimeAssemblies(assemblies[config.Os].Runtimes,
                                   asmsLinks.Runtime, config.Os, lol, logger);

            // GetCrossgen2Assemblies()
        }
        return ;
    }

    private void FetchProcessedAssemblies(List<AssembliesDescription> allProcessed,
                                          string procAsmsLink,
                                          string os,
                                          HashSet<string> lol,
                                          MultiIOLogger logger)
    {
        // Copy the processed assemblies from the location specified in the
        // link, to our resources folder.
        CopyAssembliesFromPathUsingLink(allProcessed, procAsmsLink, os,
                                        "processed", lol, logger);
    }

    private void FetchRuntimeAssemblies(List<AssembliesDescription> allRuntimes,
                                        string runAsmsLink,
                                        string os,
                                        HashSet<string> lol,
                                        MultiIOLogger logger)
    {
        // We have given runtimes. Therefore, the assemblies link has been set,
        // either directly in the YAML, or to the first one if originally omitted.
        // So, we just copy them normally, unless it is required we want the
        // latest nightly build.
        if (!allRuntimes.IsEmpty() && !runAsmsLink.Equals("Latest"))
        {
            CopyAssembliesFromPathUsingLink(allRuntimes, runAsmsLink, os,
                                            "runtimes", lol, logger);
            return ;
        }

        // We are left with the remaining case. Either we have no runtimes
        // specified, or the user explicitly requested a nightly build.
        string dstPath = Path.Combine(Constants.Paths.Resources, os, "runtimes",
                                      "latest");

        logger.Write("\nNo runtimes specified. Will use a nightly build...\n");

        if (Directory.Exists(dstPath))
        {
            // If we haven't informed the user about these assemblies
            // already present, then do so now. Otherwise, just skip
            // and continue processing.
            if (!lol.Contains(dstPath))
            {
                logger.Write($"'{os.Capitalize()}' nightly runtime build"
                            + $" found in {dstPath}. Skipping...\n");
                lol.Add(dstPath);
            }

            return ;
        }

        // No runtimes found, so we download a nightly .NET SDK build.
        Directory.CreateDirectory(dstPath);
    }

    private void CopyAssembliesFromPathUsingLink(List<AssembliesDescription> allAsms,
                                                 string asmsLink,
                                                 string os,
                                                 string asmsKind,
                                                 HashSet<string> lol,
                                                 MultiIOLogger logger)
    {
        // It is guaranteed we will find a match here. If not, then that
        // would mean a bug in our validation process that we would have
        // to take a look at.
        AssembliesDescription searchedAsms = allAsms.Find(
            asmsDesc => asmsDesc.Name.Equals(asmsLink)
        )!;

        // Check if we already have these processed assemblies. If not,
        // then we copy them. Otherwise, we skip them.
        string srcPath = searchedAsms.Path;
        string dstPath = Path.Combine(Constants.Paths.Resources, os, asmsKind,
                                      searchedAsms.Name);

        if (Directory.Exists(dstPath))
        {
            // If we haven't informed the user about these assemblies
            // already present, then do so now. Otherwise, just skip
            // and continue processing.
            if (!lol.Contains(dstPath))
            {
                logger.Write($"'{searchedAsms.Name}' {asmsKind} assemblies"
                            + $" found in {dstPath}. Skipping...\n");
                lol.Add(dstPath);
            }

            return ;
        }

        // Bring those processed assemblies to our resources folder tree.
        logger.Write($"Copying {asmsKind} assemblies from '{srcPath}' to"
                    + $" '{dstPath}'...\n");
        CopyDirectoryContents(srcPath, dstPath);
    }

    private void DownloadNightlyBuild(string os, string dstPath, MultiIOLogger logger)
    {
        // The SDK download urls are classified by an OS code.
        string osCode = os switch
        {
            "windows" => "win",
            "macos" => "osx",
            "linux" => "linux",
            _ => throw new System.PlatformNotSupportedException($"{os} is unsupported."
                                                            + " How did this get here?")
        };

        // The compressed file extension differs as well among platforms.
        string zipExtension = osCode.Equals("win") ? "zip" : "tar.gz";

        // We can now construct the url to download.
        string url = $"https://aka.ms/dotnet/{Constants.DotnetVersion}xx/daily/"
                    + $"dotnet-sdk-{osCode}-x64.{zipExtension}";

        // Download and extract the bundled zip or tar.
        logger.Write($"Downloading latest {os.Capitalize()} .NET SDK nightly build...\n");
        var webClient = new WebClient();

        webClient.DownloadFile(
            url,
            Path.Combine(dstPath, $"dotnet-sdk-{osCode}.{zipExtension}")
        );

        logger.Write($"Extracting latest {os.Capitalize()} .NET SDK nightly build...\n");
        using (Process tar = new Process())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xf {dstPath}/dotnet-sdk-{osCode}.{zipExtension}"
                           + $" -C {dstPath}",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                UseShellExecute = false,
            };

            tar.StartInfo = startInfo;
            tar.Start();
            tar.WaitForExit();
        }
    }

    private void CopyDirectoryContents(string sourceDir, string destinationDir)
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
            CopyDirectoryContents(dInfo.FullName, dirDestinationPath);
        }
    }
}
