using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace WDE.SourceCodeIntegrationEditor.SourceCode;

internal class FastByteSearcher
{
    // performs a very fast search for a text in a file
    // doesn't load the whole file into memory
    // doesn't allocate any memory
    internal static void IndexOfWholeWorldForAll(string mmapFilePath, byte[] needle, byte[] buffer, List<(long lineNumber, long offset, string line)> results)
    {
        Span<int> partialMatchTable = stackalloc int[needle.Length];
        BuildPartialMatchTable(needle, partialMatchTable);
        long baseViewSize = buffer.Length;
        long adjustedViewSize = baseViewSize;

        if (needle.Length > adjustedViewSize)
        {
            throw new ArgumentException("Needle length is greater than the adjusted view size");
        }

        if (!File.Exists(mmapFilePath))
            return;

        long fileLength = new FileInfo(mmapFilePath).Length;
        if (fileLength == 0)
            return;
        using var mmapFile = MemoryMappedFile.CreateFromFile(mmapFilePath, FileMode.Open, null, fileLength, MemoryMappedFileAccess.Read);

        // Adjust the batch size dynamically based on the needle's length
        long overlap = needle.Length - 1;
        long lineNumber = 0;

        for (long offset = 0; offset < fileLength; offset += adjustedViewSize - overlap)
        {
            long sizeToRead = Math.Min(adjustedViewSize, fileLength - offset);
            using (var accessor = mmapFile.CreateViewAccessor(offset, sizeToRead, MemoryMappedFileAccess.Read))
            {
                int readBytes = accessor.ReadArray(0, buffer, 0, (int)sizeToRead);

                long start = 0;
                long length = readBytes;
                do
                {
                    long result = KmpSearch(buffer.AsSpan((int)start, (int)length), needle, partialMatchTable);
                    if (result != -1)
                    {
                        result += start;
                        for (long i = start; i < result; ++i)
                        {
                            if (buffer[i] == '\n')
                                lineNumber++;
                        }

                        bool isStartValid = result == 0 || (!Char.IsLetterOrDigit((char)buffer[result - 1]) && buffer[result - 1] != '.');
                        bool isEndValid = result + needle.Length == readBytes ||
                                          (!Char.IsLetterOrDigit((char)buffer[result + needle.Length]) &&
                                           buffer[result + needle.Length] != '.');
                        if (isStartValid && isEndValid)
                        {
                            var firstNewLine = buffer.AsSpan(0, (int)result).LastIndexOf((byte)'\n');
                            var nextNewLine = buffer.AsSpan((int)result).IndexOf((byte)'\n');
                            if (firstNewLine == -1)
                                firstNewLine = 0;
                            else
                                firstNewLine ++; // skip \n
                            if (nextNewLine == -1)
                                nextNewLine = readBytes;
                            else
                                nextNewLine += (int)result;
                            var line = Encoding.UTF8.GetString(buffer, firstNewLine, (int)(nextNewLine - firstNewLine));
                            results.Add((lineNumber, offset + result, line));
                        }

                        start = result + 1;
                        length = readBytes - (int)start;
                    }
                    else
                    {
                        for (long i = start; i < readBytes - overlap; ++i)
                        {
                            if (buffer[i] == '\n')
                                lineNumber++;
                        }
                        break;
                    }
                } while (true);
            }

            // Avoid reading past the end of the file
            if (offset + adjustedViewSize >= fileLength)
            {
                break;
            }
        }
    }

    private static void BuildPartialMatchTable(byte[] pat, Span<int> lps)
    {
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

    private static long KmpSearch(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle, ReadOnlySpan<int> partialMatchTable)
    {
        int M = needle.Length;
        int N = haystack.Length;

        int i = 0; // index for txt[]
        int j = 0; // index for pat[]
        while ((N - i) >= (M - j))
        {
            if (needle[j] == haystack[i])
            {
                j++;
                i++;
            }

            if (j == M)
            {
                return i - j;
                //j = partialMatchTable[j - 1];
            }

            // mismatch after j matches
            else if (i < N && needle[j] != haystack[i]) {
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
}