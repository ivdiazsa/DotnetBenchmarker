// File: src/Utilities/Constants.cs
using System;

// Class: Constants
public static class Constants
{
    public static readonly string BasePath = Environment.CurrentDirectory;
    public static readonly string LogsPath = $"{BasePath}/Logs";
    public static readonly string ResourcesPath = $"{BasePath}/Resources";
    public static readonly string ResultsPath = $"{BasePath}/Results";
    public static readonly string RuntimeRepoShippingPath = "artifacts/packages/Release/Shipping";
    public static readonly string Timestamp = DateTime.Now.ToString("MMdd-HHmm");
}
