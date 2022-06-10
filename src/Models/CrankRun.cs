// File: src/Models/CrankRun.cs

// Class: CrankRun
public class CrankRun
{
    public string Args { get; }
    public string Name { get; }

    public CrankRun(string cmdArgs, string cfgName)
    {
        Args = cmdArgs;
        Name = cfgName;
    }
}
