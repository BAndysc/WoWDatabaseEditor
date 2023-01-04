using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace WDE.SqlWorkbench.Models;

/// <summary>
/// MySqlDecimal provided by MySqlConnector is internal, which sucks, so I had to copy it here.
/// </summary>
public readonly struct PublicMySqlDecimal : IComparable<PublicMySqlDecimal>
{
    public static readonly PublicMySqlDecimal Zero = default;

    /// <summary>
    /// Gets the original value of this <see cref="PublicMySqlDecimal"/> as a <see cref="string"/>.
    /// </summary>
    public override string ToString() => Value;

    public string Value => m_value ?? "0"; // null-coalescing operator for default struct value
    
    public bool IsNegative => m_negative;

    public string Whole => m_whole ?? "0";
    
    public string Fraction => m_fraction ?? "";
    
    private PublicMySqlDecimal(bool mNegative, string whole, string fraction)
    {
        m_negative = mNegative;
        m_whole = whole;
        m_fraction = fraction;
        m_value = (mNegative ? "-" : "") + $"{whole}{(fraction.Length > 0 ? "." : "")}{fraction}";
    }
    
    private static readonly Regex s_pattern = new(@"^-?0*([0-9]+)(\.([0-9]+))?$");

    private readonly string m_value;
    private readonly bool m_negative;
    // whole part without leading zeros (canonical form)
    private readonly string m_whole;
    // fraction part without trailing zeros and can't be equal to "0", but can be "" (canonical form)
    private readonly string? m_fraction;
    
    public static PublicMySqlDecimal FromDecimal(MySqlDecimal dec)
    {
        return Parse(dec.ToString());
    }
    
    public static PublicMySqlDecimal FromDecimal(decimal dec)
    {
        return Parse(dec.ToString("0.00", CultureInfo.InvariantCulture));
    }
    
    public static bool TryParse(string str, out PublicMySqlDecimal o, int? maxDecimalPlaces = null)
    {
        if (s_pattern.Match(str) is { Success: true } match)
        {
            var wholeLength = match.Groups[1].Length;
            var fractionLength = match.Groups[3].Value.TrimEnd('0').Length;

            if (maxDecimalPlaces.HasValue && fractionLength > maxDecimalPlaces.Value)
            {
                o = default;
                return false;
            }

            var isWithinLengthLimits = wholeLength + fractionLength <= 65 && fractionLength <= 30;
            var isNegative = str[0] == '-';
            var isNegativeZero = isNegative && match.Groups[1].Value == "0" && fractionLength == 0;
            if (isWithinLengthLimits && !isNegativeZero)
            {
                var whole = match.Groups[1].Value;
                var fraction = match.Groups[3].Value.TrimEnd('0');
                o = new PublicMySqlDecimal(isNegative, whole, fraction);
                return true;
            }
        }

        o = default;
        return false;
    }
    
    public static PublicMySqlDecimal Parse(string str)
    {
        if (TryParse(str, out var o))
            return o;
        throw new FormatException($"Could not parse the value as a PublicMySqlDecimal: {str}");
    }
    
    public static PublicMySqlDecimal FromSignWholeFractional(bool sign, string whole, string fractional)
    {
        if (whole.Any(x => x < '0' || x > '9') || fractional.Any(x => x < '0' || x > '9'))
            throw new FormatException("Whole or fractional part contains non-numeric characters");
        
        whole = whole.TrimStart('0');
        if (whole.Length == 0)
            whole = "0";
        fractional = fractional.TrimEnd('0');
        
        return new PublicMySqlDecimal(sign, whole, fractional);
    }
    
    public int CompareTo(PublicMySqlDecimal other)
    {
        if (m_negative != other.m_negative)
        {
            // Different signs, negative number is smaller if m_negative is true
            return m_negative ? -1 : 1;
        }

        int wholeComparison = CompareWholeParts(m_whole, other.m_whole);
        if (wholeComparison != 0)
        {
            // If signs are the same, return the comparison result for the whole parts
            return m_negative ? -wholeComparison : wholeComparison;
        }

        // Whole parts are equal, compare fractions
        string thisFraction = m_fraction ?? string.Empty;
        string otherFraction = other.m_fraction ?? string.Empty;

        int fractionComparison = CompareFractionalParts(thisFraction, otherFraction);
        return m_negative ? -fractionComparison : fractionComparison;
    }

    private static int CompareWholeParts(string thisWhole, string otherWhole)
    {
        if (thisWhole.Length != otherWhole.Length)
        {
            // Compare based on length if the lengths are different
            return thisWhole.Length.CompareTo(otherWhole.Length);
        }

        return string.CompareOrdinal(thisWhole, otherWhole);
    }

    private static int CompareFractionalParts(string thisFraction, string otherFraction)
    {
        int minLength = Math.Min(thisFraction.Length, otherFraction.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (thisFraction[i] != otherFraction[i])
            {
                // Compare numeric values at each index
                return thisFraction[i].CompareTo(otherFraction[i]);
            }
        }

        // If one fraction is longer, the longer one is larger
        return thisFraction.Length.CompareTo(otherFraction.Length);
    }
    
    public static bool operator <(PublicMySqlDecimal left, PublicMySqlDecimal right) => left.CompareTo(right) < 0;
    public static bool operator <=(PublicMySqlDecimal left, PublicMySqlDecimal right) => left.CompareTo(right) <= 0;
    public static bool operator >(PublicMySqlDecimal left, PublicMySqlDecimal right) => left.CompareTo(right) > 0;
    public static bool operator >=(PublicMySqlDecimal left, PublicMySqlDecimal right) => left.CompareTo(right) >= 0;
    public static bool operator ==(PublicMySqlDecimal left, PublicMySqlDecimal right) => left.CompareTo(right) == 0;
    public static bool operator !=(PublicMySqlDecimal left, PublicMySqlDecimal right) => left.CompareTo(right) != 0;

    public override int GetHashCode()
    {
        return 0;
    }

    public override bool Equals(object? obj)
    {
        if (obj is PublicMySqlDecimal other)
            return other == this;
        return false;
    }
}