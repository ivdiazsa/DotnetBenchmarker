// File: src/Parsers/CommandLineParser.cs
using System.CommandLine;

// Class: CommandLineParser
public static class CommandLineParser
{
    // Class goes here.
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

        var rootCommand = new RootCommand();
        rootCommand.Add(configFileOption);
        rootCommand.Add(iterationsOption);

        rootCommand.SetHandler((configFileOptionValue,
                                iterationsOptionValue) =>
        {
            bank.ConfigFile = configFileOptionValue;
            bank.Iterations = iterationsOptionValue;
        },
        configFileOption, iterationsOption);

        rootCommand.Invoke(args);
    }
}
