// File: src/Utilities/ListyExtensions.cs
using System.Collections.Generic;
using System.Linq;

namespace DotnetBenchmarker;

// Class: ListyExtensions
static class ListyExtensions
{
    public static bool IsEmpty<T>(this IEnumerable<T> collection)
    {
        return collection.Count() == 0;
    }
}
