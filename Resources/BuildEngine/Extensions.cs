// File: Extensions.cs
using System.Linq;
using System.Text;

// Class: Extensions
public static class Extensions
{
    public static string ToUpperSnakeCase(this string str)
    {
        return string.Concat(str.Select((letter, index) =>
                                    index > 0 && char.IsUpper(letter)
                                  ? "_" + letter.ToString()
                                  : letter.ToString()))
                     .ToUpperInvariant();
    }

    public static bool IsEmpty(this StringBuilder sb)
    {
        return sb.Length == 0;
    }
}
