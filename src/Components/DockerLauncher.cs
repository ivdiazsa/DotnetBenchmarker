// File: src/Components/DockerLauncher.cs
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// Class: Docker Launcher
internal static partial class DockerLauncher
{
    private static List<DockerImage> s_images = new List<DockerImage>();

    public static void RunImage(string imageName, bool removeContainer,
                                bool interactive, string mount,
                                MultiIOLogger logger, params string[] args)
    {
        var dockerRunArgsSb = new StringBuilder("run");

        if (removeContainer)
            dockerRunArgsSb.Append(" --rm");

        if (interactive)
            dockerRunArgsSb.Append(" -it");

        if (!string.IsNullOrEmpty(mount))
            dockerRunArgsSb.AppendFormat(" -v {0}:/mount -w /mount", mount);

        dockerRunArgsSb.AppendFormat(" {0}", imageName);
        foreach (string arg in args)
        {
            dockerRunArgsSb.AppendFormat(" {0}", arg);
        }

        string dockerRunArgs = dockerRunArgsSb.ToString();
        logger.Write($"\ndocker {dockerRunArgs}\n\n");

        using (Process docker = new Process())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = dockerRunArgs,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            docker.StartInfo = startInfo;
            docker.Start();

            while (!docker.StandardOutput.EndOfStream)
            {
                string line = docker.StandardOutput.ReadLine()!;
                logger.Write($"{line}\n");
            }
            docker.WaitForExit();
        }
    }

    public static void CreateImage(string imageName, string dockerfilePath,
                                   MultiIOLogger logger, params string[] args)
    {
        if (ImageExists(imageName))
        {
            logger.Write($"\nThe Docker Image {imageName} was found ready to use."
                        + " Skipping...\n");
            return ;
        }

        var dockerBuildArgsSb = new StringBuilder("build");
        dockerBuildArgsSb.AppendFormat(" --file {0}/Dockerfile", dockerfilePath);

        foreach (string arg in args)
        {
            dockerBuildArgsSb.AppendFormat(" --build-arg {0}", arg);
        }

        dockerBuildArgsSb.AppendFormat(" --tag {0}", imageName);
        dockerBuildArgsSb.AppendFormat(" {0}", dockerfilePath);

        string dockerBuildArgs = dockerBuildArgsSb.ToString();
        logger.Write($"\ndocker {dockerBuildArgs}\n\n");

        using (Process docker = new Process())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = dockerBuildArgs,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            docker.StartInfo = startInfo;
            docker.Start();

            while (!docker.StandardOutput.EndOfStream)
            {
                string line = docker.StandardOutput.ReadLine()!;
                logger.Write($"{line}\n");
            }
            docker.WaitForExit();
        }
    }

    private static bool ImageExists(string repositoryOrImageName)
    {
        FetchInstalledImages();
        return s_images.Any(img => img.Repository
                                      .Equals(repositoryOrImageName,
                                              System.StringComparison.OrdinalIgnoreCase));
    }

    private static void FetchInstalledImages()
    {
        string[] imagesList;

        using (Process docker = new Process())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "image ls --all",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            docker.StartInfo = startInfo;
            docker.Start();

            // We skip the first and last rows for the following reasons:
            // - First: It's the table header, which we don't require here.
            // - Last: Docker prints an empty line at the end of the output,
            //         which we don't require either.
            imagesList = docker.StandardOutput.ReadToEnd()
                                              .Split("\n")
                                              .Skip(1)
                                              .SkipLast(1)
                                              .ToArray();
            docker.WaitForExit();
        }

        // For now, we will assume that the images list doesn't change drastically
        // enough during one run of this app, to make this condition false.
        if (imagesList.Length == s_images.Count)
            return ;

        foreach (string imageData in imagesList)
        {
            // C# insisted in taking whitespace elements into account for Split().
            // Hence, we had to filter using IsNullOrWhitespace(), and apply Trim()
            // to the remaining ones.
            string[] fields = imageData.Split(' ')
                                       .Where(field => !string.IsNullOrWhiteSpace(field))
                                       .Select(field => field.Trim())
                                       .ToArray();

            // We are using Last() here because the "Created" field in the output
            // from 'docker image ls' has strings with spaces...
            DockerImage image = new DockerImage(fields[0], fields[1],
                                                fields[2], fields.Last());
            s_images.Add(image);
        }
    }
}
