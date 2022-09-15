// File: src/Models/Configurations/DotnetVersions.cs

namespace DotnetBenchmarker;

// Class: DotnetVersions
public class DotnetVersions
{
    public string Sdk { get; set; }
    public string Runtime { get; set; }
    public string Aspnetcore { get; set; }

    public DotnetVersions()
    {
        Sdk = "Latest";
        Runtime = "Latest";
        Aspnetcore = "Latest";
    }

    public override string ToString()
    {
        return $".NET Versions To Use:\n"
            + $"  SDK: {Sdk}\n"
            + $"  Runtime: {Runtime}\n"
            + $"  ASP.NET Core: {Aspnetcore}";
    }
}
