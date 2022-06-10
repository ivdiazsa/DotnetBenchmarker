// File: src/AppOptionsBank.cs
using System;

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
}
