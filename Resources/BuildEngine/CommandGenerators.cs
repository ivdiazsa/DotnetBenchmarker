// File: CommandGenerators.cs
using System;
using System.IO;
using System.Text;

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
        CommandSb.AppendFormat(" --targetos {0}", TargetOS);
        CommandSb.Append(" --targetarch x64");

        if (Env.UseAvx2)
        {
            Console.WriteLine("\nWill apply AVX2 instruction set...");
            CommandSb.Append(" --instruction-set avx2");
        }

        string crossgenDir = Path.GetDirectoryName(Paths.crossgen2exe)!;

        if (File.Exists($"{crossgenDir}/StandardOptimizationData.mibc"))
        {
            Console.WriteLine("Will use StandardOptimizationData.mibc...");
            CommandSb.AppendFormat(" --mibc {0}/StandardOptimizationData.mibc",
                                    crossgenDir);
        }
    }

    public string GetCmd()
    {
        return CommandSb.ToString();
    }
}

internal class NormalCommandGenerator : BaseCommandGenerator
{
    public NormalCommandGenerator(in AppPaths paths, EngineEnvironment engEnv,
                                  string targetOs)
        : base(paths, engEnv, targetOs) {}

    public override void GenerateCmd()
    {
        // base.GenerateCmd();
        Console.WriteLine("Normal Crossgen'ing Under Construction!");
        Environment.Exit(3);
    }
}

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
        CommandSb.AppendFormat(" --out {0}/{1}.r2r.dll",
                               Paths.output,
                               CompositeFile);
    }
}

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

        CommandSb.AppendFormat(" --reference {0}/*.dll", Paths.fx);
        GenerateCmdFooter(assembliesToCompositeSb.ToString());
    }
}
