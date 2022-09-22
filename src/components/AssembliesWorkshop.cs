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

    public void Run()
    {
        var retriever = new MaterialsRetriever();
        retriever.SearchAndFetch(_assemblies, _configurations, _logger);
        BuildReadyToRunImages();
    }

    private void BuildReadyToRunImages()
    {
        for (int i = 0, total = _configurations.Count; i < total; i++)
        {
            var config = _configurations[i];
            var buildParams = config.BuildPhase;

            _logger.Write($"\n\nSetting up for configuration {config.Name} ({i+1}/{total})...\n");

            if (buildParams is null)
            {
                _logger.Write("INFO: No Build Phase required for this"
                            + " configuration. Moving on to the next...\n");
                continue;
            }

            // These configuration's binaries are already there, presumably
            // from a previous run. We just let the user know and move on to
            // the next configuration.
            string outputFolder = Path.Combine(Constants.Paths.Resources,
                                               config.Os, "processed",
                                               $"{config.Name}-processed");

            if (Directory.Exists(outputFolder))
            {
                _logger.Write($"INFO: Configuration {config.Name} output binaries"
                            + $" found in {outputFolder}. Moving on to the next...\n");
                config.ProcessedAssembliesPath = outputFolder;
                continue;
            }
            else
            {
                Directory.CreateDirectory(outputFolder);
            }

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

            // Non-Windows platforms have their own libraries with their coreclr.
            // We have to copy them to the output folder, otherwise our composites
            // won't have their engine to run. Windows has its coreclr as another
            // dll, and therefore it's already included in the composite build.

            if (config.Os.Equals("linux"))
            {
                // Copy all the .so files to the output folder.
                string[] soFiles = Directory.GetFiles(fxPath, "*.so",
                                                      SearchOption.TopDirectoryOnly);

                foreach (string so in soFiles)
                {
                    string soName = Path.GetFileName(so);
                    _logger.Write($"Copying {soName}...\n");
                    File.Copy(so, Path.Combine(outputFolder, soName));
                }
            }
            else if (config.Os.Equals("macos"))
            {
                // Copy all the .dylib files to the output folder.
                string[] dylibFiles = Directory.GetFiles(fxPath, "*.dylib",
                                                         SearchOption.TopDirectoryOnly);

                foreach (string dylib in dylibFiles)
                {
                    string dylibName = Path.GetFileName(dylib);
                    _logger.Write($"Copying {dylibName}...\n");
                    File.Copy(dylib, Path.Combine(outputFolder, dylibName));
                }
            }

            // Set the configuration's processed assemblies path to the
            // output folder we just used.
            config.ProcessedAssembliesPath = outputFolder;
        }
    }

    // TODO: This function is meant to do any sort of Crossgen2 processing, but
    // currently, only composites are supported due to urgency. Making support
    // more general is one of the highest priority work items at the moment.
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
            _logger.Write("Compiling Framework Composites...\n");
            compositeResultName += "framework";
            cmdSb.AppendFormat(" {0}", Path.Combine(fxPath, "*.dll"));

            // Bundle ASP.NET for the full Fx+Asp Composite!
            if (buildParams.BundleAspNet)
            {
                _logger.Write("ASP.NET will be bundled into the composite image...\n");
                compositeResultName += "-aspnet";
                cmdSb.AppendFormat(" {0}", Path.Combine(aspNetPath, "*.dll"));
            }
        }

        // ASP.NET Composites! Note this is mutually exclusive with bundling
        // with the framework. This has already been validated at the beginning.
        // If not, then that's a bug in the Validator we've got to take a look at.
        if (buildParams.AspNetComposite)
        {
            compositeResultName += "aspnetcore";
            _logger.Write("Compiling ASP.NET Composites...\n");
            cmdSb.AppendFormat(" {0}", Path.Combine(aspNetPath, "*.dll"));
            cmdSb.AppendFormat(" --reference={0}", Path.Combine(fxPath, "*.dll"));
        }

        // Specify the path where we want to output our new R2R images, and return :)
        cmdSb.AppendFormat(" --out={0}.r2r.dll",
                           Path.Combine(outputPath, compositeResultName));
        return cmdSb.ToString();
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
}
