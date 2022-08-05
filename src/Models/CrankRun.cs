// File: src/Models/CrankRun.cs

// Inner Class: CrankRun
public partial class CrankRunner
{
    private class CrankRun
    {
        public string Name { get; }
        public string Args { get; }
        public string OutputFiles { get; }

        // Need to also know the owning configuration name to know what numbers
        // we are seeing, later on when processing and displaying results :)
        public CrankRun(string cmdArgs, string cfgName, string processedAssemblies)
        {
            Args = cmdArgs;
            Name = cfgName;
            OutputFiles = processedAssemblies;
        }
    }
}
