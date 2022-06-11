// File: src/AppOptionsBank.cs
using System;
using System.Collections.Generic;

// Class: AppOptionsBank
public class AppOptionsBank
{
    public string ConfigFile { get; set; }
    public int Iterations { get; set; }
    public AppDescription AppDesc { get; set; }

    public AppOptionsBank()
    {
        ConfigFile = String.Empty;
        Iterations = 1;
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

    public List<Configuration> GetConfigurations()
    {
        return AppDesc.Configurations;
    }
}
