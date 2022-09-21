// File: src/Models/AssembliesDescription.cs
using System.Collections.Generic;
using System.Text;

namespace DotnetBenchmarker;

// Class: AssembliesDescription
public class AssembliesDescription
{
    public string Name { get; set; }
    public string Path { get; set; }

    public AssembliesDescription()
    {
        Name = string.Empty;
        Path = string.Empty;
    }

    public AssembliesDescription(string name, string path)
    {
        Name = name;
        Path = path;
    }

    public override string ToString()
    {
        return $"Name: {Name}\n"
            + $"Path: {Path}";
    }
}

// Class: AssembliesCollection
public class AssembliesCollection
{
    public List<AssembliesDescription> Processed { get; set; }
    public List<AssembliesDescription> Runtimes { get; set; }
    public List<AssembliesDescription> Crossgen2s { get; set; }

    public AssembliesCollection()
    {
        Processed = new List<AssembliesDescription>();
        Runtimes = new List<AssembliesDescription>();
        Crossgen2s = new List<AssembliesDescription>();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (!Processed.IsEmpty())
            sb.Append(FormatAssembliesDescription(Processed, "Processed"));

        if (!Runtimes.IsEmpty())
            sb.Append(FormatAssembliesDescription(Runtimes, "Runtimes"));

        if (!Crossgen2s.IsEmpty())
            sb.Append(FormatAssembliesDescription(Crossgen2s, "Crossgen2s"));

        return sb.ToString();
    }

    private string FormatAssembliesDescription(List<AssembliesDescription> asmsList,
                                               string whichAsms)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\n{0} Assemblies:\n\n", whichAsms);

        foreach (var asmDesc in asmsList)
        {
            sb.AppendLine(asmDesc.ToString());
        }
        return sb.ToString();
    }
}
