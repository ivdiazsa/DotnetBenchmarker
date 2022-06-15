// File: src/Utilities/StringExtensions.cs
using System.Linq;

// Class: StringExtensions
static class StringExtensions
{
    public static string Capitalize(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        char capitalLetter = char.ToUpper(value[0]);
        return value.Length == 1 ? capitalLetter.ToString()
                                 : capitalLetter + value.Substring(1);
    }

    public static string CleanControlChars(this string value)
    {
        return new string(value.Where(c => !char.IsControl(c)).ToArray());
    }
}
