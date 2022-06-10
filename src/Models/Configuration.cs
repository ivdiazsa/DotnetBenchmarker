// File: src/Models/Configuration.cs

// Class: Configuration
public class Configuration
{
    public string Name { get; set; }
    public string Os { get; set; }
    public RunPhaseDescription RunPhase { get; set; }

    public Configuration()
    {
        Name = "unnamed-funny-configuration";
        Os = "linux";
        RunPhase = new RunPhaseDescription();
    }
}
