// File: src/Components/OutputProcessor.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using ResultsDictionary = System.Collections.Generic.Dictionary<string,
    System.Collections.Generic.List<
        System.Collections.Generic.Dictionary<string, string>>>;

using IterationsList = System.Collections.Generic.List<
    System.Collections.Generic.Dictionary<string, string>>;

// Class: OutputProcessor
public class OutputProcessor
{
    private ResultsDictionary _runResults;
    private TextWriter _streamWriter;
    private BestTable<float>? _processedData;
    private string[] _formats;

    public OutputProcessor(string inputFile, string outputFile, string[] formats)
    {
        // We might want to save the processed numbers to a file, or simply
        // print them to the console.
        if (string.IsNullOrEmpty(outputFile))
            _streamWriter = Console.Out;
        else
            _streamWriter = new StreamWriter(outputFile);

        // Store the requested output formats given, in order to use them later.
        // If none are given, we will default to our table format.
        if (formats.IsEmpty())
            _formats = new string[] { "table" };
        else
            _formats = formats;

        string rawJson = File.ReadAllText(inputFile);
        _runResults = JsonSerializer.Deserialize<ResultsDictionary>(rawJson)!;
    }

    // Don't leak memory :)
    ~OutputProcessor()
    {
        _streamWriter.Close();
    }

    // This function will eventually support us passing it any arbitrary function
    // we might write for different ways of processing the data.
    public void ComputeReport(params string[] fieldsToFilter)
    {
        ExampleReportFunc(fieldsToFilter);
    }

    public void PrintToStream()
    {
        if (_processedData is null)
        {
            _streamWriter.WriteLine("\nThere was no data to table and print."
                                  + " Returning...\n");
            return ;
        }

        // Print the final results numbers :)
        DataFormatter<float> formatter;

        foreach (var item in _formats)
        {
            if (item.Equals("table", StringComparison.OrdinalIgnoreCase))
            {
                formatter = new TableFormatter<float>(_processedData);
                _streamWriter.Write("\n");
                _streamWriter.WriteLine(formatter.Draw());
            }

            if (item.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                formatter = new CsvFormatter<float>(_processedData);
                _streamWriter.Write("\n");
                _streamWriter.WriteLine(formatter.Draw());
            }
        }
    }

    public void ExampleReportFunc(params string[] fieldsToFilter)
    {
        // One row per configuration run.
        int rows = _runResults.Keys.Count;

        // One column per metric.
        int columns = fieldsToFilter.Length;

        // Will document this code later.
        _processedData = new BestTable<float>(rows, columns);
        int configNo = 0;

        foreach (KeyValuePair<string, IterationsList> configRun in _runResults)
        {
            string configName = configRun.Key;
            IterationsList iterList = configRun.Value;

            foreach (Dictionary<string, string> iter in iterList)
            for (int fIndex = 0; fIndex < fieldsToFilter.Length; fIndex++)
            {
                string metric = fieldsToFilter[fIndex];

                if (!iter.ContainsKey(metric))
                {
                    throw new KeyNotFoundException($"The requested metric {metric}"
                        + " was not found in the results JSON.");
                }

                float value = Convert.ToSingle(iter[metric].Replace(",", ""));
                _processedData[configNo, fIndex] += value;
            }

            for (int i = 0; i < _processedData.Columns; i++)
            {
                _processedData[configNo, i] /= iterList.Count;
            }

            configNo++;
        }

        _processedData.Headers = fieldsToFilter.Prepend("Configuration");
        _processedData.SideLabels = _runResults.Keys.ToArray();
    }
}

/*
    Configuration | Build Time | Start Time | Published Size
    my-configy-1  | Averages   | Averages   | Averages
    my-configy-2  | Averages   | Averages   | Averages
*/
