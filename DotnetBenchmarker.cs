﻿// File: DotnetBenchmarker.cs

// Class: DotnetBenchmarker
// This is our little script :)

// WARNING: FOR EXTERNALLY SUPPLIED FILES, ENSURE THEY ARE WRITTEN WITH THE LF
//          LINE TERMINATOR! I DON'T WANT TO SPEND OVER AN HOUR AGAIN DEALING
//          WITH A FILE NOT FOUND ERROR IN BASH, ALL BECAUSE OF THE ADDITIONAL
//          CLRF CHARACTER SCREWING UP ALL THE NON-HARD-CODED TEXT.

// Funny Note: When running tests without a build phase, we are uploading the
// files at the root of this repo instead LOL. I don't even know how this tool
// is supposed to work anymore :')

namespace DotnetBenchmarker;

internal class BenchmarkerCore
{
    static void Main(string[] args)
    {
        // Main Script Here!

        var optsBank = new AppOptionsBank();
        optsBank.Init(args);
        PrepareResourcesTree();

        // Download, copy, and build all the necessary stuff for our given
        // configurations.
        var workshop = new AssembliesWorkshop(optsBank.GetAssemblies(),
                                              optsBank.GetConfigurations());
        workshop.Run(optsBank.Rebuild);
        System.Console.WriteLine("\nAll builds finished successfully!\n");

        if (optsBank.BuildOnly)
        {
            System.Console.WriteLine("Exiting now...\n");
            System.Environment.Exit(0);
        }

        // Submit to crank and record the results of the runs.
        var runner = new CrankRunner(optsBank.AppDesc.Configurations,
                                     optsBank.Iterations);
        runner.Execute();
        System.Console.WriteLine("\nAll tests runs finished!\n");
    }

    static void PrepareResourcesTree()
    {
        AppPaths resourcesPaths = Constants.Paths;

        if (!System.IO.Directory.Exists(resourcesPaths.Logs))
            System.IO.Directory.CreateDirectory(resourcesPaths.Logs);

        if (!System.IO.Directory.Exists(resourcesPaths.Resources))
            System.IO.Directory.CreateDirectory(resourcesPaths.Resources);

        if (!System.IO.Directory.Exists(resourcesPaths.Results))
            System.IO.Directory.CreateDirectory(resourcesPaths.Results);
    }

    // This functionie is only used for testing individual components :)
    static void TestExit()
    {
        System.Environment.Exit(3);
    }
}
