using System;
using System.Numerics;

namespace WDE.Common.Utils;

public static class NumbersExtensions
{
    private static int maxLength = "18446744073709551615".Length + 2; // +2 for a sign

    /// <summary>
    /// Non alloc function which returns true if the number string representation contains the given string
    /// </summary>
    /// <param name="number"></param>
    /// <param name="str"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool Contains<T>(this T number, string str) where T : INumber<T>
    {
        if (str.ContainsNonDigit())
            return false;
        Span<char> output = stackalloc char[maxLength];
        number.TryFormat(output, out var chars, default, null);
        var numberAsStringSpan = (ReadOnlySpan<char>)output[..chars];
        return numberAsStringSpan.Contains(str.AsSpan(), StringComparison.Ordinal);
    }
    
    /// <summary>
    /// Non alloc function which returns true if the number string representation is exactly the given string
    /// </summary>
    /// <param name="number"></param>
    /// <param name="str"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool Is<T>(this T number, string str) where T : INumber<T>
    {
        if (str.ContainsNonDigit())
            return false;
        Span<char> output = stackalloc char[maxLength];
        number.TryFormat(output, out var chars, default, null);
        var numberAsStringSpan = (ReadOnlySpan<char>)output[..chars];
        return numberAsStringSpan.Equals(str.AsSpan(), StringComparison.Ordinal);
    }

    private static bool ContainsNonDigit(this string str)
    {
        int length = str.Length;
        int i = 0;
        if (str[0] == '-')
            i++;
        for (; i < length; ++i)
        {
            if (!char.IsDigit(str[i]))
                return true;
        }

        return false;
    }
}