// File: DotnetBenchmarker.cs

// Class: DotnetBenchmarker
// This is our little script :)

// NOTE: Might be beneficial to make a ProcessStartInfo template class, since
//       all spawned Process objects in this app follow that same pattern.

// NEXT STEPS:
// 1) Implement the numeric calculations.
// 2) Implement non-composites Crossgen2'ing.
// 3) Refactor this mess before it becomes an actually serious issue.

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

    static void TestExit()
    {
        System.Environment.Exit(3);
    }
}
