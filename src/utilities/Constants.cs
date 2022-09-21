// File: src/utilities/Constants.cs
using System;

namespace DotnetBenchmarker;

// TODO: Try to use this enum for all OS-related data. If keeping the other
// hard-coded values proves to be the better and clearer choice, then this
// work item can be considered done :)
enum SupportedOS { Windows, Macos, Linux }

readonly struct AppPaths
{
    public AppPaths()
    {
        Base = Environment.CurrentDirectory;
        Logs = $"{Base}/logs";
        Resources = $"{Base}/resources";
        Results = $"{Base}/results";
    }

    public string Base { get; init; }
    public string Logs { get; init; }
    public string Resources { get; init; }
    public string Results { get; init; }
}

static class Constants
{
    public static readonly string DotnetVersion = "8.0.1";
    public static readonly string Timestamp = DateTime.Now.ToString("MMdd-HHmm");
    public static readonly AppPaths Paths = new AppPaths();

    public static readonly string RunningOs = OperatingSystem.IsWindows() ? "windows"
                                                : OperatingSystem.IsMacOS() ? "macos"
                                                : OperatingSystem.IsLinux() ? "linux"
                                                : "other";
}
