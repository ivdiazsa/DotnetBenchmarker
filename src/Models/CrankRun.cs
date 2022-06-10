// File: src/Models/CrankRun.cs

// Class: CrankRun
public class CrankRun
{
    public string Args { get; }
    public string Name { get; }
    public string Output { get; set; }

    public CrankRun(string cmdArgs, string cfgName)
    {
        Args = cmdArgs;
        Name = cfgName;
        Output = "(no output)";
    }
}
