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

        foreach (KeyValuePair<string, Runtime> runtimeDesc in runtimes)
        {
            string os = runtimeDesc.Key;
            string srcPath = string.Empty;
            string destPath = $"{Constants.ResourcesPath}/Dotnet{os.Capitalize()}/dotnet7.0";
        
            // TODO: Add check to skip if we already have the runtime binaries
            //       for this OS :)
            if (Directory.Exists(destPath))
            {
                logger.Write("\nFound a ready to use .NET runtime for"
                            + $" {os.Capitalize()}. Continuing...\n");
                continue;
            }

            if (!string.IsNullOrEmpty(runtimeDesc.Value.BinariesPath))
            {
                srcPath = runtimeDesc.Value.BinariesPath;
                destPath += "/shared";

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
                // TODO: Add support to completely omit the "runtimes" info
                //       in the YAML file when a nightly build will be requested.
                DownloadNightlyRuntime(destPath, os, logger);
            }

            runtimeDesc.Value.BinariesPath = destPath;
        }
    }

    public void SearchAndFetchCrossgen2s(Dictionary<string, Crossgen2> crossgen2s,
                                         MultiIOLogger logger)
    {
        logger.Write("\nBeginning search and copy of the crossgen2 binaries...\n");

        foreach (KeyValuePair<string, Crossgen2> crossgen2Desc in crossgen2s)
        {
            string os = crossgen2Desc.Key;
            string srcPath = crossgen2Desc.Value.Path;
            string destPath = $"{Constants.ResourcesPath}/Crossgen2{os.Capitalize()}";

            // TODO: Add check to skip if we already have the crossgen2 binaries
            //       for this OS :)
            if (Directory.Exists(destPath))
            {
                logger.Write("\nFound a ready to use crossgen2 build for"
                            + $" {os.Capitalize()}. Continuing...\n");
                continue;
            }

            logger.Write($"Copying crossgen2 binaries from {srcPath}"
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
    }

    private void DownloadNightlyRuntime(string destPath, string os,
                                        MultiIOLogger logger)
    {
        string sdkFilename = string.Empty;

        switch (os)
        {
            case "linux":
                sdkFilename = "linux-x64.tar.gz";
                break;
            case "windows":
                sdkFilename = "win-x64.zip";
                break;
            default:
                throw new NotSupportedException($"Invalid OS {os}."
                                               + " How did this get here?");
        }

        if (!Directory.Exists(destPath))
            Directory.CreateDirectory(destPath);

        string url = $"https://aka.ms/dotnet/7.0.1xx/daily/dotnet-sdk-{sdkFilename}";
        logger.Write($"\nDownloading latest {os.Capitalize()} .NET nightly build...\n");

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
