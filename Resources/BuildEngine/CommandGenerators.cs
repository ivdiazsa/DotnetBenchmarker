// File: CommandGenerators.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Summary of this system: Since the commands differ very slightly between types
// of assembly processing, this template approach was the most suitable for easier
// maintainability, and code readability. Since the repeated code is kept to a
// minimum, the differences between the different ways of processing are more
// visual and apparent.

// Base Class: BaseCommandGenerator
internal abstract class BaseCommandGenerator
{
    protected AppPaths Paths { get; }
    protected EngineEnvironment Env { get; }

    protected StringBuilder CommandSb { get; }
    protected string TargetOS { get; }

    public BaseCommandGenerator(in AppPaths paths, EngineEnvironment engEnv,
                                string targetOs)
    {
        Paths = paths;
        Env = engEnv;
        CommandSb = new StringBuilder();
        TargetOS = targetOs;
    }

    public virtual void GenerateCmd()
    {
        CommandSb.Append(Paths.crossgen2exe);
        CommandSb.AppendFormat(" --targetos={0}", TargetOS);
        CommandSb.Append(" --targetarch=x64");

        if (Env.UseAvx2)
        {
            Console.WriteLine("\nWill apply AVX2 instruction set...");
            CommandSb.Append(" --instruction-set=avx2");
        }

        string crossgenDir = Path.GetDirectoryName(Paths.crossgen2exe)!;

        // NOTE: It might be necessary to support using an arbitrary number of
        //       optimization data files.
        if (File.Exists($"{crossgenDir}/StandardOptimizationData.mibc"))
        {
            Console.WriteLine("Will use StandardOptimizationData.mibc...");
            CommandSb.AppendFormat(" --mibc={0}/StandardOptimizationData.mibc",
                                    crossgenDir);
        }
    }

    public virtual string GetCmd()
    {
        return CommandSb.ToString();
    }
}

internal class NormalCommandGenerator : BaseCommandGenerator
{
    private bool _firstRun;
    private StringBuilder _normalCmdSb;

    public NormalCommandGenerator(in AppPaths paths, EngineEnvironment engEnv,
                                  string targetOs)
        : base(paths, engEnv, targetOs)
    {
        _firstRun = true;
        _normalCmdSb = new StringBuilder();
    }

    public void GenerateCmd(string assembly, params string[] referencePaths)
    {
        // This is to prevent the "Will use..." messages displayed by the base
        // GenerateCmd() method for each assembly. The logs are much more
        // readable this way.
        if (_firstRun)
        {
            base.GenerateCmd();
            _firstRun = false;
        }

        if (!_normalCmdSb.IsEmpty())
            _normalCmdSb.Clear();

        foreach (string refy in referencePaths)
        {
            _normalCmdSb.AppendFormat(" --reference={0}/*.dll", refy);
        }

        _normalCmdSb.AppendFormat(" {0}", assembly);
        _normalCmdSb.AppendFormat(" --out={0}/{1}", Paths.output,
                                                    Path.GetFileName(assembly));
    }

    public override string GetCmd()
    {
        return base.GetCmd() + _normalCmdSb.ToString();
    }
}

// Derived Class Level 1: CompositeCommandGenerator
internal abstract class CompositeCommandGenerator : BaseCommandGenerator
{
    protected string CompositeFile { get; set; }

    public CompositeCommandGenerator(in AppPaths paths, EngineEnvironment engEnv,
                                     string targetOs)
        : base(paths, engEnv, targetOs)
    {
        CompositeFile = "(null)";
    }

    public override void GenerateCmd()
    {
        base.GenerateCmd();
        CommandSb.Append(" --composite");
    }

    protected void GenerateCmdFooter(string assembliesToComposite)
    {
        CommandSb.AppendFormat(" {0}", assembliesToComposite);
        CommandSb.AppendFormat(" --out={0}/{1}.r2r.dll",
                               Paths.output,
                               CompositeFile);
    }
}

// Derived Class Level 2: FxCompositeCommandGenerator
internal class FxCompositeCommandGenerator : CompositeCommandGenerator
{
    public FxCompositeCommandGenerator(in AppPaths paths, EngineEnvironment engEnv,
                                       string targetOs)
        : base(paths, engEnv, targetOs)
    {
        CompositeFile = "framework";
    }

    public override void GenerateCmd()
    {
        base.GenerateCmd();
        Console.WriteLine("\nCompiling Framework Composites...");

        var assembliesToCompositeSb = new StringBuilder();

        if (Env.RequestedPartialFxComposites())
        {
            Console.WriteLine("Partial Framework Under Construction!");
            Environment.Exit(3);
        }
        else
        {
            assembliesToCompositeSb.AppendFormat(" {0}/*.dll", Paths.fx);
        }

        if (Env.BundleAspnet)
        {
            Console.WriteLine("ASP.NET will be bundled into the composite image...");
            CompositeFile += "-aspnet";

            if (Env.RequestedPartialAspnetComposites())
            {
                Console.WriteLine("Partial ASP.NET Bundle Under Construction!");
                Environment.Exit(3);
            }
            else
            {
                assembliesToCompositeSb.AppendFormat(" {0}/*.dll", Paths.asp);
            }
        }
        GenerateCmdFooter(assembliesToCompositeSb.ToString());
    }
}

// Derived Class Level 2: AspCompositeCommandGenerator
internal class AspCompositeCommandGenerator : CompositeCommandGenerator
{
    public AspCompositeCommandGenerator(in AppPaths paths, EngineEnvironment engEnv,
                                        string targetOs)
        : base(paths, engEnv, targetOs)
    {
        CompositeFile = "aspnetcore";
    }

    public override void GenerateCmd()
    {
        base.GenerateCmd();
        Console.WriteLine("\nCompiling ASP.NET Composites...");

        var assembliesToCompositeSb = new StringBuilder();

        if (Env.RequestedPartialAspnetComposites())
        {
            Console.WriteLine("Partial ASP.NET Under Construction!");
            Environment.Exit(3);
        }
        else
        {
            assembliesToCompositeSb.AppendFormat(" {0}/*.dll", Paths.asp);
        }

        CommandSb.AppendFormat(" --reference={0}/*.dll", Paths.fx);
        GenerateCmdFooter(assembliesToCompositeSb.ToString());
    }
}
