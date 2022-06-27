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

        // TODO: Add an assertion here to ensure the resources path was passed.
        //       Otherwise, well... A disaster is waiting to happen :)
        string resourcesDir = args[0];
        Directory.SetCurrentDirectory(resourcesDir);

        string outputDir = $"{OSCode}-output-{engine.CompositesType}";
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        AppPaths enginePaths = GetPathsSet(engine);
        BaseCommandGenerator gen;

        if (engine.FrameworkComposite)
            gen = new FxCompositeCommandGenerator(enginePaths, engine, TargetOS);

        else if (engine.AspnetComposite || !engine.BundleAspnet)
            gen = new AspCompositeCommandGenerator(enginePaths, engine, TargetOS);

        else
            gen = new NormalCommandGenerator(enginePaths, engine, TargetOS);

        gen.GenerateCmd();

        using (Process crossgen2 = new Process())
        {
            string[] fullCmdArgs = gen.GetCmd().Split(' ');

            var startInfo = new ProcessStartInfo
            {
                FileName = fullCmdArgs.FirstOrDefault(enginePaths.crossgen2exe),
                Arguments = string.Join(' ', fullCmdArgs.Skip(1)),
            };

            crossgen2.StartInfo = startInfo;
            crossgen2.Start();
            crossgen2.WaitForExit();
        }
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
