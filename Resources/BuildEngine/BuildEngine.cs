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
        return ;
    }

    // private static void ProcessNonComposite(AppPaths enginePaths,
    //                                         EngineEnvironment engine)
    // {
    //     var gen = new NormalCommandGenerator(enginePaths, engine, TargetOS);
    //     var runtimeAssemblies = new List<string>();
    //     var aspnetAssemblies = new List<string>();

    //     // Here, we're going to add later some flags to define whether the user
    //     // wants to process fx and/or aspnet assemblies. For now, we will go the
    //     // safe route and process all those we can find.

    //     if (Directory.Exists(enginePaths.fx))
    //     {
    //         runtimeAssemblies.AddRange(Directory.GetFiles(enginePaths.fx, "*.dll"));
    //     }

    //     if (Directory.Exists(enginePaths.asp))
    //     {
    //         aspnetAssemblies.AddRange(Directory.GetFiles(enginePaths.asp, "*.dll"));
    //     }

    //     foreach (string assembly in runtimeAssemblies)
    //     {
    //         System.Console.WriteLine(assembly);
    //         gen.GenerateCmd(assembly, runtimeAssemblies);
    //         System.Console.WriteLine($"\n{gen.GetCmd()}\n");
    //         RunCrossgen2(gen.GetCmd(), enginePaths.crossgen2exe);
    //     }

    //     // For ASP.NET, we need to reference both, their fellow aspnet binaries,
    //     // as well as the runtime ones. That's the reason we are joining both
    //     // lists in the runtime assemblies one.
    //     runtimeAssemblies.AddRange(aspnetAssemblies);

    //     foreach (string assembly in aspnetAssemblies)
    //     {
    //         gen.GenerateCmd(assembly, runtimeAssemblies);
    //         RunCrossgen2(gen.GetCmd(), enginePaths.crossgen2exe);
    //     }
    // }

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
}
