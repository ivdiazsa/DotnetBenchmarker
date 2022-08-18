// File: src/Components/CrankRunner.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

// Class: CrankRunner
public partial class CrankRunner
{
    private List<CrankRun> _cranks;
    private List<Configuration> _configs;
    private int _iterations;
    private MultiIOLogger _logger;

    private const int KB_SIZE = 1024;

    public CrankRunner(List<Configuration> configs, int numIters = 1)
    {
        _cranks = new List<CrankRun>();
        _configs = configs;
        _iterations = numIters;
        _logger = new MultiIOLogger($"{Constants.LogsPath}/run-log-{Constants.Timestamp}.txt");
    }

    public void Execute()
    {
        PrepareRuns();
        ExecuteRuns();
    }

    private void PrepareRuns()
    {
        _logger.Write("\nCreating Crank objects from the configurations received...\n");

        // Simply create the little objects with the crank command to run for
        // each configuration.
        foreach (Configuration cfg in _configs)
        {
            string cmdArgs = GenerateCrankCmdArgs(cfg);
            _cranks.Add(new CrankRun(cmdArgs, cfg.Name, cfg.ProcessedAssembliesPath));
        }
    }

    private void ExecuteRuns()
    {
        // In order to store the results for further analysis, we gotta keep
        // track of the relevant part of the output, and parse it into a JSON.
        // See Parsers/ResultsParser.cs for details on implementation of this.

        var outputKeep = new List<string>();
        var resultsParser = new ResultsParser();
        _logger.Write("\nStarting sending to crank...\n");

        // Iterate each configuration.
        for (int i = 0; i < _cranks.Count; i++)
        {
            CrankRun cr = _cranks[i];
            double outputSize = CalculateAssembliesSize(cr.OutputFiles);
            resultsParser.RunName = cr.Name;

            _logger.Write($"\n\nRunning config '{cr.Name}' ({i+1}/{_cranks.Count})...\n");

            // Run the current configuration the required number of times.
            for (int j = 0; j < _iterations; j++)
            {
                outputKeep.Clear();
                cr.UpdateTraceIndexIfExists(j, j+1);

                _logger.Write($"\n\nIteration ({j+1}/{_iterations})...\n");
                _logger.Write($"\ncrank {cr.Args}\n");

                using (Process crank = new Process())
                {
                    var startInfo = new ProcessStartInfo();
                    crank.StartInfo = startInfo.BaseTemplate("crank", cr.Args);
                    crank.Start();

                    while (!crank.StandardOutput.EndOfStream)
                    {
                        string line = crank.StandardOutput.ReadLine()!;
                        _logger.Write($"{line.CleanControlChars()}\n");
                        outputKeep.Add(line);
                    }
                    crank.WaitForExit();
                }
                resultsParser.ParseAndStoreIterationResults(j + 1, outputSize,
                                                            outputKeep);
            }
            resultsParser.StoreRunResults();
        }

        // Record the run's results in its JSON file.
        resultsParser.SerializeToJSON();
        _logger.Write("\nFinished with the tests!\n");
    }

    private string GenerateCrankCmdArgs(Configuration config,
                                        string appName = "application")
    {
        var cmdSb = new StringBuilder();
        string osCode = config.Os.Substring(0, 3).ToLower();
        RunPhaseDescription runEnv = config.RunPhase;

        cmdSb.AppendFormat(" --config {0}", config.ScenarioFile);

        cmdSb.AppendFormat(" --scenario {0}", config.Scenario);
        cmdSb.AppendFormat(" --profile aspnet-citrine-{0}", osCode);
        cmdSb.AppendFormat(" --{0}.framework net7.0", appName);

        cmdSb.AppendFormat(" --{0}.buildArguments"
                         + " -p:PublishReadyToRun={1}",
                           appName,
                           runEnv.AppR2R.ToString());

        cmdSb.AppendFormat(" --{0}.buildArguments"
                         + " -p:PublishReadyToRunComposite={1}",
                           appName,
                           config.BuildPhase.FullComposite.ToString());

        if (config.BuildPhase.UseAvx2)
        {
            cmdSb.AppendFormat(" --{0}.buildArguments"
                             + " -p:PublishReadyToRunCrossgen2ExtraArgs="
                             + "--instruction-set:avx2",
                               appName);
        }

        // Since the main use case of this app is to compare different benchmarks,
        // we should always start from the same sources. If processing was done,
        // then upload those assemblies to crank. If not, then upload the normal
        // assemblies from your build acquired elsewhere.

        if (config.BuildPhase.NeedsRecompilation())
        {
            cmdSb.AppendFormat(" --{0}.options.outputFiles {1}/*",
                               appName, config.ProcessedAssembliesPath);
        }
        else
        {
            // In the case of using a nightly build, the runtime and the asp
            // binaries are located in different paths. We get those here through
            // a string separated with a semi-colon(;). This also applies if you
            // provided your own binaries and have them in different spots.
            string[] defaultAssemblies = config.ProcessedAssembliesPath.Split(';');
            foreach (string path in defaultAssemblies)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                cmdSb.AppendFormat(" --{0}.options.outputFiles {1}/*",
                                   appName, path);
            }
        }

        if (config.Options is not null)
        {
            // Process the additional options here.
            RunOptions opts = config.Options;
            if (opts.Trace is not null)
            {
                var traceOpts = opts.Trace;
                string tracingApp = osCode switch
                {
                    "lin" => "dotnetTrace",
                    "win" => "collect",
                    _ => throw new PlatformNotSupportedException(),
                };

                cmdSb.AppendFormat(" --{0}.{1} true", appName, tracingApp);

                if (osCode.Equals("lin") && !traceOpts.TraceProviders.IsEmpty())
                {
                    cmdSb.AppendFormat(" --{0}.dotnetTraceProviders {1}",
                                       appName, string.Join(',', traceOpts.TraceProviders));
                }
                else if (osCode.Equals("win") && !traceOpts.CollectArgs.IsEmpty())
                {
                    cmdSb.AppendFormat(" --{0}.collectArguments {1}",
                                       appName, string.Join(',', traceOpts.CollectArgs));
                }

                cmdSb.AppendFormat(" --{0}.options.traceOutput {1}-{2}-0",
                                   appName,
                                   traceOpts.OutputName,
                                   config.Name);
                cmdSb.AppendFormat(" --{0}.options.collectCounters true", appName);
            }
        }

        cmdSb.AppendFormat(" --{0}.environmentVariables COMPlus_ReadyToRun={1}",
                           appName, runEnv.EnvReadyToRun ? "1" : "0");

        cmdSb.AppendFormat(" --{0}.environmentVariables COMPlus_TieredCompilation={1}",
                           appName, runEnv.EnvTieredCompilation ? "1" : "0");

        return cmdSb.ToString();
    }

    private double CalculateAssembliesSize(string assembliesPath)
    {
        double totalSize = 0.0;

        // In the case of scenarios where no recompilation is needed, we will
        // be using the given binaries as is, which includes the case of the
        // official builds, which have a specific layout. In this case, we will
        // have more than one path with assemblies, and so we have to consider
        // them all for the total size we plan to return.
        List<FileInfo> files = new List<FileInfo>(
            assembliesPath.Split(";").Select(path => new DirectoryInfo(path))
                                     .Select(dir => dir.GetFiles())
                                     .SelectMany(fi => fi)
        );

        foreach (FileInfo fi in files)
        {
            double fiSize = (double)fi.Length / (double)KB_SIZE;
            totalSize += fiSize;
        }
        return totalSize;
    }
}
