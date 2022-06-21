// File: DotnetBenchmarker.cs

// Class: DotnetBenchmarker
// This is our little script :)

// NEXT STEPS:
// 1) Windows Stuff and Composites Implementation
// 2) Support of ComputeReport() in OutputProcessor to run any function passed
//    to it, rather than "hard-coding" it in its source file.
// 3) Fix of non-composite AVX2 processing
// 4) Try refactoring BuildComposites.sh to use functions and look cleaner
// 5) Support of partial composites

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
