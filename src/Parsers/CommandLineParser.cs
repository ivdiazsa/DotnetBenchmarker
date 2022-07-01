// File: src/Parsers/CommandLineParser.cs
using System.CommandLine;

// Class: CommandLineParser
public static class CommandLineParser
{
    public static void ParseIntoOptsBank(AppOptionsBank bank, string[] args)
    {
        // Define the command-line flags we will support, along with their
        // default values, in case they are not provided.

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

        // Add them to the handler.
        var rootCommand = new RootCommand();
        rootCommand.Add(configFileOption);
        rootCommand.Add(iterationsOption);
        rootCommand.Add(buildOnlyOption);

        // Store the provided values into our AppOptionsBank :)
        rootCommand.SetHandler((configFileOptionValue,
                                iterationsOptionValue,
                                buildOnlyOptionValue) =>
        {
            bank.ConfigFile = configFileOptionValue;
            bank.Iterations = iterationsOptionValue;
            bank.BuildOnly = buildOnlyOptionValue;
        },
        configFileOption, iterationsOption, buildOnlyOption);

        // All the heavy lifting is done by the System.CommandLine library.
        rootCommand.Invoke(args);
    }
}
