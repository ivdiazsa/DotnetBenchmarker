// File: src/Parsers/YamlConfigFileParser.cs
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DotnetBenchmarker;

// Class: YamlConfigFileParser
public static class YamlConfigFileParser
{
    public static void ParseIntoOptsBank(AppOptionsBank bank)
    {
        string yaml = File.ReadAllText(bank.ConfigFile);
        var deserializer = new DeserializerBuilder()
                              .WithNamingConvention(CamelCaseNamingConvention.Instance)
                              .Build();

        AppDescription appDesc = deserializer.Deserialize<AppDescription>(yaml);

        // From the YAML parsing, we only get the list of flags written down.
        // We now need to initialize the *_PhaseDescription objects, using these
        // lists of parameters to know which flags to set.
        foreach (Configuration cfg in appDesc.Configurations)
        {
            if (cfg.BuildPhase is not null)
                cfg.BuildPhase.InitFromParamsList();

            if (cfg.RunPhase is not null)
                cfg.RunPhase.InitFromParamsList();
        }
        bank.AppDesc = appDesc;
    }
}
