﻿// File: DotnetBenchmarker.cs

// Class: DotnetBenchmarker
// This is our little script :)

// WORK ITEMS:
//
// - Begin working on the app!
// - Check the TODO and NOTE notes throughout the app.
// - OS Compatibility Matrix.

// WARNING: FOR EXTERNALLY SUPPLIED FILES, ENSURE THEY ARE WRITTEN WITH THE LF
//          LINE TERMINATOR! I DON'T WANT TO SPEND OVER AN HOUR AGAIN DEALING
//          WITH A FILE NOT FOUND ERROR IN BASH, ALL BECAUSE OF THE ADDITIONAL
//          CLRF CHARACTER SCREWING UP ALL THE NON-HARD-CODED TEXT.

namespace DotnetBenchmarker;

internal class BenchmarkerCore
{
    static void Main(string[] args)
    {
        // Main Script Here!
        var optsBank = new AppOptionsBank();
        optsBank.Init(args);
        PrepareResourcesTree();

        if (optsBank.Build)
        {
            var workshop = new AssembliesWorkshop(optsBank.AppDesc.Assemblies,
                                                optsBank.AppDesc.Configurations);
            workshop.Run();
        }
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
