// File: src/utilities/Validator.cs
using System;
using System.Collections.Generic;
using System.Linq;

// Our app has multiple components to validate. The reason we're using a dictionary
// for the found errors, is so we can separate them by component and are able to
// give the user a clearer explanation of what went wrong.
using ErrorsDictionary = System.Collections.Generic.Dictionary<string,
                            System.Collections.Generic.List<string>>;

namespace DotnetBenchmarker;

// NOTE: I'm not sure yet whether this class might need some splitting-refactoring.
//       Either by creating additional helper classes, or make it partial and
//       scatter each validating function to a different accordingly place.

// Class: Validator
internal static class Validator
{
    internal static bool ValidateAll(AppDescription appDesc)
    {
        var errors = new ErrorsDictionary();
        ValidateAssembliesOSs(appDesc, errors);
        ValidateConfigurations(appDesc, errors);
        return errors.IsEmpty();
    }

    private static void ValidateAssembliesOSs(AppDescription appDesc,
                                              ErrorsDictionary errorsFound)
    {
        IEnumerable<string> providedAssembliesOSs = appDesc.Assemblies.Keys;

        // Check if the config YAML file has any unsupported OS's and mark them
        // as errors in the file.
        IEnumerable<string> oddOSs = providedAssembliesOSs.Where(
            os => !Enum.IsDefined(typeof(SupportedOS), os.Capitalize())
        );

        if (oddOSs.IsEmpty())
            return;

        // Read through the list of found unsupported OS's, and record the error
        // message that is going to be displayed later to the user.
        var theseErrors = new List<string>();
        foreach (string item in oddOSs)
        {
            theseErrors.Add($"The OS {item} is not currently supported.");
        }

        errorsFound.Add("Assemblies", theseErrors);
    }

    private static void ValidateConfigurations(AppDescription appDesc,
                                               ErrorsDictionary errorsFound)
    {
        foreach (Configuration item in appDesc.Configurations)
        {
            var configErrors = new List<string>();

            if (item.BuildPhase is not null)
            {
                ValidateProcessingMaterials(appDesc.Assemblies, item, configErrors);
                ValidateBuildPhase(item, configErrors);

                // If we are provided with 'Processed' assemblies, then we assume
                // those are ready to be used. Hence we would skip the BuildPhase
                // if there's one provided. We let the user know and move on since
                // it's not a cause for failing :)
                if (item.BuildPhase is not null
                    && !string.IsNullOrEmpty(item.AssembliesToUse.Processed))
                {
                    Console.WriteLine($"INFO: Configuration {item.Name} has a"
                                    + " Build Phase and Processed Assemblies"
                                    + " specified. Will use the assemblies and"
                                    + " skip the build phase...");
                }
            }

            // Just letting the user know that if we have no RunPhase, we'll
            // just let Crank run its defaults.
            if (item.RunPhase is null)
            {
                Console.WriteLine($"INFO: Configuration {item.Name} doesn't have"
                                + " a Run Phase. Will use crank's default params...");
            }
        }
    }

    private static void ValidateProcessingMaterials(
        Dictionary<string, AssembliesCollection> materials,
        Configuration config,
        List<string> cfgErrors)
    {
        if (!materials.ContainsKey(config.Os))
        {
            cfgErrors.Add($"OS {config.Os} requires building materials, but none"
                        + " were provided.");
            return ;
        }

        AssembliesNameLinks cfgMaterialLinks = config.AssembliesToUse;
        AssembliesCollection providedMaterialsForOs = materials.GetValueOrDefault(config.Os)!;

        // Check that the referenced assemblies in the config in the YAML, are
        // present in the 'Assemblies' section. Otherwise, we will fail later on
        // whenever we need them.

        ValidateMaterialLinks(cfgMaterialLinks.Processed, "Processed",
                              providedMaterialsForOs.Processed, cfgErrors);

        ValidateMaterialLinks(cfgMaterialLinks.Runtime, "Runtime",
                              providedMaterialsForOs.Runtimes, cfgErrors);

        ValidateMaterialLinks(cfgMaterialLinks.Crossgen2, "Crossgen2",
                              providedMaterialsForOs.Crossgen2s, cfgErrors);
    }

    private static void ValidateMaterialLinks(string cfgMatLink,
                                              string matType,
                                              List<AssembliesDescription> providedMats,
                                              List<string> cfgErrors)
    {
        if (string.IsNullOrEmpty(cfgMatLink))
            return ;

        // We found referenced already processed assemblies. Checking done!
        if (providedMats.Any(p => p.Name.Equals(cfgMatLink)))
            return ;

        cfgErrors.Add($"AssembliesToUse: The config references the {matType}"
                    + $" Assemblies '{cfgMatLink}', but they were not found in"
                    + " the 'Assemblies section of the YAML.");
    }

    private static void ValidateBuildPhase(Configuration config,
                                           List<string> cfgErrors)
    {
        if (!config.BuildPhase!.BundleAspNet)
            return ;

        BuildPhaseDescription phase = config.BuildPhase;

        if (phase.AspNetComposite)
        {
            cfgErrors.Add("BuildPhase: Can't use 'BundleAspNet' and"
                        + " 'AspNetComposite' at the same time.");
        }

        if (!phase.FrameworkComposite)
        {
            cfgErrors.Add("BuildPhase: Can't use 'BundleAspNet' if"
                        + " 'FrameworkComposite' is not present.");
        }
    }
}
