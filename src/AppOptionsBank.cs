// File: src/AppOptionsBank.cs
using System;
using System.Collections.Generic;

namespace DotnetBenchmarker;

// Class: AppOptionsBank
public class AppOptionsBank
{
    public bool BuildOnly { get; set; }
    public string ConfigFile { get; set; }
    public int Iterations { get; set; }
    public bool Rebuild { get; set; }

    public AppDescription AppDesc { get; set; }

    public AppOptionsBank()
    {
        BuildOnly = false;
        ConfigFile = string.Empty;
        Iterations = 1;
        Rebuild = false;
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

        if (Iterations < 1)
        {
            Console.WriteLine($"We can't run {Iterations} iterations. Setting"
                            + " to the default value of 1.");
            Iterations = 1;
        }

        YamlConfigFileParser.ParseIntoOptsBank(this);

        // Make sure that all data is in order. This is to guarantee (as much as
        // we can) that the app later on will work as expected. Catching failures
        // early on is much better than failing unexpectedly later, potentially
        // after some time of execution and leaving resources dangling.

        if (Validator.ValidateAll(AppDesc))
        {
            Console.WriteLine("\nEverything's in order. Running the app now :)");
        }
        else
        {
            Console.WriteLine("\nErrors described above. Exiting now...\n");
            Environment.Exit(-2);
        }

        AppDesc.MatchAssembliesToConfigs();
    }

    public void ShowAppDescription() => Console.Write(AppDesc.ToString());

    public Dictionary<string, AssembliesCollection> GetAssemblies()
    {
        return AppDesc.Assemblies;
    }

    public List<Configuration> GetConfigurations()
    {
        return AppDesc.Configurations;
    }
}
