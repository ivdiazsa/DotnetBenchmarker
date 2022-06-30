// File: src/Utilities/ListyExtensions.cs

// Class: ListyExtensions
static class ListyExtensions
{
    public static T[] Prepend<T>(this T[] array, T newElement)
    {
        var result = new T[array.Length + 1];
        result[0] = newElement;
        for (int i = 1; i < result.Length; i++)
        {
            result[i] = array[i - 1];
        }
        return result;
    }
}
