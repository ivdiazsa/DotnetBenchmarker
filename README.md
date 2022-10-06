# Dotnet Benchmarker

This tool was created with the purpose of making it simple and easy to do performance tests for .NET runtime. It sets up and configures the runtime according to your needs, and sends the requests to run to dotnet's _crank_ tool.

The Benchmarker is currently specialized in Crossgen2 and composite images, but can be used for many more scenarios. Plus, I'm actively developing and maintaining it, so new features are in the works!

## Instructions

Here will go the instructions on how to work with the Benchmarker.

**DISCLAIMER**: Right now, I'm only listing the basics for the most common scenarios. I will later on devote enough time to fully explaining everything there is to know about the tool and how to use it.

First, you need to have the .NET 6 and .NET 7 SDK's installed on your machine. For most purposes, .NET 7 is the version to use, but the _crank_ tool (more on it later), also requires .NET 6 to be able to run for the time being. You can download them from the official .NET website:

* [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

For informational purposes, I used _Preview 7 (7.0.0-preview.7)_ while developing this tool.

You can then confirm it's ready to go by issuing `dotnet --info` in your terminal. It should show both versions of .NET in the output. For example, on my Mac, it shows something like this:

```text
.NET SDK:
 Version:   7.0.100-preview.7.22377.5
 Commit:    ba310d9309

Runtime Environment:
 OS Name:     Mac OS X
 OS Version:  12.6
 OS Platform: Darwin
 RID:         osx.12-arm64
 Base Path:   /usr/local/share/dotnet/sdk/7.0.100-preview.7.22377.5/

Host:
  Version:      7.0.0-preview.7.22375.6
  Architecture: arm64
  Commit:       eecb028078

.NET SDKs installed:
  6.0.400 [/usr/local/share/dotnet/sdk]
  7.0.100-preview.7.22377.5 [/usr/local/share/dotnet/sdk]

.NET runtimes installed:
  Microsoft.AspNetCore.App 6.0.8 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 7.0.0-preview.7.22376.6 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 6.0.8 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 7.0.0-preview.7.22375.6 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
```

Once you have dotnet installed, you have to install the _crank_ tool. Issue the following command in your terminal:

```bash
dotnet tool update Microsoft.Crank.Controller --version "0.2.0-*" --global
```

Run `crank --help` to ensure that it's ready to go. On Windows and MacOS, it should be direct. On Linux, just make sure you add the dotnet tools directory to your _PATH_ environment variable.

Next, you'll need a build of _Crossgen2_. You can get this by building it from the _runtime_ repo. Here's how to do it:

```bash
git clone https://github.com/dotnet/runtime.git
cd runtime
./build.sh -s clr -c Release
```

Replace `build.sh` with `build.cmd` if you're building on Windows. Also, it is worth noting that it's highly preferable you build on _Release_ mode as in this example.

You'll find the crossgen2 build under `path/to/runtime/artifacts/bin/coreclr/<OS>.x64.Release/crossgen2`. Keep note of that path because you're going to need it later.

Now, you're ready to begin working with the benchmarker! First, clone the repo and build the project on _Release_ mode:

```bash
git clone https://github.com/ivdiazsa/DotnetBenchmarker.git
cd DotnetBenchmarker
dotnet build -c Release
```

The app is ready to go! Now, you need to write what and how you want to build and run, through a _YAML_ file. For a template to see everything you can add to it, check out the [design yaml doc](template-design.yaml) I wrote. Note that it's for illustrational purposes only. You have to write your own one with actual existing paths and whatnot.

For this example, here is a small _YAML config_ file I wrote to use while testing the tool as I implemented it:

```yml
# test-config.yaml
assemblies:

  windows: # Going to build the composites on a Windows machine, hence we need a Windows crossgen2 build.
    crossgen2s:
      - name: RuntimeRepo
        path: path\to\runtime\artifacts\bin\coreclr\windows.x64.Release\crossgen2

configurations:

  - name: NormalLinuxOnWindows # Any name you want.
    os: linux # Target OS: We are going to generate Linux composites to run on crank's Linux servers.
    assembliesToUse:
      runtime: Latest # We'll be using a nightly build of the SDK.
      crossgen2: RuntimeRepo # This is the key that points to which Crossgen2 you want to use. Note that the name above is 'RuntimeRepo'.
    scenariosFile: https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/json.benchmarks.yml # Crank stuff.
    scenario: json # We want to run the 'json' scenario defined in the file linked above.
    buildPhase:
      params:
        - frameworkcomposite # Build framework composites.
        - bundleaspnet # Bundle/include the asp.net binaries into the composite image.
    runPhase:
      params:
        - appr2r # Tell crank to build its app using ReadyToRun enabled.
        - envreadytorun # Tell crank to set DOTNET_ReadyToRun=1 in its environment.
        - envtieredcompilation # Tell crank to set DOTNET_TieredCompilation=1 in its environment.
```

Some notes regarding the previous little _YAML config file_:

* We are telling it to use a nightly build of the SDK. This is downloaded by the benchmarker from the [installer repo](https://github.com/dotnet/installer). For this example, since we want to target Linux, it would download this one: <https://aka.ms/dotnet/8.0.1xx/daily/dotnet-sdk-linux-x64.tar.gz>.
* The `scenariosFile` is part of TechEmpower's configuration files, which we only use.
* The _yaml_ file's data is case-sensitive.

Now, about the _Partial Composites_:

You have to write a text file or two with a list of the names of the assemblies you want to compile into the composite. I say one or two because one file enlists framework assemblies, while the other enlists aspnet assemblies. The format is simple. Write each assembly on one line. For example:

_FxPartials.txt_:

```text
System.Private.CoreLib.dll
System.Runtime.dll
System.Collections.dll
```

_AspPartials.txt_:

```text
Microsoft.AspNetCore.dll
Microsoft.AspNetCore.Html.dll
Microsoft.AspNetCore.Diagnostics.dll
```

Note that I have no idea if bundling these assemblies would yield any performance gains. I just picked them at random for illustrational purposes.

Once you have this files, you ought to link them to the _Build Phase_ of your _Configuration_:

```yml
buildPhase:
  fxAssembliesSubset: Path/To/FxPartials.txt
  aspAssembliesSubset: Path/To/AspPartials.txt
  params:
  - frameworkcomposite # Build framework composites.
  - bundleaspnet # Bundle/include the asp.net binaries into the composite image.
```

**WARNING**: Make sure there are no trailing blank lines in your partials files! There's a small bug that makes the tool consider them as an empty file, and therefore fails when attempting to build because such file is indeed not there. I already have a fix but didn't have time to push it into the repo. Will do so soon.

Now that everything is set up, you're ready to run the benchmarker!

Simply call the corresponding script at the root of the repo with the appropriate flags, and sit back and relax while the benchmarker does all the building and crank dealing for you. These are the currently supported command-line options:

* `--config-file`: The only mandatory flag. With this one, you tell the benchmarker where your `yaml` file is.
* `--iterations`: How many times you want to run each configuration with crank. Defaults to '1' if omitted. Might be obvious but this does not include building since there's no need to build more than once :)
* `--build-only`: If you only want to generate the composites, but not run anything with crank, pass this flag to the benchmarker script. Defaults to 'false' if omitted.
* `--rebuild`: Build and process again each configuration's assemblies, regardless of whether they were already there or not.

If you need any reminder about these flags, you can always run the benchmarker with the `--help` flag.

So now, let's suppose you want to build and run the crank tests three times. At the root of the repo, simply issue the following command:

```bash
./runbenchmarker.sh --config-file /path/to/test-config.yaml --iterations 3
```

Use `runbenchmarker.cmd` if you're running on Windows.

Once everything is finished, you'll find the results of each iteration, of each configuration, in a _JSON_ file saved to `DotnetBenchmarker/results`. Likewise, everything that transpires gets recorded in logs, which are saved to `DotnetBenchmarker/logs`. All the materials used (crossgen2's, runtime's, output processed binaries), are saved to their respective places in `DotnetBenchmarker/resources`.

Hope everything here is good enough for you to get working successfully with the benchmarker! I will give this doc more shape and write a full documentation soon!
