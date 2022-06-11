// File: src/Models/Configuration.cs

// Class: Configuration
// Might be worth experimenting with an abstract base class, since both,
// Build and Run Phases Descriptors have very mirror-like functionality.
public class Configuration
{
    public string Name { get; set; }
    public string Os { get; set; }
    public BuildPhaseDescription BuildPhase { get; set; }
    public RunPhaseDescription RunPhase { get; set; }

    public Configuration()
    {
        Name = "unnamed-funny-configuration";
        Os = "linux";
        BuildPhase = new BuildPhaseDescription();
        RunPhase = new RunPhaseDescription();
    }
}
