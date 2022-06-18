// File: DotnetBenchmarker.cs
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

// Class: DotnetBenchmarker
// This is our little script :)

// NEXT STEPS:
// 1) Implement the numeric calculations.
//    * The current prototype is a mess. Redo it with jagged arrays instead.
// 2) Implement non-composites Crossgen2'ing.
// 3) Implementation of a fully functional RunBenchmarker.cmd script
// 4) Creation of missing Logs and Results folders at launch if necessary
// 5) Windows Composites Implementation
// 6) Replacement the NotSupported Exceptions with Platform Exceptions in all
//    the different OS checks throughout the app

using ResultsList = System.Collections.Generic.List<
                        System.Collections.Generic.Dictionary<string, string>>;

internal class DotnetBenchmarker
{
    static void Main(string[] args)
    {
        var output = new OutputProcessor("Results/results-0615-1818.json");
        output.ComputeReport("Build Time (ms)", "Start Time (ms)");
        output.PrintToStream();
        TestExit();

        // Main Script Here!

        var optsBank = new AppOptionsBank();
        optsBank.Init(args);

        var builder = new CompositesBuilder(optsBank.GetRuntimesGroupedByOS(),
                                            optsBank.GetCrossgen2sGroupedByOS(),
                                            optsBank.GetConfigurations());
        builder.Run();

        if (optsBank.BuildOnly)
        {
            System.Console.WriteLine("Assemblies generated successfully. Exiting now...");
            System.Environment.Exit(0);
        }

        var runner = new CrankRunner(optsBank.GetConfigurations(),
                                     optsBank.Iterations);
        runner.Execute();
    }

    // This functionie is only used for testing individual components :)
    static void TestExit()
    {
        System.Environment.Exit(3);
    }
}
