// File: BuildEngine.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Class: BuildEngine
// This little buddy will process and generate all composite or non-composite
// assemblies as requested :)
public partial class BuildEngine
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

        // If for whatever reason, we can't read the Resources folder, or it was
        // not passed, then the BuildEngine can't work, so we have to exit.
        if (string.IsNullOrEmpty(args[0]) || !Directory.Exists(args[0]))
        {
            throw new ArgumentException("The passed resources directory"
                + $" {(string.IsNullOrEmpty(args[0]) ? "(null)" : args[0])}"
                + " was unexpected.");
        }

        // Since this app is run from the DotnetBenchmarker, which is run from
        // a cmd script located in the root of the repo, that's the path set
        // as current directory on Windows. We change to the Resources folder,
        // so that we have better access to our resources, and probably even
        // more importantly, to match the directory structure of the Linux container.

        // Because the BuildEngine is multi-platform, it must be able to run
        // transparently, regardless of OS, hence all structures must match.

        string resourcesDir = args[0];
        Directory.SetCurrentDirectory(resourcesDir);

        string outputDir = $"{OSCode}-output-{engine.CompositesType}";
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        AppPaths enginePaths = GetPathsSet(engine);
        var engineCore = new EngineCore();

        // Run the engine!
        if (engine.RequestedNonComposites())
            engineCore.ProcessNonComposite(enginePaths, engine);
        else
            engineCore.ProcessComposite(enginePaths, engine);
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
}
