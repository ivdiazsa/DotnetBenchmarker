// File: src/Models/Configurations/BuildPhaseDescription.cs
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DotnetBenchmarker;

// Class: BuildPhaseDescription
public class BuildPhaseDescription
{
    // This Params list allows us to write in list forms the parameters in
    // the YAML file. This is required by YAMLDotNet, which we use to parse it.
    public List<string> Params { get; set; }
    public string AssembliesSubset { get; set; }

    public bool FrameworkComposite { get; set; }
    public bool AspNetComposite { get; set; }
    public bool BundleAspNet { get; set; }
    public bool FullComposite { get; set; } // Currently not supported.
    public bool UseAvx2 { get; set; }

    private string _fxResultName = string.Empty;
    public string FxResultName
    {
        get
        {
            // No need to calculate it more than once :)
            if (!string.IsNullOrEmpty(_fxResultName))
                return _fxResultName;

            if (!NeedsRecompilation())
            {
                _fxResultName = "vanilla";
                return _fxResultName;
            }

            var resultNameSb = new StringBuilder("framework");

            if (UseAvx2)
                resultNameSb.Append("-avx2");
            if (FrameworkComposite && BundleAspNet)
                resultNameSb.Append("-aspnet-bundle");
            if (AspNetComposite && !BundleAspNet)
            {
                if (FrameworkComposite)
                    resultNameSb.Append("-aspnet-separated");
                else
                    resultNameSb.Append("-aspnet");
            }

            _fxResultName = resultNameSb.ToString();
            return _fxResultName;
        }
    }

    public BuildPhaseDescription()
    {
        Params = new List<string>();
        AssembliesSubset = string.Empty;

        FrameworkComposite = false;
        AspNetComposite = false;
        BundleAspNet = false;
        FullComposite = false;
        UseAvx2 = false;
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

    // Depending on what kind of binaries this phase's owning configuration will
    // produce, we will have to label them differently. This is to be able to
    // determine later on (see Components/CompositesBuilder.cs for more details)
    // whether we already have built this specific kind of assemblies, and
    // in general for clarity in the logging :)

    public bool IsComposite()
    {
        return FrameworkComposite || AspNetComposite;
    }

    public bool IsPartialSubset()
    {
        return !string.IsNullOrEmpty(AssembliesSubset);
    }

    public bool NeedsRecompilation()
    {
        return IsComposite() || UseAvx2;
    }

    public override string ToString()
    {
        var strBuilder = new StringBuilder("Build Phase Parameters:\n");

        strBuilder.AppendFormat("Framework Composite: {0}\n",
                                FrameworkComposite.ToString());

        strBuilder.AppendFormat("Include ASP.NET: {0}\n",
                                 BundleAspNet.ToString());

        strBuilder.AppendFormat("ASP.NET Separate Composite: {0}\n",
                                 AspNetComposite.ToString());

        strBuilder.AppendFormat("Build With AVX2 Enabled: {0}\n",
                                 UseAvx2.ToString());

        strBuilder.AppendFormat("Build a Full Composite with the Runtime and App: {0}\n",
                                 FullComposite.ToString());

        strBuilder.AppendFormat("Assemblies to Build: {0}",
                                IsPartialSubset()
                                ? $"Listed in File {AssembliesSubset}"
                                : "All Assemblies");

        return strBuilder.ToString();
    }
}
