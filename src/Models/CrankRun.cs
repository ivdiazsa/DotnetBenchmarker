// File: src/Models/CrankRun.cs

// Inner Class: CrankRun
public partial class CrankRunner
{
    private class CrankRun
    {
        public string Args { get; }
        public string Name { get; }

        // Need to also know the owning configuration name to know what numbers
        // we are seeing, later on when processing and displaying results :)
        public CrankRun(string cmdArgs, string cfgName)
        {
            Args = cmdArgs;
            Name = cfgName;
        }
    }
}
