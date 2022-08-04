// File: src/Models/CrankRun.cs

// Inner Class: CrankRun
public partial class CrankRunner
{
    private class CrankRun
    {
        public string Name { get; }
        public string OutputFiles { get; }

        private string _args;
        public string Args { get { return _args; } }

        // Need to also know the owning configuration name to know what numbers
        // we are seeing, later on when processing and displaying results :)
        public CrankRun(string cmdArgs, string cfgName, string processedAssemblies)
        {
            _args = cmdArgs;
            Name = cfgName;
            OutputFiles = processedAssemblies;
        }

        public void UpdateTraceIndex(int oldIndex, int newIndex)
        {
            _args = _args.Replace($"-{oldIndex}", $"-{newIndex}");
        }
    }
}
