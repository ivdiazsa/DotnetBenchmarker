// File: src/Models/Runtime.cs

// Class: Runtime
public class Runtime
{
    public string Os { get; set; }
    public string BinariesPath { get; set; }
    public string RepoPath { get; set; }

    public Runtime()
    {
        Os = "linux";
        BinariesPath = string.Empty;
        RepoPath = string.Empty;
    }
}
