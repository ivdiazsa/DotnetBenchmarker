// File: src/Utilities/Constants.cs
using System;

// Class: Constants
// TODO: Add checks to ensure that all folder paths exist when the application
//       is launched. Otherwise, create them so the app doesn't fail to find
//       them, and consequently crash.
public static class Constants
{
    public static readonly string BasePath = Environment.CurrentDirectory;
    public static readonly string LogsPath = $"{BasePath}/Logs";
    public static readonly string ResourcesPath = $"{BasePath}/Resources";
    public static readonly string ResultsPath = $"{BasePath}/Results";
    public static readonly string Timestamp = DateTime.Now.ToString("MMdd-HHmm");
}
