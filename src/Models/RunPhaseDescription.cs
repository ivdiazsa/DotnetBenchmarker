// File: src/Models/RunPhaseDescription.cs
using System.Collections.Generic;
using System.Reflection;
using System.Text;

// Class: RunPhaseDescription
public class RunPhaseDescription
{
    // Same deal as with the BuildPhaseDescription. This Params list allows us
    // to write in list forms the parameters in the YAML file.
    public List<string> Params { get; set; }

    public bool AppR2R { get; set; }
    public bool EnvReadyToRun { get; set; }
    public bool EnvTieredCompilation { get; set; }

    public RunPhaseDescription()
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
