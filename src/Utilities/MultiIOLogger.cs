// File: src/Utilities/MultiIOLogger.cs
using System;
using System.IO;

// Class: MultiIOLogger
public class MultiIOLogger
{
    public string LogFilename { get; }
    private StreamWriter _writer;

    public MultiIOLogger(string logName)
    {
        LogFilename = logName;
        _writer = new StreamWriter(logName);
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
