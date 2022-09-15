// File: src/Models/Configurations/Configuration.cs
using System.Collections.Generic;

namespace DotnetBenchmarker;

// Class: Configuration
public class Configuration
{
    public string Name { get; set; }
    public string Os { get; set; }
    public string ScenariosFile { get; set; }
    public string Scenario { get; set; }

    public DotnetVersions Versions { get; set; }
    public AssembliesNameLinks AssembliesToUse { get; set; }
    public BuildPhaseDescription? BuildPhase { get; set; }
    public RunPhaseDescription? RunPhase { get; set; }

    public Configuration()
    {
        Name = "unnamed-funny-configuration";
        Os = "none";
        ScenariosFile = "https://raw.githubusercontent.com/aspnet/Benchmarks/main/"
                        + "scenarios/plaintext.benchmarks.yml";
        Scenario = "plaintext";

        Versions = new DotnetVersions();
        AssembliesToUse = new AssembliesNameLinks();
    }

    public override string ToString()
    {
        return $"Configuration Description:\n"
            + $"\nName: {Name}\n"
            + $"Target OS: {Os}\n"
            + $"{Versions.ToString()}\n"
            + $"{AssembliesToUse.ToString()}\n"
            + $"Scenarios File: {ScenariosFile}\n"
            + $"Scenario: {Scenario}\n"
            + $"\n{(BuildPhase is null ? "No build phase." : BuildPhase.ToString())}\n"
            + $"\n{(RunPhase is null ? "No run phase." : RunPhase.ToString())}";
    }
}
