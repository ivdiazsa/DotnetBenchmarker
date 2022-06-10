// File: src/Models/RunEnvironment.cs
using System.Collections.Generic;
using System.Reflection;
using System.Text;

// Class: RunEnvironmentDescription
public class RunEnvironmentDescription
{
    public List<string> Params { get; set; }

    public bool AppR2R { get; set; }
    public bool EnvReadyToRun { get; set; }
    public bool EnvTieredCompilation { get; set; }

    public RunEnvironmentDescription()
    {
        Params = new List<string>();
        AppR2R = false;
        EnvReadyToRun = false;
        EnvTieredCompilation = false;
    }

    internal void InitFromParamsList()
    {
        foreach (string param in Params)
        {
            this.GetType()
                .GetProperty(param, BindingFlags.IgnoreCase
                                  | BindingFlags.Instance
                                  | BindingFlags.Public)!
                .SetValue(this, true);
        }
    }

    public override string ToString()
    {
        var strBuilder = new StringBuilder();
        strBuilder.AppendFormat("App Built with R2R: {0}\n", AppR2R.ToString());

        strBuilder.AppendFormat("Set COMPlus_ReadyToRun: {0}\n",
                                EnvReadyToRun.ToString());

        strBuilder.AppendFormat("Set COMPlus_TieredCompilation: {0}\n",
                                EnvTieredCompilation.ToString());
        return strBuilder.ToString();
    }
}
