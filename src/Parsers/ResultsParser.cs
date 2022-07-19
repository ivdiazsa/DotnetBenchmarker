// File: src/Parsers/ResultsParser.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using RunStats = System.Collections.Generic.List
                    <System.Collections.Generic.Dictionary<string, string>>;

using GlobalStats = System.Collections.Generic.Dictionary
                        <string, System.Collections.Generic.List
                            <System.Collections.Generic.Dictionary<string, string>>>;

// Class: ResultsParser
public class ResultsParser
{
    public string ResultsFilename { get; }
    public string RunName { get; set; }

    private RunStats _currentRunStats;
    private GlobalStats _allStats;

    public ResultsParser()
    {
        if (!Directory.Exists(Constants.ResultsPath))
            Directory.CreateDirectory(Constants.ResultsPath);

        ResultsFilename = $"{Constants.ResultsPath}/results-{Constants.Timestamp}.json";
        RunName = string.Empty;
        _currentRunStats = new RunStats();
        _allStats = new GlobalStats();
    }

    public void ParseAndStoreIterationResults(int iterNum, double assembliesSize,
                                              List<string> iterOutput)
    {
        var iterStats = new Dictionary<string, string>();
        iterStats.Add("Iteration", iterNum.ToString());
        iterStats.Add("Assemblies Size (KB)", assembliesSize.ToString());

        // Skip(2) is to just pass the table header. This will most likely be
        // changed when we implement more complex data processing, involving more
        // tables and whatnot.
        List<string[]> statsTable = iterOutput.SkipWhile(line => !line.StartsWith("|"))
                                              .Skip(2)
                                              .TakeWhile(line => !string.IsNullOrEmpty(line))
                                              .Select(line => line.Split("|"))
                                              .ToList();

        // Add each metric with their corresponding value to this iteration's
        // dictionary.
        foreach (string[] row in statsTable)
        {
            iterStats.Add(row[1].Trim(), row[2].Trim());
        }

        // Add this iteration's dictionary to the list of this configuration's
        // runs.
        _currentRunStats.Add(iterStats);
    }

    public void StoreRunResults()
    {
        // Add the current configuration's results to the "global" dictionary,
        // where all the numbers are stored. The configuration name is used
        // as the key to know where these numbers came from.
        _allStats.Add(RunName, new RunStats(_currentRunStats));
        _currentRunStats.Clear();
    }

    public void SerializeToJSON()
    {
        var jOptions = new JsonSerializerOptions { WriteIndented = true };
        string jString = JsonSerializer.Serialize(_allStats, jOptions);
        File.WriteAllText(ResultsFilename, jString);
    }
}
