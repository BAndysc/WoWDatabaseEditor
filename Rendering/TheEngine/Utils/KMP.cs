namespace TheEngine.Utils;

internal class KMP
{
    // todo: this is copy paste from FastByteSearcher in WDE.Common, worth merging?

    internal static unsafe long KmpSearch(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle, ReadOnlySpan<int> partialMatchTable)
    {
        static byte ToLowerAscii(byte b) => (b >= 65 && b <= 90) ? (byte)(b + 32) : b;
        int i = 0; // index for txt[]
        int j = 0; // index for pat[]
        while ((haystack.Length - i) >= (needle.Length - j))
        {
            if (ToLowerAscii(needle[j]) == ToLowerAscii(haystack[i]))
            {
                j++;
                i++;
            }

            if (j == needle.Length)
            {
                return i - j;
                //j = partialMatchTable[j - 1];
            }

            // mismatch after j matches
            else if (i < haystack.Length && ToLowerAscii(needle[j]) != ToLowerAscii(haystack[i])) {
                // Do not match lps[0..lps[j-1]] characters,
                // they will match anyway
                if (j != 0)
                    j = partialMatchTable[j - 1];
                else
                    i = i + 1;
            }
        }

        return -1;
    }

    internal static void BuildPartialMatchTable(ReadOnlySpan<byte> pat, Span<int> lps)
    {
        if (lps.Length < pat.Length)
            throw new ArgumentException("lps must be at least as long as pat");
        // length of the previous longest prefix suffix
        int len = 0;

        lps[0] = 0; // lps[0] is always 0

        // the loop calculates lps[i] for i = 1 to M-1
        int i = 1;
        while (i < pat.Length)
        {
            if (pat[i] == pat[len])
            {
                len++;
                lps[i] = len;
                i++;
            }
            else // (pat[i] != pat[len])
            {
                // This is tricky. Consider the example.
                // AAACAAAA and i = 7. The idea is similar
                // to search step.
                if (len != 0) {
                    len = lps[len - 1];

                    // Also, note that we do not increment
                    // i here
                }
                else // if (len == 0)
                {
                    lps[i] = 0;
                    i++;
                }
            }
        }
    }
}