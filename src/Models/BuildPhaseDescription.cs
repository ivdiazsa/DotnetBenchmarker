// File: src/Models/BuildPhaseDescription.cs
using System.Collections.Generic;
using System.Reflection;
using System.Text;

// Class: BuildPhaseDescription
public class BuildPhaseDescription
{
    // Class definition goes here.
    public List<string> Params { get; set; }

    public bool FrameworkComposite { get; set; }
    public bool BundleAspNet { get; set; }
    public bool AspNetComposite { get; set; }

    private string _fxResultName = string.Empty;

    public BuildPhaseDescription()
    {
        Params = new List<string>();
        FrameworkComposite = false;
        BundleAspNet = false;
        AspNetComposite = false;
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

    public string FxResultName()
    {
        if (!string.IsNullOrEmpty(_fxResultName))
            return _fxResultName;
        
        if (!NeedsRecompilation())
        {
            _fxResultName = "vanilla";
            return _fxResultName;
        }

        var resultNameSb = new StringBuilder("framework");

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

    public bool NeedsRecompilation()
    {
        return (FrameworkComposite || AspNetComposite);
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
        return strBuilder.ToString();
    }
}
