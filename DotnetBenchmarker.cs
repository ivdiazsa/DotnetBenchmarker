// File: DotnetBenchmarker.cs

// Class: DotnetBenchmarker
// This is our little script :)

// WORK ITEMS AND BUGS:
// 1) Allow clean runs (i.e. Crossgen2 builds should not be required when there
//    are no BuildPhases for a specific OS).
// 2) Finish writing the README.
// 3) Implement Universal Options Support.

// WARNING: FOR EXTERNALLY SUPPLIED FILES, ENSURE THEY ARE WRITTEN WITH THE LF
//          LINE TERMINATOR! I DON'T WANT TO SPEND OVER AN HOUR AGAIN DEALING
//          WITH A FILE NOT FOUND ERROR IN BASH, ALL BECAUSE OF THE ADDITIONAL
//          CLRF CHARACTER SCREWING UP ALL THE NON-HARD-CODED TEXT.

internal class DotnetBenchmarker
{
    static void Main(string[] args)
    {
        // Main Script Here!

        var optsBank = new AppOptionsBank();
        optsBank.Init(args);

        var configs = optsBank.GetConfigurations();

        var builder = new CompositesBuilder(optsBank.GetRuntimesGroupedByOS(),
                                            optsBank.GetCrossgen2sGroupedByOS(),
                                            optsBank.GetConfigurations());
        builder.Run();

        if (optsBank.BuildOnly)
        {
            System.Console.WriteLine("\nAssemblies generated successfully."
                                     + " Exiting now...");
            System.Environment.Exit(0);
        }

        var runner = new CrankRunner(optsBank.GetConfigurations(),
                                     optsBank.Iterations);
        runner.Execute();

        var output = new OutputProcessor($"{Constants.ResultsPath}/"
                                       + $"results-{Constants.Timestamp}.json",
                                         optsBank.OutputFile,
                                         optsBank.OutputFormat);

        output.ComputeReport(() => output.ExampleReportFunc("Build Time (ms)",
                                                            "Start Time (ms)",
                                                            "Assemblies Size (KB)"));
        output.PrintToStream();
    }

    // This functionie is only used for testing individual components :)
    static void TestExit()
    {
        System.Environment.Exit(3);
    }
}
