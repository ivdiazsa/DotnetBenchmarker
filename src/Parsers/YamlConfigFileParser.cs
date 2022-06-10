// File: src/Parsers/YamlConfigFileParser.cs
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        foreach (Configuration cfg in appDesc.Configurations)
        {
            cfg.RunPhase.InitFromParamsList();
        }
        bank.AppDesc = appDesc;
    }
}
