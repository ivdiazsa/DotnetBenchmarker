// File: EngineEnvironment.cs
using System;
using System.Reflection;

// Inner Class: EngineEnvironment
internal class EngineEnvironment
{
    // WARNING: Do not change these properties' names or casing.
    //          The Reflection process used to read the environment variables
    //          and fill these properties depends on this naming convention.

    public bool AspnetComposite { get; set; }
    public bool BundleAspnet { get; set; }
    public bool FrameworkComposite { get; set; }
    public bool UseAvx2 { get; set; }

    public string? CompositesType { get; set; }
    public string? DotnetVersionNumber { get; set; }
    public string? PartialAspnetComposites { get; set; }
    public string? PartialFrameworkComposites { get; set; }

    public EngineEnvironment()
    {
        PropertyInfo[] engineProperties = this.GetType().GetProperties();

        foreach (PropertyInfo prop in engineProperties)
        {
            string propEnvVariable = prop.Name.ToUpperSnakeCase();
            prop.SetValue(this, ReadAndConvertEnvironmentValue(propEnvVariable));
        }
    }

    public bool RequestedPartialFxComposites()
    {
        return System.IO.File.Exists(PartialFrameworkComposites);
    }

    public bool RequestedPartialAspnetComposites()
    {
        return System.IO.File.Exists(PartialAspnetComposites);
    }

    // NOTE: Might be worth it to add Boolean handling for the empty
    //       environment variable check. Right now, we assume all are set
    //       to something, but we don't know whether that's gonna remain
    //       this way in the future. It's not that likely but it's also
    //       not that unlikely as well.
    private dynamic ReadAndConvertEnvironmentValue(string envVarName)
    {
        string? env = Environment.GetEnvironmentVariable(envVarName);

        if (string.IsNullOrEmpty(env) || env.Equals("0"))
            return string.Empty;

        switch (env.ToLowerInvariant())
        {
            case "true":
            case "false":
                return bool.Parse(env);
            default:
                return env;
        }
    }
}
