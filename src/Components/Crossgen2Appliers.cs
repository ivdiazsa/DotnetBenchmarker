// File: src/Components/Crossgen2Appliers.cs

public partial class CompositesBuilder
{
    private static class LinuxCrossgen2er
    {
        // Do Linux Crossgen2 with Docker here :)
        internal static void Apply(Configuration config, MultiIOLogger logger)
        {
            BuildPhaseDescription buildMode = config.BuildPhase;

            string[] dockerArgs = new string[]
            {
                $"ASPNET_COMPOSITE_ARG={buildMode.AspNetComposite}",
                $"BUNDLE_ASPNET_ARG={buildMode.BundleAspNet}",
                $"COMPOSITES_TYPE_ARG={config.BuildResultsName}",
                $"DOTNET_VERSION_ARG=7.0",
                $"FRAMEWORK_COMPOSITE_ARG={buildMode.FrameworkComposite}",
                $"USE_AVX2_ARG={buildMode.UseAvx2}",
            };

            string imageName = $"{config.BuildResultsName}-builder";

            DockerLauncher.CreateImage(imageName, Constants.ResourcesPath,
                                       logger, dockerArgs);

            DockerLauncher.RunImage(imageName, true, false, Constants.ResourcesPath,
                                    logger, "./BuildComposites.sh");
        }
    }

    private static class WindowsCrossgen2er
    {
        // Under construction! :)
        internal static void Apply(Configuration config, MultiIOLogger logger)
        {
            System.Console.WriteLine("Under Construction! :)");
            System.Environment.Exit(3);
        }
    }
}
