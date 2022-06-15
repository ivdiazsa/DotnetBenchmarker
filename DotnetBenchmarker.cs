// File: DotnetBenchmarker.cs

// Class: DotnetBenchmarker
// This is our little script :)

// NEXT STEPS:
// 1) Implement the numeric calculations.
// 2) Implement non-composites Crossgen2'ing.
// 3) Refactor this mess before it becomes an actually serious issue.
//      * Removal of Control Characters from process stdout logging
//      * Extraction of binaries search from the Composites Builder
// 4) Implementation of a fully functional RunBenchmarker.cmd script
// 5) Addition of partial composites support

internal class DotnetBenchmarker
{
    static void Main(string[] args)
    {
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
