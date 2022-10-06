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

        var buildOnlyOption = new Option<bool>(
            name: "--build-only",
            description: "Only generate the configuration(s) binaries.",
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

        var rebuildOption = new Option<bool>(
            name: "--rebuild",
            description: "Build all the processed assemblies regardless of whether"
                        + " they are already there or not.",
            getDefaultValue: () => false
        );

        // Add them to the handler.
        // My Ruby side might want to try out using Reflection if this list
        // gets too big in the future.
        var rootCommand = new RootCommand();
        rootCommand.Add(buildOnlyOption);
        rootCommand.Add(configFileOption);
        rootCommand.Add(iterationsOption);
        rootCommand.Add(rebuildOption);

        // Store the provided values into our AppOptionsBank :)
        rootCommand.SetHandler((buildOnlyOptionValue,
                                configFileOptionValue,
                                iterationsOptionValue,
                                rebuildOptionValue) =>
        {
            bank.BuildOnly = buildOnlyOptionValue;
            bank.ConfigFile = configFileOptionValue;
            bank.Iterations = iterationsOptionValue;
            bank.Rebuild = rebuildOptionValue;
        },
        buildOnlyOption, configFileOption, iterationsOption, rebuildOption);

        // All the heavy lifting is done by the System.CommandLine library.
        rootCommand.Invoke(args);
    }
}
