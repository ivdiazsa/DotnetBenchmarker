// File: src/Models/DockerImage.cs

// Structure: DockerImage
internal static partial class DockerLauncher
{
    private struct DockerImage
    {
        // Struct definition goes here.
        public DockerImage(string repo, string tag, string id, string size)
        {
            Repository = repo;
            Tag = tag;
            ImageID = id;
            Size = size;
        }

        public string Repository { get; }
        public string Tag { get; }
        public string ImageID { get; }
        public string Size { get; }

        public override string ToString()
        {
            string desc = $"Repository: {Repository}\n"
                        + $"Tag: {Tag}\n"
                        + $"Image ID: {ImageID}\n"
                        + $"Size: {Size}\n";
            return desc;
        }
    }
}
