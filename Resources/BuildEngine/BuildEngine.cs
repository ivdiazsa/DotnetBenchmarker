// File: BuildEngine.cs
using System;

// Class: BuildEngine
// This little buddy will process and generate all composite or non-composite
// assemblies as requested :)
internal partial class BuildEngine
{
    static void Main(string[] args)
    {
        // This reads the environment variables set by the Dockerfile or
        // Powershell, depending on the platform, automatically.
        var engine = new EngineEnvironment();
    }
}
