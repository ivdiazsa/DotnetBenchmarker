// File: src/AppOptionsBank.cs
using System;

namespace DotnetBenchmarker;

// Class: AppOptionsBank
public class AppOptionsBank
{
    public bool Build { get; set; }
    public string ConfigFile { get; set; }
    public int Iterations { get; set; }
    public bool Run { get; set; }

    public AppOptionsBank()
    {
        Build = false;
        ConfigFile = string.Empty;
        Iterations = 1;
        Run = false;
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

        // YamlConfigFileParser.ParseIntoOptsBank(this);
    }

    public void CmdLines()
    {
        System.Console.WriteLine(Build);
        System.Console.WriteLine(ConfigFile);
        System.Console.WriteLine(Iterations);
        System.Console.WriteLine(Run);
    }
}
