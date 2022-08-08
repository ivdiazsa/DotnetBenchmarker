// File: src/Models/RunOptions.cs
using System.Collections.Generic;

// Class: RunOptions
public class RunOptions
{
    // Move this to its own file whenever we add more run options to our tool.
    // Inner Class: TraceOptions
    public class TraceOptions
    {
        public List<string> TraceProviders { get; set; }
        public List<string> CollectArgs { get; set; }
        public string OutputName { get; set; }

        public TraceOptions()
        {
            TraceProviders = new List<string>();
            CollectArgs = new List<string>();
            OutputName = "funny-trace";
        }
    }

    public TraceOptions? Trace { get; set; }
}
