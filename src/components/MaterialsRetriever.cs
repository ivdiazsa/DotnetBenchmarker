// File: src/components/MaterialsRetriever.cs
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace DotnetBenchmarker;

// Class: MaterialsRetriever
public partial class AssembliesWorkshop
{
    internal class MaterialsRetriever
    {
        public void SearchAndFetch(Dictionary<string, AssembliesCollection> assemblies,
                                   List<Configuration> configurations,
                                   MultiIOLogger logger)
        {
            foreach (Configuration config in configurations)
            {
                AssembliesNameLinks asmsLinks = config.AssembliesToUse;
                logger.Write("\n");

                // Find and copy the processed assemblies (if any) for this
                // configuration. If processed assemblies are present, then
                // there's no need to further get a runtime and a crossgen2,
                // so we just return.
                if (!string.IsNullOrEmpty(asmsLinks.Processed))
                {
                    FetchProcessedAssemblies(assemblies[config.Os].Processed,
                                             asmsLinks.Processed, config.Os,
                                             logger);
                    continue;
                }

                // Find and copy the runtime assemblies for this configuration,
                // or download the nightly build.
                FetchRuntimeAssemblies(assemblies[config.Os].Runtimes,
                                       asmsLinks.Runtime, config.Os, logger);

                // Find and copy the crossgen2 assemblies for this configuration.
                // Note that these ones depend on the OS the app is running on,
                // not on the configuration's target OS.
                FetchCrossgen2Assemblies(assemblies[Constants.RunningOs].Crossgen2s,
                                         asmsLinks.Crossgen2, Constants.RunningOs,
                                         logger);
            }
        }

        private void FetchProcessedAssemblies(List<AssembliesDescription> allProcessed,
                                              string procAsmsLink,
                                              string os,
                                              MultiIOLogger logger)
        {
            // Copy the processed assemblies from the location specified in the
            // link, to our resources folder.
            CopyAssembliesFromPathUsingLink(allProcessed, procAsmsLink, os,
                                            "processed", logger);
        }

        private void FetchRuntimeAssemblies(List<AssembliesDescription> allRuntimes,
                                            string runAsmsLink,
                                            string os,
                                            MultiIOLogger logger)
        {
            // We have given runtimes. Therefore, the assemblies link has been
            // set, either directly in the YAML, or to the first one if originally
            // omitted. So, we just copy them normally, unless it is specified
            // we want the latest nightly build.
            if (!allRuntimes.IsEmpty() && !runAsmsLink.Equals("Latest"))
            {
                CopyAssembliesFromPathUsingLink(allRuntimes, runAsmsLink, os,
                                                "runtimes", logger);
                return ;
            }

            // We are left with the remaining case. Either we have no runtimes
            // specified, or the user explicitly requested a nightly build.
            string dstPath = Path.Combine(Constants.Paths.Resources, os, "runtimes",
                                        "latest");

            logger.Write("No runtimes specified. Will use a nightly build...\n");

            if (Directory.Exists(dstPath))
            {
                logger.Write($"'{os.Capitalize()}' nightly runtime build found in"
                            + $" {dstPath}. Skipping...\n");

                // Remember that "Latest" builds are added programatically to
                // the list, so we have to make sure to readd it during any
                // subsequent runs.
                if (!allRuntimes.Any(r => r.Path.Equals(dstPath)))
                    allRuntimes.Add(new AssembliesDescription("Latest", dstPath));

                return ;
            }

            // No runtimes found, so we download a nightly .NET SDK build.
            Directory.CreateDirectory(dstPath);
            DownloadNightlyBuild(os, dstPath, logger);

            // Since we officially now have another runtime build (the latest),
            // add it to the list so later on the configuration(s) that require
            // it can find it.
            allRuntimes.Add(new AssembliesDescription("Latest", dstPath));
        }

        private void FetchCrossgen2Assemblies(List<AssembliesDescription> allCg2s,
                                              string cg2AsmsLink,
                                              string os,
                                              MultiIOLogger logger
        )
        {
            // Copy the processed assemblies from the location specified in the
            // link, to our resources folder.
            CopyAssembliesFromPathUsingLink(allCg2s, cg2AsmsLink, os,
                                            "crossgen2s", logger);
        }

        private void CopyAssembliesFromPathUsingLink(List<AssembliesDescription> allAsms,
                                                     string asmsLink,
                                                     string os,
                                                     string asmsKind,
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
            string dstPath = Path.Combine(Constants.Paths.Resources, os,
                                          asmsKind, searchedAsms.Name);

            if (Directory.Exists(dstPath))
            {
                logger.Write($"'{searchedAsms.Name}' {asmsKind} assemblies"
                            + $" found in {dstPath}. Skipping...\n");
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
                _ => throw new System.PlatformNotSupportedException(
                    $"{os} is unsupported. How did this get here?"
                )
            };

            // The compressed file extension differs as well among platforms.
            string zipExtension = osCode.Equals("win") ? "zip" : "tar.gz";

            // We can now construct the url to download.
            string url = $"https://aka.ms/dotnet/{Constants.DotnetVersion}xx/"
                        + $"daily/dotnet-sdk-{osCode}-x64.{zipExtension}";

            string dlPath = Path.Combine(dstPath,
                                        $"dotnet-sdk-{osCode}-x64.{zipExtension}");

            // Download and extract the bundled zip or tar.
            logger.Write($"Downloading latest {os.Capitalize()} .NET SDK nightly build"
                        + $" to {dlPath}...\n");

            var webClient = new WebClient();
            webClient.DownloadFile(url, dlPath);

            logger.Write($"Extracting latest {os.Capitalize()} .NET SDK nightly build...\n");
            using (Process tar = new Process())
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = $"-xf {dlPath} -C {dstPath}",
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
}
