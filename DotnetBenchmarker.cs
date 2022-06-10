﻿// File: DotnetBenchmarker.cs
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

// Class: DotnetBenchmarker
// This is our little script :)
internal class DotnetBenchmarker
{
    static void Main(string[] args)
    {
        var optsBank = new AppOptionsBank();
        optsBank.Init(args);

        var runner = new CrankRunner(optsBank.AppDesc.Configurations);
        runner.Execute();
    }
}
