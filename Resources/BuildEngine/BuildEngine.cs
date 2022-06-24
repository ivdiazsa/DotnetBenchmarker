// File: BuildEngine.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// Class: BuildEngine
// This little buddy will process and generate all composite or non-composite
// assemblies as requested :)
internal partial class BuildEngine
{
    static readonly Dictionary<PlatformID, string> OSMap =
        new Dictionary<PlatformID, string>()
        {
            { PlatformID.Win32NT, "Windows" },
            { PlatformID.Unix, "Linux" }
        };

    static readonly string TargetOS = OSMap[Environment.OSVersion.Platform];
    static readonly string OSCode = TargetOS.Substring(0, 3).ToLower();

    private struct BinsPaths
    {
        public string output { get; }
        public string dotnet { get; }
        public string fx { get; }
        public string asp { get; }
        public string crossgen2exe { get; }

        public BinsPaths(string outputPath, string dotnetPath, string fxPath,
                         string aspPath, string crossgen2Path)
        {
            output = outputPath;
            dotnet = dotnetPath;
            fx = fxPath;
            asp = aspPath;
            crossgen2exe = Environment.OSVersion.Platform == PlatformID.Win32NT
                           ? crossgen2Path + ".exe"
                           : crossgen2Path;
        }
    }

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

        BinsPaths enginePaths = GetPathsSet(engine);
        StringBuilder cmdArgsSb = DefineBaseDefaultArgs(engine);

        if (engine.FrameworkComposite)
        {
            Console.WriteLine("\nCompiling Framework Composites...");
            string label = "framework";
            DefineCompositeArgs(engine, cmdArgsSb, ref label);
        }
    }

    private static BinsPaths GetPathsSet(EngineEnvironment engEnv)
    {
        string dotnetRootPath = $"Dotnet{TargetOS}/dotnet{engEnv.DotnetVersionNumber}";

        return new BinsPaths(
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

    private static StringBuilder DefineBaseDefaultArgs(EngineEnvironment engEnv)
    {
        var defaultArgs = new StringBuilder();
        defaultArgs.AppendFormat(" --targetos {0}", TargetOS);
        defaultArgs.Append(" --targetarch x64");

        if (engEnv.UseAvx2)
        {
            Console.WriteLine("\nWill apply AVX2 Instruction Set...");
            defaultArgs.Append(" --instruction-set avx2");
        }

        // NOTE: Might be asked for in the future to support using an arbitrary
        //       number of mibc optimization files, and/or with different names.
        if (File.Exists($"Crossgen2{TargetOS}/StandardOptimizationData.mibc"))
        {
            Console.WriteLine("Will use StandardOptimizationData.mibc...");
            defaultArgs.AppendFormat(" --mibc Crossgen2{0}/StandardOptimizationData.mibc",
                                     TargetOS);
        }

        return defaultArgs;
    }

    private static void DefineCompositeArgs(EngineEnvironment engEnv,
                                            StringBuilder argsSb,
                                            ref string compositeFileLabel)
    {
        argsSb.Append(" --composite");
        return ;
    }

    private static void EnlistPartialCompositeAssemblies(EngineEnvironment engEnv,
                                                         StringBuilder argsSb)
    {
        return ;
    }
}
