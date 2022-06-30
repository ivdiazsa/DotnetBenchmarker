// File: BuildEngine.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// Class: BuildEngine
// This little buddy will process and generate all composite or non-composite
// assemblies as requested :)
public class BuildEngine
{
    static readonly Dictionary<PlatformID, string> OSMap =
        new Dictionary<PlatformID, string>()
        {
            { PlatformID.Win32NT, "Windows" },
            { PlatformID.Unix, "Linux" }
        };

    static readonly string TargetOS = OSMap[Environment.OSVersion.Platform];
    static readonly string OSCode = TargetOS.Substring(0, 3).ToLower();

    static void Main(string[] args)
    {
        // This reads the environment variables set by the Dockerfile or
        // Powershell, depending on the platform, automatically.
        var engine = new EngineEnvironment();

        if (string.IsNullOrEmpty(args[0]) || !Directory.Exists(args[0]))
        {
            throw new ArgumentException("The passed resources directory"
                + $" {(string.IsNullOrEmpty(args[0]) ? "(null)" : args[0])}"
                + " was unexpected.");
        }

        string resourcesDir = args[0];
        Directory.SetCurrentDirectory(resourcesDir);

        string outputDir = $"{OSCode}-output-{engine.CompositesType}";
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        AppPaths enginePaths = GetPathsSet(engine);

        if (engine.RequestedNonComposites())
            ProcessNonComposite(enginePaths, engine);
        else
            ProcessComposite(enginePaths, engine);
    }

    private static AppPaths GetPathsSet(EngineEnvironment engEnv)
    {
        string dotnetRootPath = $"Dotnet{TargetOS}/dotnet{engEnv.DotnetVersionNumber}";

        return new AppPaths(
            outputPath: $"{OSCode}-output-{engEnv.CompositesType}",
            dotnetPath: dotnetRootPath,
            fxPath: FindPathToFile($"{dotnetRootPath}/shared",
                                   "System.Private.CoreLib.dll"),
            aspPath: FindPathToFile($"{dotnetRootPath}/shared",
                                    "Microsoft.AspNetCore.dll"),
            crossgen2Path: $"Crossgen2{TargetOS}/crossgen2"
        );
    }

    private static string FindPathToFile(string root, string filename)
    {
        return Path.GetDirectoryName(
            Directory.GetFiles(root, filename, SearchOption.AllDirectories)
                     .FirstOrDefault(string.Empty)
        )!;
    }

    private static void ProcessNonComposite(AppPaths enginePaths,
                                            EngineEnvironment engine)
    {
        var gen = new NormalCommandGenerator(enginePaths, engine, TargetOS);

        // This will probably be changed later to allow the user to select
        // whether they want to process either one or both products assemblies.
        // That will likely require some overhaul in the main app as well though.

        if (Directory.Exists(enginePaths.fx))
        {
            string[] assemblies = Directory.GetFiles(enginePaths.fx, "*.dll");
            foreach (var bin in assemblies)
            {
                gen.GenerateCmd(bin, enginePaths.fx);
                RunCrossgen2(gen.GetCmd(), enginePaths.crossgen2exe);
            }
        }

        if (Directory.Exists(enginePaths.asp))
        {
            // Crossgen2 the asp.net assemblies.
        }

        CopyRemainingBinaries(enginePaths.fx, enginePaths.asp, enginePaths.output);
    }

    private static void ProcessComposite(AppPaths enginePaths,
                                         EngineEnvironment engine)
    {
        CompositeCommandGenerator gen;

        if (engine.FrameworkComposite)
            gen = new FxCompositeCommandGenerator(enginePaths, engine, TargetOS);

        else if (engine.AspnetComposite || !engine.BundleAspnet)
            gen = new AspCompositeCommandGenerator(enginePaths, engine, TargetOS);

        else
            throw new ArgumentException("Could not process this given configuration"
                                        + " for composites generation.");

        gen.GenerateCmd();
        RunCrossgen2(gen.GetCmd(), enginePaths.crossgen2exe);
    }

    private static void RunCrossgen2(string generatedCmd, string crossgen2Path)
    {
        using (Process crossgen2 = new Process())
        {
            string[] fullCmdArgs = generatedCmd.Split(' ');

            var startInfo = new ProcessStartInfo
            {
                FileName = fullCmdArgs.FirstOrDefault(crossgen2Path),
                Arguments = string.Join(' ', fullCmdArgs.Skip(1)),
            };

            crossgen2.StartInfo = startInfo;
            crossgen2.Start();
            crossgen2.WaitForExit();
        }
    }

    private static void CopyRemainingBinaries(string netCorePath, string aspNetPath,
                                              string resultsPath)
    {
        MergeFolders(netCorePath, resultsPath);
        MergeFolders(aspNetPath, resultsPath);
    }

    private static void MergeFolders(string srcPath, string destPath)
    {
        string[] files = Directory.GetFiles(srcPath);
        foreach (var item in files)
        {
            string itemName = Path.GetFileName(item);
            string destItemPath = Path.Combine(destPath, itemName);

            if (!File.Exists(destItemPath))
            {
                Console.WriteLine($"Copying {itemName} from {srcPath} to {destPath}...");
                File.Copy(item, destItemPath);
            }
        }
    }
}
