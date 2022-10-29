// File: src/components/ResultsHandler.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using RunStats = System.Collections.Generic.List
                    <System.Collections.Generic.Dictionary<string, string>>;

using GlobalStats = System.Collections.Generic.Dictionary
                        <string, System.Collections.Generic.List
                            <System.Collections.Generic.Dictionary<string, string>>>;

namespace DotnetBenchmarker;

// Class: ResultsHandler
public class ResultsHandler
{
    public string ResultsFileName { get; }
    public string ConfigName { get; set; }
    public long ConfigAssembliesSize { get; set; }

    private RunStats _currentRunStats;
    private GlobalStats _allStats;

    public ResultsHandler()
    {
        ResultsFileName = Path.Combine(Constants.Paths.Results,
                                      $"results-{Constants.Timestamp}.json");
        ConfigName = string.Empty;
        ConfigAssembliesSize = 0;
        _currentRunStats = new RunStats();
        _allStats = new GlobalStats();
    }

    public void ParseAndStoreIterationResults(int iterNum, List<string> iterOutput)
    {
        var iterStats = new Dictionary<string, string>();
        iterStats.Add("Iteration", iterNum.ToString());

        // Skip(2) is to just pass the table header. This will most likely be
        // changed when we implement more complex data processing, involving more
        // tables and whatnot.
        IEnumerable<string[]> statsTable =
                iterOutput.SkipWhile(line => !line.StartsWith("|"))
                          .Skip(2)
                          .TakeWhile(line => !string.IsNullOrEmpty(line))
                          .Select(line => line.Split("|"));

        // Add each metric with their corresponding value to this iteration's
        // dictionary.
        foreach (string[] row in statsTable)
        {
            iterStats.Add(row[1].Trim(), row[2].Trim());
        }

        iterStats.Add("Assemblies Total Size (KB)", ConfigAssembliesSize.ToString());

        // Add this iteration's dictionary to the list of this configuration's
        // runs.
        _currentRunStats.Add(iterStats);
    }

    public void StoreConfigRunResults()
    {
        // Add the current configuration's results to the "global" dictionary,
        // where all the numbers are stored. The configuration name is used
        // as the key to know where these numbers came from.
        _allStats.Add(ConfigName, new RunStats(_currentRunStats));

        // Now that they are stored and persisting, we can clear this data
        // structure, so it can be used for the next configuration we run.
        _currentRunStats.Clear();
    }

    public void SerializeToJSON()
    {
        var jOptions = new JsonSerializerOptions { WriteIndented = true };
        string jString = JsonSerializer.Serialize(_allStats, jOptions);
        File.WriteAllText(ResultsFileName, jString);
    }
}
