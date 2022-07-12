// File: src/Components/BinariesRetriever.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
// using System.Net.Http;
using System.Text.RegularExpressions;

// Class: BinariesRetriever
public class BinariesRetriever
{
    public void SearchAndFetchRuntimes(Dictionary<string, Runtime> runtimes,
                                       MultiIOLogger logger)
    {
        logger.Write("\nBeginning search and copy of the runtime binaries...\n");

        // Our runtime(s)' information comes in a dictionary, where the OS is
        // the key for easy management and use. See AppOptionsBank.cs to know
        // where it comes from.
        foreach (KeyValuePair<string, Runtime> runtimeDesc in runtimes)
        {
            string os = runtimeDesc.Key;
            string srcPath = string.Empty;
            string destPath = $"{Constants.ResourcesPath}/Dotnet{os.Capitalize()}/dotnet7.0";
        
            if (HasValidRuntime($"{destPath}/shared"))
            {
                logger.Write("\nFound a ready to use .NET runtime for"
                            + $" {os.Capitalize()}. Continuing...\n");
                runtimeDesc.Value.BinariesPath = destPath;
                continue;
            }

            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);

            if (!string.IsNullOrEmpty(runtimeDesc.Value.BinariesPath))
            {
                // If a path with the runtime binaries was provided, then we only
                // ought to copy them to our Resources location.
                srcPath = runtimeDesc.Value.BinariesPath;
                destPath += "/shared"; // Match the nightly build layout.

                logger.Write($"Copying runtime binaries from {srcPath}"
                            + $" to {destPath}...\n");

                CopyBinariesFromPath(srcPath, destPath, true);
            }
            else
            {
                // Download a nightly build from the installer repo.
                DownloadNightlyRuntime(destPath, os, logger);

                // If a runtime repo path was specified, then fetch those
                // binaries as well. The reason we also downloaded the nightly
                // build in this case, is to also have the ASP.NET core assemblies.

                if (!string.IsNullOrEmpty(runtimeDesc.Value.RepoPath))
                {
                    CopyBinariesFromRuntimeRepo(runtimeDesc.Value.RepoPath,
                                                destPath, os, logger);

                    // Keep only the runtime repo netcore binaries.
                    string[] netcoreBuilds = Directory.GetDirectories($"{destPath}/"
                                                            + "shared/Microsoft.NETCore.App");
                    foreach (var folder in netcoreBuilds)
                    {
                        if (!folder.Contains("dev"))
                            Directory.Delete(folder, true);
                    }
                }
            }

            runtimeDesc.Value.BinariesPath = destPath;
        }
    }

    public void SearchAndFetchCrossgen2s(Dictionary<string, Crossgen2> crossgen2s,
                                         MultiIOLogger logger)
    {
        logger.Write("\nBeginning search and copy of the crossgen2 binaries...\n");

        // Same case as described in the previous function with the runtimes.
        foreach (KeyValuePair<string, Crossgen2> crossgen2Desc in crossgen2s)
        {
            string os = crossgen2Desc.Key;
            string srcPath = crossgen2Desc.Value.Path;
            string destPath = $"{Constants.ResourcesPath}/Crossgen2{os.Capitalize()}";

            if (HasValidCrossgen2(destPath))
            {
                logger.Write("\nFound a ready to use crossgen2 build for"
                            + $" {os.Capitalize()}. Continuing...\n");
                crossgen2Desc.Value.Path = destPath;
                continue;
            }

            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);

            logger.Write($"Copying crossgen2 binaries from {srcPath}"
                        + $" to {destPath}...\n");

            // Copy the crossgen2 build to our Resources path. Here, we don't do
            // a deep copy because potentially, the crossgen2 build folder might
            // have further nested published artifacts, which will confuse the
            // BuildEngine later on. So, we ensure we only one build.
            CopyBinariesFromPath(srcPath, destPath, false);
            crossgen2Desc.Value.Path = destPath;
        }
    }

    private bool HasValidRuntime(string dir)
    {
        if (!Directory.Exists(dir))
            return false;

        // Since we don't know the structure of the runtime folder given to us
        // beforehand, we have to search it all in its entirety to determine
        // whether it's ready to use or not.

        string[] coreLibDlls = Directory.GetFiles(dir, "System.Private.CoreLib.dll",
                                                  SearchOption.AllDirectories);

        string[] runtimeDlls = Directory.GetFiles(dir, "System.Runtime.dll",
                                                  SearchOption.AllDirectories);

        // We will assume that if these two dll's are present, then the provided
        // runtime is fine to use :)
        return !coreLibDlls.IsEmpty() && !runtimeDlls.IsEmpty();
    }

    private bool HasValidCrossgen2(string dir)
    {
        if (!Directory.Exists(dir))
            return false;

        // We will assume that if the dll and exe are present, then the crossgen2
        // build is fine to use :)
        return (File.Exists($"{dir}/crossgen2.dll")
            && (File.Exists($"{dir}/crossgen2.exe")
                || File.Exists($"{dir}/crossgen2")));
    }

    private void CopyBinariesFromPath(string srcPath, string destPath, bool recurse)
    {
        var srcDirInfo = new DirectoryInfo(srcPath);
        DirectoryInfo[] dirsToCopy = srcDirInfo.GetDirectories();
        FileInfo[] filesToCopy = srcDirInfo.GetFiles();

        foreach (FileInfo f in filesToCopy)
        {
            string destFilepath = Path.Combine(destPath, f.Name);
            f.CopyTo(destFilepath);
        }

        // Do a deep copy instead of just top level.
        if (recurse)
        {
            foreach (DirectoryInfo d in dirsToCopy)
            {
                string destSubDirPath = Path.Combine(destPath, d.Name);
                CopyBinariesFromPath(d.FullName, destSubDirPath, true);
            }
        }
    }

    private void CopyBinariesFromRuntimeRepo(string repoPath, string destPath,
                                             string os, MultiIOLogger logger)
    {
        // The Shipping path in the runtime repo contains several files with
        // different flavors of the runtime, including the "default" one we
        // want to use here. It's equivalent as if you were able to get a nightly
        // build, but using your own version of the repo, instead of the official one.
        string shippingPath = $"{repoPath}/{Constants.RuntimeRepoShippingPath}";
        string zipNamePattern = @"dotnet-runtime-(\d)(\.\d){2}-dev";

        string extension = os.Equals("windows", StringComparison.OrdinalIgnoreCase)
                           ? "*.zip"
                           : "*.tar.gz";

        string binsCompressed = Directory.GetFiles(shippingPath, extension)
                                         .FirstOrDefault(z => Regex.IsMatch(z, zipNamePattern),
                                                         string.Empty);

        logger.Write($"\nUsing Runtime Repo located in {repoPath}...\n");

        if (string.IsNullOrEmpty(binsCompressed))
        {
            throw new FileNotFoundException($"No runtime dev zip found in {shippingPath}"
                                            + " Make sure you built the packs and"
                                            + " try again.");
        }

        logger.Write($"Copying {binsCompressed}...\n");
        File.Copy(binsCompressed, Path.Combine(destPath, Path.GetFileName(binsCompressed)));

        logger.Write($"Extracting {os.Capitalize()} runtime dev build...\n");
        ExtractCompressedFile(binsCompressed, destPath);
    }

    private void DownloadNightlyRuntime(string destPath, string os,
                                        MultiIOLogger logger)
    {
        string sdkFilename = string.Empty;

        // The nightly builds come in different formats, depending on the OS
        // they are aimed for.
        switch (os)
        {
            case "linux":
                sdkFilename = "linux-x64.tar.gz";
                break;
            case "windows":
                sdkFilename = "win-x64.zip";
                break;
            default:
                throw new PlatformNotSupportedException($"Invalid OS {os}."
                                                  + " How did this get here?");
        }

        string url = $"https://aka.ms/dotnet/7.0.1xx/daily/dotnet-sdk-{sdkFilename}";
        logger.Write($"\nDownloading latest {os.Capitalize()} .NET SDK nightly build...\n");

        // Download and extract the bundled zip or tar.
        var webClient = new WebClient();
        webClient.DownloadFile(url, $"{destPath}/dotnet-sdk-{sdkFilename}");

        logger.Write($"Extracting latest {os.Capitalize()} .NET SDK nightly build...\n");
        ExtractCompressedFile($"{destPath}/dotnet-sdk-{sdkFilename}", destPath);

        // Will use the code below instead when I finally figure out how to get
        // the stupid async commands work. They just ignore me at the moment.

        // using (HttpClient webClient = new HttpClient())
        // using (Stream s = await webClient.GetStreamAsync(url))
        // using (FileStream fs = new FileStream($"{destPath}/{sdkFilename}",
        //                                         FileMode.CreateNew))
        // {
        //     await s.CopyToAsync(fs);
        // }
    }

    private void ExtractCompressedFile(string zipTar, string dest)
    {
        using (Process tar = new Process())
        {
            var startInfo = new ProcessStartInfo();
            tar.StartInfo = startInfo.BaseTemplate("tar", $"-xf {zipTar} -C {dest}");
            tar.Start();
            tar.WaitForExit();
        }
    }
}
