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

        // The EngineEnvironment properties are named right after the
        // environment variables that define how to composite and whatnot.
        // So, we just read them and we can initialize our EngineEnvironment
        // straight away using Reflection.
        // Magic extendability and maintainability!

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

    // For now, non-composite processing == non-composite avx2.
    // However, I'm adding this method for clarity, and in anticipation of
    // requiring to support other types of non-composites.
    public bool RequestedNonComposites()
    {
        return UseAvx2 && !(FrameworkComposite || AspnetComposite);
    }

    // NOTE: Might be worth it to add Boolean handling for the empty
    //       environment variable check. Right now, we assume all are set
    //       to something, but we don't know whether that's gonna remain
    //       this way in the future. It's not that likely but it's also
    //       not that unlikely as well.
    private dynamic ReadAndConvertEnvironmentValue(string envVarName)
    {
        // Translate the environment variable values. The reason this function
        // is defined as "dynamic", is because we can get either strings or
        // booleans from the environment at the current time.
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
