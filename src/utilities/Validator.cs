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
    private static int _totalErrors = 0;

    internal static bool ValidateAll(AppDescription appDesc)
    {
        var errors = new ErrorsDictionary();
        ValidateAssembliesOSs(appDesc, errors);
        ValidateConfigurations(appDesc, errors);

        // Print all the problems that we found, if any.
        if (!errors.IsEmpty())
        {
            Console.WriteLine("\nProblems found while analyzing the app:\n");

            foreach (KeyValuePair<string, List<string>> item in errors)
            {
                Console.WriteLine($"{item.Key}:\n");
                foreach (string message in item.Value)
                {
                    Console.WriteLine(message);
                }
                Console.Write("\n");
            }

            Console.Write($"There were {_totalErrors} problems found.\n");
            return false;
        }
        return true;
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
            _totalErrors++;
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
                    Console.WriteLine($"INFO: Configuration '{item.Name}' has a"
                                    + " Build Phase and Processed Assemblies"
                                    + " specified. Will use the assemblies and"
                                    + " skip the build phase...");
                }
            }

            // Just letting the user know that if we have no RunPhase, we'll
            // just let Crank run its defaults.
            if (item.RunPhase is null)
            {
                Console.WriteLine($"INFO: Configuration '{item.Name}' doesn't have"
                                + " a Run Phase. Will use crank's default params...");
            }

            if (!configErrors.IsEmpty())
                errorsFound.Add($"Configuration {item.Name}", configErrors);
        }
    }

    private static void ValidateProcessingMaterials(
        Dictionary<string, AssembliesCollection> materials,
        Configuration config,
        List<string> cfgErrors)
    {
        // We have a Build Phase, and therefore we need a Crossgen2 we can run
        // on our current machine.
        if (!materials.ContainsKey(Constants.RunningOs))
        {
            cfgErrors.Add($"{Constants.RunningOs.Capitalize()} building materials"
                        + " not provided and are needed.");
            _totalErrors++;
            return ;
        }

        AssembliesNameLinks cfgMaterialLinks = config.AssembliesToUse;

        // If (a) link(s) to processed and/or runtime assemblies were specified,
        // we have to ensure they are defined in the target OS in the 'Assemblies'
        // section. Note that "Latest" is a keyword here. It means this configuration
        // will use a nightly build, and therefore we have nothing else to
        // verify here.
        if (!materials.ContainsKey(config.Os))
        {
            bool lol = false;

            if (!string.IsNullOrEmpty(cfgMaterialLinks.Processed))
            {
                cfgErrors.Add("AssembliesToUse: Processed Assemblies link was"
                            + $" specified for {config.Os.Capitalize()}, but"
                            + " none were found in the 'Assemblies' section.");
                _totalErrors++;
                lol = true;
            }

            // "Latest" is a keyword for using nightly builds, so that one is
            // valid for our app's purposes.
            if (!string.IsNullOrEmpty(cfgMaterialLinks.Runtime)
                && !cfgMaterialLinks.Runtime.Equals("Latest"))
            {
                cfgErrors.Add("AssembliesToUse: Runtime Assemblies link was"
                            + $" specified for {config.Os.Capitalize()}, but"
                            + " none were found in the 'Assemblies' section.");
                _totalErrors++;
                lol = true;
            }

            if (lol)
                return ;
        }

        var providedMaterialsForTargetOs = materials.GetValueOrDefault(config.Os)!;
        var providedMaterialsForRunningOS = materials.GetValueOrDefault(Constants.RunningOs)!;

        // Crossgen2 is a slightly different story. Since it is currently run
        // in the machine where this app is executing, we need crossgen2
        // assemblies for our running OS, rather than the config's target OS.
        if (providedMaterialsForRunningOS.Crossgen2s.IsEmpty())
        {
            cfgErrors.Add($"Running OS {Constants.RunningOs.Capitalize()} has no"
                        + " given Crossgen2's This config needs them for its"
                        + " Build Phase.");
            return ;
        }

        // Check that the referenced assemblies in the config in the YAML, are
        // present in the 'Assemblies' section. Otherwise, we will fail later on
        // whenever we need them.

        if (!string.IsNullOrEmpty(cfgMaterialLinks.Processed))
        {
            ValidateMaterialLinks(cfgMaterialLinks.Processed, "Processed",
                                  providedMaterialsForTargetOs.Processed, cfgErrors);
        }

        // If the user wants to use a nightly build, regardless of any other
        // specified runtimes, they'll use the 'Latest' keyword. In this case,
        // we have nothing to check since those binaries will be acquired later on :)
        if (!string.IsNullOrEmpty(cfgMaterialLinks.Runtime)
            && !cfgMaterialLinks.Runtime.Equals("Latest"))
        {
            ValidateMaterialLinks(cfgMaterialLinks.Runtime, "Runtime",
                                  providedMaterialsForTargetOs.Runtimes, cfgErrors);
        }

        // By this point, we are certain that at least one Crossgen2 build was
        // specified. Now, let's just make sure that there's a match.
        ValidateMaterialLinks(cfgMaterialLinks.Crossgen2, "Crossgen2",
                              providedMaterialsForRunningOS.Crossgen2s, cfgErrors);
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
                    + " the 'Assemblies' section of the YAML.");
        _totalErrors++;
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
            _totalErrors++;
        }

        if (!phase.FrameworkComposite)
        {
            cfgErrors.Add("BuildPhase: Can't use 'BundleAspNet' if"
                        + " 'FrameworkComposite' is not present.");
            _totalErrors++;
        }
    }
}
