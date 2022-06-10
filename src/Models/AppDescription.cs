// File: src/Models/AppDescription.cs
using System.Collections.Generic;

// Class: AppDescription
public class AppDescription
{
    // Class definition goes here.
    public List<Configuration> Configurations { get; set; }

    public AppDescription()
    {
        Configurations = new List<Configuration>();
    }
}
