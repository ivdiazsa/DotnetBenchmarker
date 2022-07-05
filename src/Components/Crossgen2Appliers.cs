// File: src/Components/Crossgen2Appliers.cs
using System.Diagnostics;
using System.Text;

public partial class CompositesBuilder
{
    // These two friendly classes build and run the necessary command to have
    // the appropriate script run the BuildEngine, who is in charge of actually
    // calling Crossgen2:
    //      * Linux uses Docker and a Shell script.
    //      * Windows uses a Powershell script.

    // Inner Class: LinuxCrossgen2er
    private static class LinuxCrossgen2er
    {
        internal static void Apply(Configuration config, MultiIOLogger logger)
        {
            BuildPhaseDescription buildMode = config.BuildPhase;

            string partialAspnetComposites =
                string.IsNullOrEmpty(buildMode.PartialAspComposites)
                    ? "0" : buildMode.PartialAspComposites;

            string partialFrameworkComposites =
                string.IsNullOrEmpty(buildMode.PartialFxComposites)
                    ? "0" : buildMode.PartialFxComposites;

            string[] dockerArgs = new string[]
            {
                $"ASPNET_COMPOSITE_ARG={buildMode.AspNetComposite}",
                $"BUNDLE_ASPNET_ARG={buildMode.BundleAspNet}",
                $"COMPOSITES_TYPE_ARG={config.BuildResultsName}",
                $"DOTNET_VERSION_NUMBER_ARG=7.0",
                $"FRAMEWORK_COMPOSITE_ARG={buildMode.FrameworkComposite}",
                $"PARTIAL_ASPNET_COMPOSITES_ARG={partialAspnetComposites}",
                $"PARTIAL_FRAMEWORK_COMPOSITES_ARG={partialFrameworkComposites}",
                $"USE_AVX2_ARG={buildMode.UseAvx2}",
            };

            string imageName = $"{config.BuildResultsName}-builder";

            DockerLauncher.CreateImage(imageName, Constants.ResourcesPath,
                                       logger, dockerArgs);

            DockerLauncher.RunImage(imageName, true, false, Constants.ResourcesPath,
                                    logger, "./BuildComposites.sh");
        }
    }

    // Inner Class: WindowsCrossgen2er
    private static class WindowsCrossgen2er
    {
        internal static void Apply(Configuration config, MultiIOLogger logger)
        {
            BuildPhaseDescription buildMode = config.BuildPhase;
            var powershellArgsSb = new StringBuilder();

            string partialAspnetComposites =
                string.IsNullOrEmpty(buildMode.PartialAspComposites)
                    ? "0" : buildMode.PartialAspComposites;

            string partialFrameworkComposites =
                string.IsNullOrEmpty(buildMode.PartialFxComposites)
                    ? "0" : buildMode.PartialFxComposites;

            powershellArgsSb.AppendFormat("{0}/BuildComposites.ps1",
                                          Constants.ResourcesPath);

            powershellArgsSb.AppendFormat(" -CompositesType {0}",
                                          config.BuildResultsName);

            powershellArgsSb.AppendFormat(" -PartialAspnetComposites {0}",
                                          partialAspnetComposites);

            powershellArgsSb.AppendFormat(" -PartialFrameworkComposites {0}",
                                          partialFrameworkComposites);

            powershellArgsSb.AppendFormat(" -DotnetVersionNumber 7.0");

            if (buildMode.AspNetComposite)
                powershellArgsSb.Append(" -AspNetComposite");

            if (buildMode.BundleAspNet)
                powershellArgsSb.Append(" -BundleAspNet");

            if (buildMode.FrameworkComposite)
                powershellArgsSb.Append(" -FrameworkComposite");

            if (buildMode.UseAvx2)
                powershellArgsSb.Append(" -UseAvx2");

            string powershellArgs = powershellArgsSb.ToString();
            logger.Write($"\npowershell.exe {powershellArgs}\n\n");

            using (Process powershell = new Process())
            {
                var startInfo = new ProcessStartInfo();
                powershell.StartInfo = startInfo.BaseTemplate("powershell",
                                                              powershellArgs);
                powershell.Start();

                while (!powershell.StandardOutput.EndOfStream)
                {
                    string line = powershell.StandardOutput.ReadLine()!;
                    logger.Write($"{line.CleanControlChars()}\n");
                }
                powershell.WaitForExit();
            }
        }
    }
}
