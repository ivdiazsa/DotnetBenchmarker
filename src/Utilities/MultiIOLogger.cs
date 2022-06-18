// File: src/Utilities/MultiIOLogger.cs
using System;
using System.IO;

// Class: MultiIOLogger
public class MultiIOLogger
{
    public string LogPath { get; }
    private StreamWriter _writer;

    public MultiIOLogger(string logPath)
    {
        string? logDir = Path.GetDirectoryName(logPath);

        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir!);

        LogPath = logPath;
        _writer = new StreamWriter(logPath);
        _writer.AutoFlush = true;
    }

    ~MultiIOLogger()
    {
        _writer.Close();
    }

    public void Write(string text)
    {
        _writer.Write(text);
        Console.Write(text);
    }
}
