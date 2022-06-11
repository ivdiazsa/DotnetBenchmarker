// File: DotnetBenchmarker.cs

// Class: DotnetBenchmarker
// This is our little script :)
internal class DotnetBenchmarker
{
    static void Main(string[] args)
    {
        var optsBank = new AppOptionsBank();
        optsBank.Init(args);

        var runner = new CrankRunner(optsBank.GetConfigurations(),
                                     optsBank.Iterations);
        runner.Execute();
    }
}
