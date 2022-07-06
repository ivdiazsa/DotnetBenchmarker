// File: src/AppOptionsBank.cs
using System;
using System.Collections.Generic;

// Class: AppOptionsBank
public class AppOptionsBank
{
    public int Iterations { get; set; }
    public bool BuildOnly { get; set; }
    public string ConfigFile { get; set; }
    public string OutputFile { get; set; }
    public string[] OutputFormat { get; set; }
    public AppDescription AppDesc { get; set; }

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

        if (String.IsNullOrEmpty(ConfigFile))
        {
            Console.WriteLine("A YAML configuration file is needed to know what"
                              + " to build and run.");
            Environment.Exit(-1);
        }

        YamlConfigFileParser.ParseIntoOptsBank(this);
    }

    public Dictionary<string, Runtime> GetRuntimesGroupedByOS()
    {
        var runtimesByOs = new Dictionary<string, Runtime>();

        foreach (Runtime r in AppDesc.Runtimes)
        {
            runtimesByOs.Add(r.Os, r);
        }

        return runtimesByOs;
    }

    public Dictionary<string, Crossgen2> GetCrossgen2sGroupedByOS()
    {
        var crossgen2sByOs = new Dictionary<string, Crossgen2>();

        foreach (Crossgen2 c in AppDesc.Crossgen2s)
        {
            crossgen2sByOs.Add(c.Os, c);
        }

        return crossgen2sByOs;
    }

    public List<Configuration> GetConfigurations()
    {
        return AppDesc.Configurations;
    }
}
