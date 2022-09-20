// File: src/utilities/MultiIOLogger.cs
using System;
using System.IO;

namespace DotnetBenchmarker;

// Class: MultiIOLogger
// This class used as a printer instead of the usual 'Console' class. This one
// is made to print to the console and record to a log at the same time. Note
// that it expects its input including whatever newlines you might want to add.
// In other words, it calls implicitly Write(), not WriteLine(). This, to give
// the user the most control over the output, and to keep all printing source
// code uniform throughout the app.

public class MultiIOLogger
{
    private StreamWriter _writer;

    public MultiIOLogger(string logPath)
    {
        // The 'logPath' path is guaranteed to exist, as it should be created in
        // the 'PrepareResourcesTree()' function in the main script. If it isn't,
        // then that's a bug we've got to take a look at.
        _writer = new StreamWriter(logPath);
        _writer.AutoFlush = true;
    }

    ~MultiIOLogger()
    {
        // Don't leak memory :)
        _writer.Close();
    }

    public void Write(string text)
    {
        _writer.Write(text);
        Console.Write(text);
    }
}
