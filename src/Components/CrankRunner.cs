// File: src/Components/CrankRunner.cs
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

// Class: CrankRunner
public partial class CrankRunner
{
    private List<CrankRun> _cranks;
    private List<Configuration> _configs;
    private int _iterations;
    private MultiIOLogger _logger;

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
            _cranks.Add(new CrankRun(cmdArgs, cfg.Name));
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
            resultsParser.RunName = cr.Name;

            _logger.Write($"\nRunning config '{cr.Name}' ({i+1}/{_cranks.Count})...\n");
            _logger.Write($"\ncrank {cr.Args}\n");

            // Run the current configuration the required number of times.
            for (int j = 0; j < _iterations; j++)
            {
                outputKeep.Clear();
                _logger.Write($"\nIteration ({j+1}/{_iterations})...\n\n");

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
                resultsParser.ParseAndStoreIterationResults(j + 1, outputKeep);
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
        string osCode = config.Os.Substring(0, 3);
        RunPhaseDescription runEnv = config.RunPhase;

        cmdSb.Append($" --config {config.Scenario}");

        cmdSb.Append(" --scenario plaintext");
        cmdSb.Append($" --profile aspnet-citrine-{osCode}");
        cmdSb.Append($" --{appName}.framework net7.0");

        cmdSb.Append($" --{appName}.buildArguments"
                   + $" -p:PublishReadyToRun={runEnv.AppR2R.ToString()}");

        // Since the main use case of this app is to compare different benchmarks,
        // we should always start from the same sources. If processing was done,
        // then upload those assemblies to crank. If not, then upload the normal
        // assemblies from your build acquired elsewhere.

        if (config.BuildPhase.NeedsRecompilation())
        {
            cmdSb.Append($" --{appName}.options.outputFiles"
                       + $" {config.ProcessedAssembliesPath}/*");
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

                cmdSb.Append($" --{appName}.options.outputFiles {path}/*");
            }
        }

        cmdSb.Append($" --{appName}.environmentVariables"
                   + $" COMPlus_ReadyToRun={(runEnv.EnvReadyToRun ? "1" : "0")}");

        cmdSb.Append($" --{appName}.environmentVariables"
                   + $" COMPlus_TieredCompilation={(runEnv.EnvTieredCompilation ? "1" : "0")}");

        return cmdSb.ToString();
    }
}
