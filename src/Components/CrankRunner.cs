// File: src/Components/CrankRunner.cs
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

// Class: CrankRunner
public class CrankRunner
{
    // Class definition goes here.
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

        foreach (Configuration cfg in _configs)
        {
            string cmdArgs = GenerateCrankCmdArgs(cfg);
            _cranks.Add(new CrankRun(cmdArgs, cfg.Name));
        }
    }

    private void ExecuteRuns()
    {
        var outputKeep = new List<string>();
        var resultsParser = new ResultsParser();
        _logger.Write("\nStarting sending to crank...\n");

        for (int i = 0; i < _cranks.Count; i++)
        {
            CrankRun cr = _cranks[i];
            resultsParser.RunName = cr.Name;

            _logger.Write($"\nRunning config '{cr.Name}' ({i+1}/{_cranks.Count})...\n");
            _logger.Write($"\ncrank {cr.Args}\n");

            for (int j = 0; j < _iterations; j++)
            {
                outputKeep.Clear();
                _logger.Write($"\nIteration ({j+1}/{_iterations})...\n\n");

                using (Process crank = new Process())
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "crank",
                        Arguments = cr.Args,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        UseShellExecute = false
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
                resultsParser.ParseAndStoreIterationResults(j + 1, outputKeep);
            }
            resultsParser.StoreRunResults();
        }
        resultsParser.SerializeToJSON();
        _logger.Write("\nFinished with the tests!\n");
    }

    private string GenerateCrankCmdArgs(Configuration config,
                                        string appName = "application")
    {
        var cmdSb = new StringBuilder();
        string osCode = config.Os.Substring(0, 3);
        RunPhaseDescription runEnv = config.RunPhase;

        cmdSb.Append(" --config https://raw.githubusercontent.com/"
                   + "aspnet/Benchmarks/main/scenarios/plaintext.benchmarks.yml");

        cmdSb.Append(" --scenario plaintext");
        cmdSb.Append($" --profile aspnet-citrine-{osCode}");
        cmdSb.Append($" --{appName}.framework net7.0");

        cmdSb.Append($" --{appName}.buildArguments"
                   + $" -p:PublishReadyToRun={runEnv.AppR2R.ToString()}");

        if (config.BuildPhase.NeedsRecompilation())
        {
            cmdSb.Append($" --{appName}.options.outputFiles"
                       + $" {config.ProcessedAssembliesPath}/*");
        }

        cmdSb.Append($" --{appName}.environmentVariables"
                   + $" COMPlus_ReadyToRun={(runEnv.EnvReadyToRun ? "1" : "0")}");

        cmdSb.Append($" --{appName}.environmentVariables"
                   + $" COMPlus_TieredCompilation={(runEnv.EnvTieredCompilation ? "1" : "0")}");

        return cmdSb.ToString();
    }
}
