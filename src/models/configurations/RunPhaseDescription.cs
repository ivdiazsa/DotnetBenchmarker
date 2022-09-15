// File: src/Models/Configurations/RunPhaseDescription.cs
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DotnetBenchmarker;

// Class: RunPhaseDescription
public class RunPhaseDescription
{
    // Same deal as with the BuildPhaseDescription. This Params list allows us
    // to write in list forms the parameters in the YAML file.
    public List<string> Params { get; set; }

    public bool AppR2R { get; set; }
    public bool AppAvx2 { get; set; }
    public bool EnvReadyToRun { get; set; }
    public bool EnvTieredCompilation { get; set; }

    public RunPhaseDescription()
    {
        Params = new List<string>();
        AppR2R = false;
        AppAvx2 = false;
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
        var strBuilder = new StringBuilder("Run Phase Parameters: \n");
        strBuilder.AppendFormat("App Built with R2R: {0}\n", AppR2R.ToString());
        strBuilder.AppendFormat("App Built with AVX2: {0}\n", AppAvx2.ToString());

        strBuilder.AppendFormat("Set DOTNET_ReadyToRun: {0}\n",
                                EnvReadyToRun.ToString());

        strBuilder.AppendFormat("Set DOTNET_TieredCompilation: {0}",
                                EnvTieredCompilation.ToString());
        return strBuilder.ToString();
    }
}
