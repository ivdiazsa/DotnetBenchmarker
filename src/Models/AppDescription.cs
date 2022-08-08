// File: src/Models/AppDescription.cs
using System.Collections.Generic;

// Class: AppDescription
public class AppDescription
{
    public List<Runtime> Runtimes { get; set; }
    public List<Crossgen2> Crossgen2s { get; set; }
    public List<Configuration> Configurations { get; set; }
    public RunOptions? Options { get; set; }

    public AppDescription()
    {
        Runtimes = new List<Runtime>();
        Crossgen2s = new List<Crossgen2>();
        Configurations = new List<Configuration>();
    }
}
