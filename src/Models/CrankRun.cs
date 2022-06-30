// File: src/Models/CrankRun.cs

// Inner Class: CrankRun
public partial class CrankRunner
{
    private class CrankRun
    {
        public string Args { get; }
        public string Name { get; }

        public CrankRun(string cmdArgs, string cfgName)
        {
            Args = cmdArgs;
            Name = cfgName;
        }
    }
}
