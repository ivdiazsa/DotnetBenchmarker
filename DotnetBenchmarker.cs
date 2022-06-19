﻿// File: DotnetBenchmarker.cs

// Class: DotnetBenchmarker
// This is our little script :)

// NEXT STEPS:
// 1) Reenabling of non-composites Crossgen2'ing (Only ASP.NET is missing to reenable)
// 2) Implementation of a fully functional RunBenchmarker.cmd script
// 3) Windows Stuff and Composites Implementation
// 4) Addition of always uploading binaries to crank, even when no processing
//    was done by the app.

internal class DotnetBenchmarker
{
    static void Main(string[] args)
    {
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

        var output = new OutputProcessor($"{Constants.ResultsPath}/"
                                       + $"results-{Constants.Timestamp}.json");
        output.ComputeReport("Build Time (ms)", "Start Time (ms)");
        output.PrintToStream();
    }

    // This functionie is only used for testing individual components :)
    static void TestExit()
    {
        System.Environment.Exit(3);
    }
}
