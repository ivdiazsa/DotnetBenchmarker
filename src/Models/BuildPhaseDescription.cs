// File: src/Models/BuildPhaseDescription.cs
using System.Collections.Generic;
using System.Reflection;
using System.Text;

// Class: BuildPhaseDescription
public class BuildPhaseDescription
{
    // This Params list allows us to write in list forms the parameters in
    // the YAML file. This is required by YAMLDotNet, which we use to parse it.
    public List<string> Params { get; set; }

    public string PartialFxComposites { get; set; }
    public string PartialAspComposites { get; set; }

    public bool FrameworkComposite { get; set; }
    public bool BundleAspNet { get; set; }
    public bool AspNetComposite { get; set; }
    public bool UseAvx2 { get; set; }
    public bool FullComposite { get; set; }

    private string _fxResultName = string.Empty;

    public BuildPhaseDescription()
    {
        Params = new List<string>();
        PartialFxComposites = string.Empty;
        PartialAspComposites = string.Empty;

        FrameworkComposite = false;
        BundleAspNet = false;
        AspNetComposite = false;
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

    public string FxResultName()
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

        // We currently use the partial composites list filename as part of the
        // label to differentiate among them and full composites builds.
        if (IsPartialComposites())
        {
            var fxPartials = System.IO.Path.GetFileNameWithoutExtension(PartialFxComposites);
            var aspPartials = System.IO.Path.GetFileNameWithoutExtension(PartialAspComposites);

            if (!string.IsNullOrEmpty(fxPartials))
                resultNameSb.AppendFormat("-{0}", fxPartials);

            if (!string.IsNullOrEmpty(aspPartials))
                resultNameSb.AppendFormat("-{0}", aspPartials);

            resultNameSb.Append("-partial");
        }

        _fxResultName = resultNameSb.ToString();
        return _fxResultName;
    }

    public bool NeedsRecompilation()
    {
        return (FrameworkComposite || AspNetComposite || UseAvx2);
    }

    public bool IsPartialComposites()
    {
        return (!string.IsNullOrEmpty(PartialFxComposites)
                || !string.IsNullOrEmpty(PartialAspComposites));
    }

    public override string ToString()
    {
        var strBuilder = new StringBuilder();
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

        return strBuilder.ToString();
    }
}
