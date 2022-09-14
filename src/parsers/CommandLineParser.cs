// File: src/Parsers/CommandLineParser.cs
using System.CommandLine;

namespace DotnetBenchmarker;

// Class: CommandLineParser
public static class CommandLineParser
{
    public static void ParseIntoOptsBank(AppOptionsBank bank, string[] args)
    {
        // Define the command-line flags we will support, along with their
        // default values, in case they are not provided.

        var buildOption = new Option<bool>(
            name: "--build",
            description: "Generate the configuration(s) binaries.",
            getDefaultValue: () => false
        );

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

        var runOption = new Option<bool>(
            name: "--run",
            description: "Run the configuration(s) tests.",
            getDefaultValue: () => false
        );

        // Add them to the handler.
        // My Ruby side might want to try out using Reflection if this list
        // gets too big in the future.
        var rootCommand = new RootCommand();
        rootCommand.Add(iterationsOption);
        rootCommand.Add(buildOption);
        rootCommand.Add(runOption);
        rootCommand.Add(configFileOption);

        // Store the provided values into our AppOptionsBank :)
        rootCommand.SetHandler((buildOptionValue,
                                configFileOptionValue,
                                iterationsOptionValue,
                                runOptionValue) =>
        {
            bank.Iterations = iterationsOptionValue;
            bank.Build = buildOptionValue;
            bank.Run = runOptionValue;
            bank.ConfigFile = configFileOptionValue;
        },
        buildOption, configFileOption, iterationsOption, runOption);

        // All the heavy lifting is done by the System.CommandLine library.
        rootCommand.Invoke(args);
    }
}
