// File: src/components/CrankRunner.cs
using System.Collections.Generic;
using System.Diagnostics;
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

        for (int i = 0, total = _configurations.Count; i < total; i++)
        {
            var config = _configurations[i];
            string cmdArgs = GenerateCrankArgs();

            _logger.Write($"\nRunning configuration {config.Name} ({i+1}/{total})...\n");

            for (int j = 1; j <= _iterations; j++)
            {
                _logger.Write($"\nIteration {i}/{_iterations}...\n");
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
                    }
                    crank.WaitForExit();
                }
            }
            continue;
        }
        return ;
    }

    private string GenerateCrankArgs()
    {
        var cmdSb = new StringBuilder();
        return cmdSb.ToString();
    }
}
