// File: src/components/AssembliesWorkshop.cs
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DotnetBenchmarker;

// Class: AssembliesWorkshop
public partial class AssembliesWorkshop
{
    private readonly Dictionary<string, AssembliesCollection> _assemblies;
    private readonly List<Configuration> _configurations;
    private readonly MultiIOLogger _logger;

    public AssembliesWorkshop(Dictionary<string, AssembliesCollection> asms,
                              List<Configuration> configs)
    {
        _assemblies = asms;
        _configurations = configs;
        _logger = new MultiIOLogger($"{Constants.Paths.Logs}/"
                                  + $"build-log-{Constants.Timestamp}.txt");
    }

    public void Run(bool rebuildFlag = false)
    {
        var retriever = new MaterialsRetriever();
        retriever.SearchAndFetch(_assemblies, _configurations, _logger);
        BuildReadyToRunImages(rebuildFlag);
    }

    private void BuildReadyToRunImages(bool rebuild)
    {
        for (int i = 0, total = _configurations.Count; i < total; i++)
        {
            var config = _configurations[i];
            var buildParams = config.BuildPhase;

            _logger.Write($"\n\nSetting up for configuration {config.Name}"
                        + $" ({i+1}/{total})...\n");

            if (buildParams is null)
            {
                _logger.Write("INFO: No Build Phase required for this"
                            + " configuration. Moving on to the next...\n");
                continue;
            }

            string outputFolder = Path.Combine(Constants.Paths.Resources,
                                               config.Os, "processed",
                                               $"{config.Name}-processed");

            if (Directory.Exists(outputFolder))
            {
                if (rebuild)
                {
                    // The user requested to build the configuration's binaries
                    // again, so we delete the existing ones (if any) and proceed
                    // normally.

                    _logger.Write("INFO: Binaries found for configuration"
                                + $" {config.Name}, and --rebuild flag was passed."
                                + " Deleting and processing again...\n");
                    Directory.Delete(outputFolder, true);
                }
                else
                {
                    // These configuration's binaries are already there, presumably
                    // from a previous run. We just let the user know and move on
                    // to the next configuration.

                    _logger.Write($"INFO: Configuration {config.Name} output"
                                + $" binaries found in {outputFolder}. Moving on"
                                + " to the next...\n");
                    config.ProcessedAssembliesPath = outputFolder;
                    continue;
                }
            }
            Directory.CreateDirectory(outputFolder);

            // Remember that the runtime binaries must match the target configuration,
            // while the crossgen2 binaries must match the running platform.

            AssembliesCollection targetOsAsms = _assemblies[config.Os];
            AssembliesCollection runningOsAsms = _assemblies[Constants.RunningOs];
            AssembliesNameLinks asmsToUseLinks = config.AssembliesToUse;

            string runtimePath =
                targetOsAsms.Runtimes
                            .Find(run => run.Name.Equals(asmsToUseLinks.Runtime))!
                            .Path;

            // Get the direct paths to the framework and asp.net assemblies within
            // the runtime folder.

            string fxPath = Path.GetDirectoryName(
                Directory.GetFiles(runtimePath, "System.Private.CoreLib.dll",
                                SearchOption.AllDirectories)
                        .FirstOrDefault(string.Empty)
            )!;

            string aspNetPath = Path.GetDirectoryName(
                Directory.GetFiles(runtimePath, "Microsoft.AspNetCore.dll",
                                SearchOption.AllDirectories)
                        .FirstOrDefault(string.Empty)
            )!;

            string crossgen2Path =
                runningOsAsms.Crossgen2s
                            .Find(cg2 => cg2.Name.Equals(asmsToUseLinks.Crossgen2))!
                            .Path;

            string crossgenApp = Path.Combine(crossgen2Path, "crossgen2");

            // Windows executables end in '.exe', while Unix ones don't have an
            // extension by default.
            if (Constants.RunningOs.Equals("windows"))
            {
                crossgenApp = Path.ChangeExtension(crossgenApp, ".exe");
            }

            string crossgenArgs = GenerateCrossgenArgs(config, outputFolder,
                                                       fxPath, aspNetPath,
                                                       crossgen2Path);

            _logger.Write($"\n{crossgenApp} {crossgenArgs}\n");
            RunCrossgen2(crossgenApp, crossgenArgs);

            // Our resulting build also needs the *.so/*.dylib libraries in the
            // cases of Linux/MacOS respectively, as well as any other untouched
            // binaries from the used runtime build.
            CopyRemainingFiles(config.Os, fxPath, aspNetPath, outputFolder);

            // Set the configuration's processed assemblies path to the
            // output folder we just used.
            config.ProcessedAssembliesPath = outputFolder;

            // Calculate the total size of the processed assemblies, and store
            // it to later save it to the results json file.
            config.ProcessedAssembliesSize = CalculateResultingCompositeSize(outputFolder);

        }
    }

    private string GenerateCrossgenArgs(Configuration config, string outputPath,
                                        string fxPath, string aspNetPath,
                                        string crossgen2Path)
    {
        var cmdSb = new StringBuilder();
        string compositeResultName = string.Empty;
        BuildPhaseDescription buildParams = config.BuildPhase!;

        // Target OS is defined in the configuration, and for the time being,
        // we only support targeting the x64 architecture. But do not fret!
        // Support for other platforms is in the works.
        cmdSb.AppendFormat("--targetos={0}", config.Os);
        cmdSb.Append(" --targetarch=x64");

        // Set whether we want to compile using AVX2.
        if (buildParams.UseAvx2)
        {
            _logger.Write("Will apply AVX2 instruction set...\n");
            cmdSb.Append(" --instruction-set=avx2");
        }

        // Set whether this will produce a composite image.
        if (buildParams.IsComposite())
            cmdSb.Append(" --composite");

        // Apply Standard Optimization Data if it's present. Otherwise, skip.
        if (File.Exists($"{crossgen2Path}/StandardOptimizationData.mibc"))
        {
            _logger.Write("Will use StandardOptimizationData.mibc\n");
            cmdSb.AppendFormat(" --mibc={0}", Path.Combine(crossgen2Path,
                                "StandardOptimizationData.mibc"));
        }

        // Framework Composites!
        if (buildParams.FrameworkComposite)
        {
            _logger.Write("\nCompiling Framework Composites...\n");
            compositeResultName += "framework";

            if (buildParams.IsFxPartial())
            {
                // Framework Partial Composites!
                DealWithAssembliesSubsets(cmdSb,
                                          buildParams.FxAssembliesSubset,
                                          fxPath,
                                          ref compositeResultName);
            }
            else
            {
                cmdSb.AppendFormat(" {0}", Path.Combine(fxPath, "*.dll"));
            }

            // Bundle ASP.NET for the Fx+Asp Composite!
            if (buildParams.BundleAspNet)
            {
                _logger.Write("\nASP.NET will be bundled into the composite image...\n");
                compositeResultName += "-aspnet";

                if (buildParams.IsAspPartial())
                {
                    // Bundled ASP.NET Partial Composites!
                    DealWithAssembliesSubsets(cmdSb,
                                              buildParams.AspAssembliesSubset,
                                              aspNetPath,
                                              ref compositeResultName);
                }
                else
                {
                    cmdSb.AppendFormat(" {0}", Path.Combine(aspNetPath, "*.dll"));
                }
            }
        }

        // ASP.NET Composites! Note this is mutually exclusive with bundling
        // with the framework. This has already been validated at the beginning.
        // If not, then that's a bug in the Validator we've got to take a look at.
        if (buildParams.AspNetComposite)
        {
            compositeResultName += "aspnetcore";
            _logger.Write("\nCompiling ASP.NET Composites...\n");

            if (buildParams.IsAspPartial())
            {
                // ASP.NET Partial Composites!
                DealWithAssembliesSubsets(cmdSb,
                                          buildParams.AspAssembliesSubset,
                                          aspNetPath,
                                          ref compositeResultName);
            }
            else
            {
                cmdSb.AppendFormat(" {0}", Path.Combine(aspNetPath, "*.dll"));
            }
            cmdSb.AppendFormat(" --reference={0}", Path.Combine(fxPath, "*.dll"));
        }

        // Specify the path where we want to output our new R2R images, and return :)
        cmdSb.AppendFormat(" --out={0}.r2r.dll",
                           Path.Combine(outputPath, compositeResultName));
        return cmdSb.ToString();
    }

    private void DealWithAssembliesSubsets(StringBuilder cmdSb,
                                           string subsetFile,
                                           string asmsPath,
                                           ref string compositeResultName)
    {
        // Read the provided file with the list of assemblies to process.
        string[] asmsToCompile = File.ReadAllLines(subsetFile);

        compositeResultName += "-partial";
        _logger.Write("\nRequested Partial Composites:\n");

        // Add each assembly from the file as an argument to Crossgen2. Then,
        // add a reference flag to the rest of the dll's, since they still have
        // to know about each other.
        foreach (string asm in asmsToCompile)
        {
            _logger.Write($"{asm}\n");
            cmdSb.AppendFormat(" {0}", Path.Combine(asmsPath, asm));
        }
        cmdSb.AppendFormat(" --reference={0}", Path.Combine(asmsPath, "*.dll"));
    }

    private void RunCrossgen2(string app, string args)
    {
        using (Process crossgen2 = new Process())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = app,
                Arguments = args,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
            };

            crossgen2.StartInfo = startInfo;
            crossgen2.Start();

            while (!crossgen2.StandardOutput.EndOfStream)
            {
                string line = crossgen2.StandardOutput.ReadLine()!;
                _logger.Write($"{line}\n");
            }
            crossgen2.WaitForExit();
        }
    }

    private void CopyRemainingFiles(string configOs, string fxPath, string aspPath,
                                    string outputPath)
    {
        // Experimenting with C#'s SQL-like syntax for listy programming :)
        // Find all the dll's that were not added to the output folder when doing
        // the crossgen'ing. When doing full composites, this list is expected
        // to be empty.
        List<string> filesToCopy =
            (from dll in Directory.GetFiles(fxPath, "*.dll")
                                 .Concat(Directory.GetFiles(aspPath, "*.dll"))
             where !File.Exists(Path.Combine(outputPath, Path.GetFileName(dll)))
             select dll).ToList();

        // Non-Windows platforms have their own libraries with their coreclr.
        // We have to copy them to the output folder, otherwise our composites
        // won't have their engine to run. Windows has its coreclr as another
        // dll, and therefore it's already included in the composite build.

        if (configOs.Equals("linux"))
            filesToCopy.AddRange(Directory.GetFiles(fxPath, "*.so"));
        else if (configOs.Equals("macos"))
            filesToCopy.AddRange(Directory.GetFiles(fxPath, "*.dylib"));

        foreach (string missing in filesToCopy)
        {
            string missingName = Path.GetFileName(missing);
            _logger.Write($"Copying {missingName}...\n");
            File.Copy(missing, Path.Combine(outputPath, missingName));
        }
    }

    private long CalculateResultingCompositeSize(string outputPath)
    {
        DirectoryInfo outputDi = new DirectoryInfo(outputPath);
        FileInfo[] outputDllsFis = outputDi.GetFiles("*.dll");
        return outputDllsFis.Sum(dll => dll.Length) / 1024;
    }
}
