// File: src/Models/Configurations/AssembliesNameLinks.cs

namespace DotnetBenchmarker;

// Class: AssembliesNameLinks
public class AssembliesNameLinks
{
    public string Processed { get; set; }
    public string Runtime { get; set; }
    public string Crossgen2 { get; set; }

    public AssembliesNameLinks()
    {
        Processed = string.Empty;
        Runtime = string.Empty;
        Crossgen2 = string.Empty;
    }

    public override string ToString()
    {
        return $"Paths to Assemblies to be Used:\n"
            + $"  Processed and Ready: {Processed.DefaultIfEmpty("None Specified")}\n"
            + $"  Runtime: {Runtime.DefaultIfEmpty("Latest")}\n"
            + $"  Crossgen2: {Crossgen2.DefaultIfEmpty("None Specified")}";
    }
}
