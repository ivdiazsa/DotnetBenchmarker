// File: AppPaths.cs
using System;

// Inner Struct: AppPaths
internal readonly struct AppPaths
{
    public string output { get; }
    public string dotnet { get; }
    public string fx { get; }
    public string asp { get; }
    public string crossgen2exe { get; }

    public AppPaths(string outputPath, string dotnetPath, string fxPath,
                    string aspPath, string crossgen2Path)
    {
        output = outputPath;
        dotnet = dotnetPath;
        fx = fxPath;
        asp = aspPath;
        crossgen2exe = Environment.OSVersion.Platform == PlatformID.Win32NT
                       ? crossgen2Path + ".exe"
                       : crossgen2Path;
    }
}
