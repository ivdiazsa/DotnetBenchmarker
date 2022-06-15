// File: src/Utilities/ProcessExtensions.cs
using System.Diagnostics;
using System.Text;

// Class: ProcessExtensions
static class ProcessExtensions
{
    public static ProcessStartInfo BaseTemplate(this ProcessStartInfo value,
                                                string fileName,
                                                string argsAsStr)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = argsAsStr,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        return startInfo;
    }
}
