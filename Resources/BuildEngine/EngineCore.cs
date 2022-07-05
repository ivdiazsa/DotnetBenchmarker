// File: EngineCore.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

// Inner Class: EngineCore
public partial class BuildEngine
{
    private class EngineCore
    {
        internal void ProcessNonComposite(AppPaths enginePaths,
                                          EngineEnvironment engine)
        {
            var gen = new NormalCommandGenerator(enginePaths, engine, TargetOS);

            // This will probably be changed later to allow the user to select
            // whether they want to process either one or both products assemblies.
            // That will likely require some overhaul in the main app as well though.

            if (Directory.Exists(enginePaths.fx))
            {
                ProcessIndividualAssemblies(gen,
                                            enginePaths.fx,
                                            enginePaths.crossgen2exe,
                                            enginePaths.fx);
            }

            if (Directory.Exists(enginePaths.asp))
            {
                ProcessIndividualAssemblies(gen,
                                            enginePaths.asp,
                                            enginePaths.crossgen2exe,
                                            enginePaths.fx,
                                            enginePaths.asp);
            }

            // Not all dll's are crossgen'able in Windows, so we copy those
            // instead. For Linux, we have to also provide the *.so files so
            // that our composites can be understood.
            CopyRemainingBinaries(enginePaths.fx, enginePaths.asp, enginePaths.output);
        }

        internal void ProcessComposite(AppPaths enginePaths, EngineEnvironment engine)
        {
            CompositeCommandGenerator gen;

            if (engine.FrameworkComposite)
            {
                gen = new FxCompositeCommandGenerator(enginePaths, engine, TargetOS);
                gen.GenerateCmd();
                RunCrossgen2(gen.GetCmd(), enginePaths.crossgen2exe);
            }

            if (engine.AspnetComposite && !engine.BundleAspnet)
            {
                gen = new AspCompositeCommandGenerator(enginePaths, engine, TargetOS);
                gen.GenerateCmd();
                RunCrossgen2(gen.GetCmd(), enginePaths.crossgen2exe);
            }

            CopyRemainingBinaries(enginePaths.fx, enginePaths.asp, enginePaths.output);
        }

        private void ProcessIndividualAssemblies(NormalCommandGenerator gen,
                                                 string assembliesPath,
                                                 string crossgen2Path,
                                                 params string[] refsPaths)
        {
            string[] assemblies = Directory.GetFiles(assembliesPath, "*.dll");
            foreach (var bin in assemblies)
            {
                gen.GenerateCmd(bin, refsPaths);
                RunCrossgen2(gen.GetCmd(), crossgen2Path);
            }
        }

        private void RunCrossgen2(string generatedCmd, string crossgen2Path)
        {
            using (Process crossgen2 = new Process())
            {
                string[] fullCmdArgs = generatedCmd.Split(' ');

                var startInfo = new ProcessStartInfo
                {
                    FileName = fullCmdArgs.FirstOrDefault(crossgen2Path),
                    Arguments = string.Join(' ', fullCmdArgs.Skip(1)),
                };

                crossgen2.StartInfo = startInfo;
                crossgen2.Start();
                crossgen2.WaitForExit();
            }
        }

        private void CopyRemainingBinaries(string netCorePath, string aspNetPath,
                                           string resultsPath)
        {
            Console.Write("\n");
            MergeFolders(netCorePath, resultsPath);
            MergeFolders(aspNetPath, resultsPath);
        }

        private void MergeFolders(string srcPath, string destPath)
        {
            string[] files = Directory.GetFiles(srcPath);
            foreach (var item in files)
            {
                string itemName = Path.GetFileName(item);
                string destItemPath = Path.Combine(destPath, itemName);

                // We don't want to overwrite the crossgen'd binary with the
                // untouched one.
                if (!File.Exists(destItemPath))
                {
                    Console.WriteLine($"Copying {itemName} from {srcPath} to {destPath}...");
                    File.Copy(item, destItemPath);
                }
            }
        }
    }
}
