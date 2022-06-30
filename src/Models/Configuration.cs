// File: src/Models/Configuration.cs
using System.IO;

// Class: Configuration
// Might be worth experimenting with an abstract base class, since both,
// Build and Run Phases Descriptors have very mirror-like functionality.
public class Configuration
{
    public string Name { get; set; }
    public string Os { get; set; }
    public string Scenario { get; set; }

    public BuildPhaseDescription BuildPhase { get; set; }
    public RunPhaseDescription RunPhase { get; set; }

    public string ProcessedAssembliesPath { get; set; }

    private string _buildResultsName;
    public string BuildResultsName
    { 
        get
        {
            if (!string.IsNullOrEmpty(_buildResultsName))
                return _buildResultsName;

            _buildResultsName = BuildPhase.FxResultName();
            return _buildResultsName;
        }
    }

    public Configuration()
    {
        Name = "unnamed-funny-configuration";
        Os = "linux";
        Scenario = "https://raw.githubusercontent.com/aspnet/Benchmarks/main/"
                    + "scenarios/plaintext.benchmarks.yml";
        BuildPhase = new BuildPhaseDescription();
        RunPhase = new RunPhaseDescription();

        ProcessedAssembliesPath = string.Empty;
        _buildResultsName = string.Empty;
    }
}
