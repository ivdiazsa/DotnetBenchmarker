// File: src/AppOptionsBank.cs
using System;
using System.Linq;

namespace DotnetBenchmarker;

// Class: AppOptionsBank
public class AppOptionsBank
{
    public bool Build { get; set; }
    public string ConfigFile { get; set; }
    public int Iterations { get; set; }
    public bool Run { get; set; }

    public AppDescription AppDesc { get; set; }

    public AppOptionsBank()
    {
        Build = false;
        ConfigFile = string.Empty;
        Iterations = 1;
        Run = false;
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

        // Environment.Exit(3);
        MatchAssembliesToConfigs();
    }

    public void ShowAppDescription() => Console.Write(AppDesc.ToString());

    // TODO: Move this to the AppDescription class.
    private void MatchAssembliesToConfigs()
    {
        foreach (Configuration item in AppDesc.Configurations)
        {
            AssembliesNameLinks links = item.AssembliesToUse;

            // In one of the simplest cases, we will have a configuration
            // targeting a different OS than the one we are running on, and
            // requesting a latest SDK build by omission. In this scenario,
            // there won't be assemblies specified for this configuration's
            // target OS, and that's perfectly fine.

            if (AppDesc.Assemblies.ContainsKey(item.Os))
            {
                AssembliesCollection asmsFromCfgOs = AppDesc.Assemblies[item.Os];
                if (string.IsNullOrEmpty(links.Runtime))
                {
                    var firstGivenRuntime = asmsFromCfgOs.Runtimes.FirstOrDefault()!;
                    if (firstGivenRuntime is null)
                        links.Runtime = "Latest";
                    else
                        links.Runtime = firstGivenRuntime.Name;
                }
            }

            if (string.IsNullOrEmpty(links.Crossgen2))
            {
                var asmsFromRunningOs = AppDesc.Assemblies[Constants.RunningOs];
                var firstGivenCrossgen2 = asmsFromRunningOs.Crossgen2s.FirstOrDefault()!;
                links.Crossgen2 = firstGivenCrossgen2.Name;
            }
        }
    }
}
