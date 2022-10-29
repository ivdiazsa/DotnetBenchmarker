// File: src/components/CrankRunner.cs
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DotnetBenchmarker;

// Class: CrankRunner
public class CrankRunner
{
    private List<Configuration> _configurations;
    private int _iterations;
    private MultiIOLogger _logger;

    public CrankRunner(List<Configuration> configs, int iters = 1)
    {
        _configurations = configs;
        _iterations = iters;
        _logger = new MultiIOLogger($"{Constants.Paths.Logs}/"
                                  + $"run-log-{Constants.Timestamp}.txt");
    }

    public void Execute()
    {
        _logger.Write("\nStarting the Crank runs...\n");

        // In order to store the results for further analysis, we gotta keep
        // track of the relevant part of the output, and parse it into a JSON.
        var resultsHandler = new ResultsHandler();
        var outputKeep = new List<string>();

        for (int i = 0, total = _configurations.Count; i < total; i++)
        {
            var config = _configurations[i];
            string cmdArgs = GenerateCrankArgs(config);

            resultsHandler.ConfigName = config.Name;
            resultsHandler.ConfigAssembliesSize = config.ProcessedAssembliesSize;

            _logger.Write($"\nRunning configuration {config.Name} ({i+1}/{total})...\n");

            for (int j = 1; j <= _iterations; j++)
            {
                // Clean the list containing the previous iteration's output
                // results, so it can be reused for this next iteration.
                outputKeep.Clear();

                _logger.Write($"\nIteration {j}/{_iterations}...\n");
                _logger.Write($"\ncrank {cmdArgs}\n");

                using (Process crank = new Process())
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "crank",
                        Arguments = cmdArgs,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        UseShellExecute = false,
                    };

                    crank.StartInfo = startInfo;
                    crank.Start();

                    while (!crank.StandardOutput.EndOfStream)
                    {
                        string line = crank.StandardOutput.ReadLine()!;
                        _logger.Write($"{line}\n");
                        outputKeep.Add(line);
                    }
                    crank.WaitForExit();
                }
                resultsHandler.ParseAndStoreIterationResults(j, outputKeep);
            }
            resultsHandler.StoreConfigRunResults();
        }

        // Record this run's results in its JSON file.
        resultsHandler.SerializeToJSON();
    }

    private string GenerateCrankArgs(Configuration config,
                                     string appName = "application")
    {
        var cmdSb = new StringBuilder();
        string osCode = config.Os.Substring(0, 3).ToLower();

        cmdSb.AppendFormat(" --config {0}", config.ScenariosFile);
        cmdSb.AppendFormat(" --scenario {0}", config.Scenario);
        cmdSb.AppendFormat(" --profile aspnet-citrine-{0}", osCode);

        cmdSb.AppendFormat(" --{0}.framework {1}",
                            appName,
                            Constants.DotnetAppFramework);

        // If we have a run phase, then we've got to set those parameters to
        // crank as well.
        if (config.RunPhase is not null)
        {
            RunPhaseDescription runEnv = config.RunPhase;

            // The reason we're not closing the "" here, is because there may
            // still be other build arguments to append.
            cmdSb.AppendFormat(" --{0}.buildArguments \"-p:PublishReadyToRun={1}",
                                appName, runEnv.AppR2R.ToString());

            if (runEnv.AppAvx2)
            {
                cmdSb.Append(" -p:PublishReadyToRunCrossgen2ExtraArgs="
                            + "--instruction-set:avx2");
            }

            // Add any other '--.buildArguments' before this line, so they are
            // written within the quotes surrounding those arguments.
            cmdSb.Append("\"");

            cmdSb.AppendFormat(" --{0}.environmentVariables DOTNET_ReadyToRun={1}",
                               appName, runEnv.EnvReadyToRun ? "1" : "0");

            cmdSb.AppendFormat(" --{0}.environmentVariables DOTNET_TieredCompilation={1}",
                               appName, runEnv.EnvTieredCompilation ? "1" : "0");
        }

        cmdSb.AppendFormat(" --{0}.options.outputFiles \"{1}\"",
                           appName,
                           Path.Combine(config.ProcessedAssembliesPath, "*"));

        return cmdSb.ToString();
    }
}
