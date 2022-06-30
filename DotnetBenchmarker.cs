// File: DotnetBenchmarker.cs

// Class: DotnetBenchmarker
// This is our little script :)

// NEXT STEPS:
// 1) Support of ComputeReport() in OutputProcessor to run any function passed
//    to it, rather than "hard-coding" it in its source file.
// 2) Fix of non-composite AVX2 processing (Almost there. Only ASP.NET missing).
// 3) Support of partial composites (Doesn't work...).
// 4) Perhaps a safety block to finish processing the good run results whenever
//    a faulty one was encountered.
// 5) A configuration filter so that we don't have to edit the yaml config file
//    every time we want to exclude or include (a) certain configuration(s).
// 6) Skipping of configuration binaries generation when they are actually there.
// 7) Full code documentation (Both apps).
// 8) Development of BestTable's modes.

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
