// File: src/Parsers/CommandLineParser.cs
using System.CommandLine;

// Class: CommandLineParser
public static class CommandLineParser
{
    public static void ParseIntoOptsBank(AppOptionsBank bank, string[] args)
    {
        var configFileOption = new Option<string>(
            name: "--config-file",
            description: "The YAML file with the configurations to execute.",
            getDefaultValue: () => string.Empty
        );

        var iterationsOption = new Option<int>(
            name: "--iterations",
            description: "Times to run each benchmark configuration.",
            getDefaultValue: () => 1
        );

        var buildOnlyOption = new Option<bool>(
            name: "--build-only",
            description: "Just generate the configuration(s) binaries.",
            getDefaultValue: () => false
        );

        var rootCommand = new RootCommand();
        rootCommand.Add(configFileOption);
        rootCommand.Add(iterationsOption);
        rootCommand.Add(buildOnlyOption);

        rootCommand.SetHandler((configFileOptionValue,
                                iterationsOptionValue,
                                buildOnlyOptionValue) =>
        {
            bank.ConfigFile = configFileOptionValue;
            bank.Iterations = iterationsOptionValue;
            bank.BuildOnly = buildOnlyOptionValue;
        },
        configFileOption, iterationsOption, buildOnlyOption);

        rootCommand.Invoke(args);
    }
}
