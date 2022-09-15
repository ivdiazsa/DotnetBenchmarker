// File: src/Utilities/StringExtensions.cs

namespace DotnetBenchmarker;

// Class: StringExtensions
static class StringExtensions
{
    public static string Capitalize(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        value = value.ToLower();
        char capitalLetter = char.ToUpper(value[0]);
        return value.Length == 1 ? capitalLetter.ToString()
                                 : capitalLetter + value.Substring(1);
    }

    public static string DefaultIfEmpty(this string value, string defy)
    {
        if (string.IsNullOrWhiteSpace(value)) return defy;
        return value;
    }
}
