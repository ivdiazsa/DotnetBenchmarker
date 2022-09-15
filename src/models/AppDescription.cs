// File: src/Models/AppDescription.cs
using System.Collections.Generic;
using System.Text;

namespace DotnetBenchmarker;

// Class: AppDescription
public class AppDescription
{
    // Since we're using a Dictionary here for simplicity, rather than a class
    // of its own like with the rest of the yaml fields, it's worth a brief of
    // how it stores the assemblies paths.
    //
    // The keys are the different OS's. Therefore, the only allowed values
    // are: Windows, MacOS, Linux, or none.
    public Dictionary<string, AssembliesCollection> Assemblies { get; set; }
    public List<Configuration> Configurations { get; set; }

    public AppDescription()
    {
        Assemblies = new Dictionary<string, AssembliesCollection>();
        Configurations = new List<Configuration>();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (Assemblies.IsEmpty())
        {
            sb.Append("No assemblies provided.\n\n");
        }
        else
        {
            sb.Append("Assemblies to be Used:\n\n");
            foreach (KeyValuePair<string, AssembliesCollection> asmColl in Assemblies)
            {
                sb.AppendFormat("Target OS: {0}\n", asmColl.Key);
                sb.AppendFormat("{0}\n\n", asmColl.Value.ToString());
            }
        }

        if (Configurations.IsEmpty())
        {
            sb.Append("No configurations provided.\n\n");
        }
        else
        {
            sb.Append("Configurations to be Run:\n\n");
            foreach (Configuration cfg in Configurations)
            {
                sb.AppendFormat("{0}\n\n", cfg.ToString());
            }
        }

        return sb.ToString();
    }
}
