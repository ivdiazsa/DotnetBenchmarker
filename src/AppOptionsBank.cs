// File: src/AppOptionsBank.cs
using System;
using System.Collections.Generic;
using System.Linq;

// Class: AppOptionsBank
public class AppOptionsBank
{
    public int Iterations { get; set; }
    public bool BuildOnly { get; set; }
    public string ConfigFile { get; set; }
    public string OutputFile { get; set; }
    public string[] OutputFormat { get; set; }
    public AppDescription AppDesc { get; set; }

    private Dictionary<string, Runtime>? _runtimesByOs;
    private Dictionary<string, Crossgen2>? _crossgen2sByOs;

    public AppOptionsBank()
    {
        Iterations = 1;
        BuildOnly = false;
        ConfigFile = String.Empty;
        OutputFile = string.Empty;
        OutputFormat = Array.Empty<string>();
        AppDesc = new AppDescription();
    }

    public void Init(string[] args)
    {
        CommandLineParser.ParseIntoOptsBank(this, args);

        if (string.IsNullOrEmpty(ConfigFile))
        {
            Console.WriteLine("A YAML configuration file is needed to know what"
                              + " to build and run. Use the --config-file to"
                              + " specify it through the command-line.");
            Environment.Exit(-1);
        }

        YamlConfigFileParser.ParseIntoOptsBank(this);
    }

    public Dictionary<string, Runtime> GetRuntimesGroupedByOS()
    {
        // Just checking if this has been calculated before to avoid doing
        // duplicated work :)
        if (_runtimesByOs is not null)
            return _runtimesByOs;

        var runtimesByOs = new Dictionary<string, Runtime>();

        foreach (Runtime r in AppDesc.Runtimes)
        {
            runtimesByOs.Add(r.Os, r);
        }

        _runtimesByOs = runtimesByOs;
        return runtimesByOs;
    }

    public Dictionary<string, Crossgen2> GetCrossgen2sGroupedByOS()
    {
        // Just checking if this has been calculated before to avoid doing
        // duplicated work :)
        if (_crossgen2sByOs is not null)
            return _crossgen2sByOs;

        var crossgen2sByOs = new Dictionary<string, Crossgen2>();

        foreach (Crossgen2 c in AppDesc.Crossgen2s)
        {
            crossgen2sByOs.Add(c.Os, c);
        }

        // If a runtime repo is passed, then that os' respective crossgen2 entry
        // in the configuration file can be omitted, since we can infer it. So,
        // if we are missing a crossgen2, then we will use the given repoPath in
        // the runtimes section. If there is no corresponding repo, then we will
        // just skip this and let CompositesBuilder do the graceful failing :)
        if (_runtimesByOs!.Keys.Count > crossgen2sByOs.Keys.Count)
        {
            GetCrossgen2FromRepo(
                crossgen2sByOs,
                _runtimesByOs!.Where(r => !crossgen2sByOs.ContainsKey(r.Key))
                              .ToDictionary(r => r.Key, r => r.Value)
                              .Values
            );
        }

        _crossgen2sByOs = crossgen2sByOs;
        return crossgen2sByOs;
    }

    public List<Configuration> GetConfigurations()
    {
        return AppDesc.Configurations;
    }

    private void GetCrossgen2FromRepo(Dictionary<string, Crossgen2> cg2s,
                                      IEnumerable<Runtime> toSearch)
    {
        foreach (Runtime item in toSearch)
        {
            if (string.IsNullOrEmpty(item.RepoPath))
                continue;

            // If we got here, then it means we want to use the crossgen2 build
            // from the provided runtime repo. Find the path to it, and record
            // it accordingly, so it can be used later on.

            string os = item.Os.Equals("linux", StringComparison.OrdinalIgnoreCase)
                        ? item.Os.Capitalize()
                        : item.Os;

            string repoCrossgenPath = $"{item.RepoPath}/{Constants.RuntimeRepoCoreclrPath}/"
                                    + $"{os}.x64.Release/crossgen2";
            cg2s.Add(item.Os, new Crossgen2 { Os=item.Os, Path=repoCrossgenPath });
        }
    }
}
