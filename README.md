# Dotnet Benchmarker

This tool was created with the purpose of making it simple and easy to do
performance tests for .NET runtime. It sets up and configures the runtime
according to your needs, and sends the requests to run to dotnet's _crank_ tool.

The Benchmarker is currently specialized in Crossgen2 and composite images, but
can be used for many more scenarios. Plus, I'm actively developing and
maintaining it, so new features are in the works!

## How to use the Dotnet Benchmarker

First things first. It is of utmost importance that you run the benchmarker
using the provided `RunBenchmarker.cmd` script, which is provided at the root
of this repo. The reason being, we need to keep track of some relative paths
within the codebase, and so using the script ensures these paths are set and
found correctly.

Without further ado, here's how we use it.

First, build it like you would any other .NET app:
`dotnet build DotnetBenchmarker.csproj -c Release`

These are the command-line flags one can send to the Dotnet Benchmarker:

- `--config-file`: The benchmarker is instructed on what to build, how, etc,
                   by means of a YAML configuration file (more on this later).
                   You pass the path to this file using this flag. Without this
                   parameter, the tool can't run.

- `--build-only`: Tells the benchmarker to just generate the output binaries of
                  the given configurations, and exit. No crank runs are done.

- `--iterations`: How many times to call crank with each given configuration.
                  Set to 1 by default if omitted.

- `--output-file`: Path to the file where you want the final results to be
                   recorded to. They are solely printed to the console if omitted.

- `--output-format`: Select how you want the final results to be formatted.
                     Currently, Table and CSV formats are supported.

Run the script with any/all/some of the command-line flags described above,
and let the tool do all the heavy lifting while you relax and have a cup of coffee.

### The YAML Configuration File

This is essential for the benchmarker's functioning, so I dedicated a whole
section of the README to it.

Here's an example template:

```yaml
runtimes:
  - os: linux

crossgen2s:
  - os: linux
    path: /Path/To/Your/Linux/Crossgen2/Build

configurations:
  - name: default
    os: linux
    scenario: plaintext.benchmarks.yml
    runPhase:
      params:
        - appr2r
        - envreadytorun
        - envtieredcompilation

  - name: bundled-composite
    os: linux
    scenario: plaintext.benchmarks.yml
    buildPhase:
      params:
        - frameworkcomposite
        - bundleaspnet
    runPhase:
      params:
        - appr2r
        - envreadytorun
        - envtieredcompilation
```

So basically, the configuration file is comprised of three main sections:

- Runtimes
- Crossgen2's
- Configurations

Some fields are paths to somewhere. It is highly recommended you write the
absolute path, but if you prefer/want/need to use relative paths, keep in mind
they must be relative to this repo's root (assuming you use the cmd script to
run it as highly recommended).

#### The Runtimes

In this section, you will describe where the unprocessed runtime binaries will
come from. The properties you can use are the following:

- `os`: Which platform the runtime is for. Only Windows and Linux are currently
        supported. This is the only mandatory field.

- `binariesPath`: The folder where you have your runtime binaries acquired
                  elsewhere. This option takes precedence if `repoPath` is
                  also specified.

- `repoPath`: The folder where you have your clone of the runtime repo. The
              benchmarker then figures out where the binaries are. That said,
              shipping components must have been built. More details on this
              at the end of the README. This option is still under construction,
              so, it can't be used at the present moment.

The third option is triggered if you don't specify any path, like in the above
shown template. In this case, the tool will download a nightly build from the
installer repo, and use that one.

**NOTE**: As of now, the benchmarker manages the runtimes by their operating
          system. This means that you can only specify one build per OS, at
          least for now.

#### The Crossgen2's

In this section, you will describe where your Crossgen2 build is located. The
properties you can use are the following:

- `os`: Which platform the crossgen2 is for. Only Windows and Linux are
        currently supported.

- `binariesPath`: The folder where you have your Crossgen2 build.

If you specified a runtime repo earlier, then you can omit the crossgen2 section
for that specific OS. The tool will search the repo and get it from there.

**NOTE**: Check the _NOTE_ at the end of the runtimes' section. The same
          applies to the crossgen2's.

#### The Configurations

In this section, you will enlist all the configurations you want to build
and/or run. There are currently a handful of parameters supported to describe
it, which are explained down below:

- `name`: As it sounds, this is just a label to help you identify each configuration
          in your results, logs, and so on.

- `os`: Which platform this configuration will be built and run on.

- `scenario`: Link to the yml file describing the tests that crank will run. It
              can be a local file, or a link to a raw file online.

- `buildPhase`: Parameters to be considered and applied when building the binaries.
                This is explained in detail in its corresponding section below.

- `runPhase`: Parameters to be considered and applied when running the app with crank.
              This is explained in detail in its corresponding section below.

##### The Build Phase

This subsection will contain all the key parameters on how to build the
configuration. Here are the currently supported arguments:

- `partialFxComposites`: Path to the file with the list of Netcore assemblies
                         you wish to partially composite.

- `partialAspComposites`: Path to the file with the list of ASP.NET assemblies
                          you wish to partially composite.

- `params`: A bullet list of the properties you wish to use to build. Here are
            the possible values you can add here:
  - frameworkcomposite
  - bundleaspnet
  - aspnetcomposite
  - useavx2

Something important to mention regarding the partial composites lists, is they
require a very specific formatting. Nothing too fancy though. Just one assembly
per line, no bullets or anything of the like, and just the filename. The tool
automatically fills the full paths as necessary.

##### The Run Phase

This subsection will contain all the key parameters on how to run the
configuration. Here are the currently supported arguments:

- `params`: A bullet list of the properties you wish to use to run. Here are
            the possible values you can add here:
  - appr2r
  - envreadytorun
  - envtieredcompilation
