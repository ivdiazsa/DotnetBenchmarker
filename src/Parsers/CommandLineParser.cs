// File: src/Parsers/CommandLineParser.cs
using System.CommandLine;

// Class: CommandLineParser
public static class CommandLineParser
{
    public static void ParseIntoOptsBank(AppOptionsBank bank, string[] args)
    {
        // Define the command-line flags we will support, along with their
        // default values, in case they are not provided.

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

        var configFileOption = new Option<string>(
            name: "--config-file",
            description: "The YAML file with the configurations to execute.",
            getDefaultValue: () => string.Empty
        );

        var outputFileOption = new Option<string>(
            name: "--output-file",
            description: "Name of the file to write down the results in.",
            getDefaultValue: () => string.Empty
        );

        var outputFormatOption = new Option<string>(
            name: "--output-format",
            description: "Comma-separated list of formats to display results in.",
            getDefaultValue: () => string.Empty
        );

        // Add them to the handler.
        // My Ruby side might want to try out using Reflection if this list
        // gets too big in the future.
        var rootCommand = new RootCommand();
        rootCommand.Add(iterationsOption);
        rootCommand.Add(buildOnlyOption);
        rootCommand.Add(configFileOption);
        rootCommand.Add(outputFileOption);
        rootCommand.Add(outputFormatOption);

        // Store the provided values into our AppOptionsBank :)
        rootCommand.SetHandler((iterationsOptionValue,
                                buildOnlyOptionValue,
                                configFileOptionValue,
                                outputFileOptionValue,
                                outputFormatOptionValue) =>
        {
            bank.Iterations = iterationsOptionValue;
            bank.BuildOnly = buildOnlyOptionValue;
            bank.ConfigFile = configFileOptionValue;
            bank.OutputFile = outputFileOptionValue;
            bank.OutputFormat = outputFormatOptionValue.Split(',');
        },
        iterationsOption, buildOnlyOption, configFileOption, outputFileOption,
        outputFormatOption);

        // All the heavy lifting is done by the System.CommandLine library.
        rootCommand.Invoke(args);
    }
}
