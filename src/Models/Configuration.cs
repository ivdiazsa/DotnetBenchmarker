// File: src/Models/Configuration.cs

// Class: Configuration
public class Configuration
{
    public string Name { get; set; }
    public string Os { get; set; }
    public RunEnvironmentDescription RunEnvironment { get; set; }

    public Configuration()
    {
        Name = "unnamed-funny-configuration";
        Os = "linux";
        RunEnvironment = new RunEnvironmentDescription();
    }
}
