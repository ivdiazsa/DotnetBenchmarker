// File: src/Components/BinariesRetriever.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
// using System.Net.Http;

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
            else if (!string.IsNullOrEmpty(runtimeDesc.Value.RepoPath))
            {
                // TODO: Handle getting the stuff from the runtime repo.
            }
            else
            {
                // No binaries given. Download a nightly build from the
                // installer repo in this case.
                DownloadNightlyRuntime(destPath, os, logger);
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

        if (!Directory.Exists(destPath))
            Directory.CreateDirectory(destPath);

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

        if (!Directory.Exists(destPath))
            Directory.CreateDirectory(destPath);

        string url = $"https://aka.ms/dotnet/7.0.1xx/daily/dotnet-sdk-{sdkFilename}";
        logger.Write($"\nDownloading latest {os.Capitalize()} .NET nightly build...\n");

        // Download and extract the bundled zip or tar.
        var webClient = new WebClient();
        webClient.DownloadFile(url, $"{destPath}/dotnet-sdk-{sdkFilename}");

        string srcDir = Environment.CurrentDirectory;
        Directory.SetCurrentDirectory(destPath);
        logger.Write($"Extracting latest {os.Capitalize()} .NET nightly build...\n");

        using (Process tar = new Process())
        {
            var startInfo = new ProcessStartInfo();
            tar.StartInfo = startInfo.BaseTemplate("tar",
                                                  $"-xf dotnet-sdk-{sdkFilename}");
            tar.Start();
            tar.WaitForExit();
        }
        Directory.SetCurrentDirectory(srcDir);

        // Will use the code below instead when I finally figure out how the
        // stupid async commands work. They just ignore me at the moment.

        // // The reason we are using a 'using' statement here is because we don't
        // // need to download any other stuff throughout the app's lifespan. If
        // // that changes in the future, then we would take a more global approach
        // // for the sake of conserving resources.
        // using (HttpClient webClient = new HttpClient())
        // using (Stream s = await webClient.GetStreamAsync(url))
        // using (FileStream fs = new FileStream($"{destPath}/{sdkFilename}",
        //                                         FileMode.CreateNew))
        // {
        //     await s.CopyToAsync(fs);
        // }
    }
}
