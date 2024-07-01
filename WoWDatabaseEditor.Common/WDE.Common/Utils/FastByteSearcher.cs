using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace WDE.Common.Utils;

public class FastByteSearcher
{
    private readonly bool caseInsensitive;
    private readonly bool wholeWorld;
    private int[] partialMatchTable;
    private int[] partialMatchTableReversed;
    private byte[] needleBytes;

    public FastByteSearcher(string needle, bool caseInsensitive, bool wholeWorld)
    {
        this.caseInsensitive = caseInsensitive;
        this.wholeWorld = wholeWorld;
        needleBytes = Encoding.UTF8.GetBytes(caseInsensitive ? needle.ToLowerInvariant() : needle);
        partialMatchTable = new int[needleBytes.Length];
        BuildPartialMatchTable(needleBytes, partialMatchTable);
        partialMatchTableReversed = new int[needleBytes.Length];
        BuildPartialMatchTable2(needleBytes, partialMatchTableReversed);
    }

    public readonly struct Result
    {
        public readonly long lineNumber;
        public readonly long offset;
        public readonly string line;

        public Result(long lineNumber, long offset, string line)
        {
            this.lineNumber = lineNumber;
            this.offset = offset;
            this.line = line;
        }
    }

    public struct SingleItemList<T> : IList<T>
    {
        private T item = default!;
        private int count = 0;

        public SingleItemList()
        {
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (count == 0)
                yield break;

            yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            this.item = item;
            count = 1;
        }

        public void Clear()
        {
            count = 0;
            item = default!;
        }

        public bool Contains(T item)
        {
            return count == 1 && EqualityComparer<T>.Default.Equals(this.item, item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (count == 1)
                array[arrayIndex] = item;
        }

        public bool Remove(T item)
        {
            if (count == 1 && EqualityComparer<T>.Default.Equals(this.item, item))
            {
                count = 0;
                this.item = default!;
                return true;
            }

            return false;
        }

        public int Count => count;

        public bool IsReadOnly => false;

        public int IndexOf(T item) => count == 1 && EqualityComparer<T>.Default.Equals(this.item, item) ? 0 : -1;

        public void Insert(int index, T item)
        {
            if (index == 0 && count == 0)
            {
                this.item = item;
                count = 1;
            }
            else
                throw new IndexOutOfRangeException();
        }

        public void RemoveAt(int index)
        {
            if (index == 0)
            {
                count = 0;
                item = default!;
            }
            else
                throw new IndexOutOfRangeException();
        }

        public T this[int index]
        {
            get => index == 0 && count == 1 ? item : throw new IndexOutOfRangeException();
            set
            {
                if (index == 0 && count == 1)
                    item = value;
                else
                    throw new IndexOutOfRangeException();
            }
        }
    }

    public struct OpenedFileSearcher : System.IDisposable
    {
        private readonly FastByteSearcher data;
        private MemoryMappedFile? mmapFile;
        public readonly long FileLength;

        public OpenedFileSearcher(FastByteSearcher data, string mmapFilePath)
        {
            this.data = data;
            if (!File.Exists(mmapFilePath))
                throw new FileNotFoundException("File not found", mmapFilePath);

            FileLength = new FileInfo(mmapFilePath).Length;

            if (FileLength > 0)
                mmapFile = MemoryMappedFile.CreateFromFile(mmapFilePath, FileMode.Open, null, FileLength, MemoryMappedFileAccess.Read);
        }

        public Result? FindOne(long byteOffset, int? maxLength, bool reversed)
        {
            if (mmapFile == null)
                return null;
            var temporaryBuffer = ArrayPool<byte>.Shared.Rent(1024 * 1000);

            try
            {
                var results = new SingleItemList<Result>();
                IndexOfForAll(mmapFile, FileLength, data.needleBytes, reversed? data.partialMatchTableReversed : data.partialMatchTable, byteOffset, 1, maxLength, data.wholeWorld, data.caseInsensitive, reversed, temporaryBuffer, ref results);
                return results.Count == 1 ? results[0] : null;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(temporaryBuffer);
            }
        }

        public void Search(bool reversed, byte[]? temporaryBuffer, List<Result> results)
        {
            if (mmapFile == null)
                return;

            bool pooledBuffer = false;
            if (temporaryBuffer == null)
            {
                temporaryBuffer = ArrayPool<byte>.Shared.Rent(1024 * 10);
                pooledBuffer = true;
            }

            try
            {
                IndexOfForAll(mmapFile, FileLength, data.needleBytes, reversed ? data.partialMatchTableReversed : data.partialMatchTable, 0, null, null, data.wholeWorld, data.caseInsensitive, reversed, temporaryBuffer, ref results);
            }
            finally
            {
                if (pooledBuffer)
                    ArrayPool<byte>.Shared.Return(temporaryBuffer);
            }
        }

        public void Dispose()
        {
            mmapFile?.Dispose();
        }
    }

    public OpenedFileSearcher OpenFile(string fileName)
    {
        OpenedFileSearcher searcher = new(this, fileName);
        return searcher;
    }

    public Result? FindOne(string mmapFilePath, long byteOffset, int? maxLength, bool reversed)
    {
        using var file = OpenFile(mmapFilePath);
        return file.FindOne(byteOffset, maxLength, reversed);
    }

    public void Search(string mmapFilePath, bool reversed, byte[]? temporaryBuffer, List<Result> results)
    {
        using var file = OpenFile(mmapFilePath);
        file.Search(reversed, temporaryBuffer, results);
    }

    internal static unsafe void IndexOfForAll<T>(MemoryMappedFile mmapFile, long fileLength, byte[] needle, int[] partialMatchTable, long startOffset, int? maxResults, int? maxReadLength, bool wholeWorld, bool caseInsensitive, bool reversed, byte[] buffer, ref T results) where T : IList<Result>
    {
        long baseViewSize = buffer.Length;
        long adjustedViewSize = baseViewSize;

        if (needle.Length > adjustedViewSize)
            throw new ArgumentException("Needle length is greater than the adjusted view size");

        if (maxReadLength.HasValue)
            fileLength = Math.Min(fileLength, maxReadLength.Value + startOffset);

        if (reversed)
            startOffset -= adjustedViewSize;

        // Pinning the arrays actually gives better performance
        fixed (byte* needlePtr = needle)
        {
            fixed (int* partialMatchTablePtr = partialMatchTable)
            {
                fixed (byte* bufferPtr = buffer)
                {
                    // Adjust the batch size dynamically based on the needle's length
                    long overlap = needle.Length - 1;
                    long lineNumber = 0;

                    for (long offset = startOffset; !reversed ? offset < fileLength : offset + adjustedViewSize > 0; offset += (adjustedViewSize - overlap) * (reversed ? -1 : 1))
                    {
                        bool last = false;
                        long sizeToRead = Math.Min(adjustedViewSize, fileLength - offset);
                        if (offset < 0)
                        {
                            sizeToRead += offset;
                            offset = 0;
                            last = true;
                        }
                        using (var accessor = mmapFile.CreateViewAccessor(offset, sizeToRead, MemoryMappedFileAccess.Read))
                        {
                            int readBytes = accessor.ReadArray(0, buffer, 0, (int)sizeToRead);
                            if (caseInsensitive)
                                AsciiUtils.TransformLowercase(bufferPtr, readBytes);

                            long start = 0;
                            long length = readBytes;
                            do
                            {
                                long result = reversed ?
                                    ReverseKmpSearch(bufferPtr + start, (int)length, needlePtr, needle.Length, partialMatchTablePtr, partialMatchTable.Length) :
                                    KmpSearch(bufferPtr + start, (int)length, needlePtr, needle.Length, partialMatchTablePtr, partialMatchTable.Length);

                                if (result != -1)
                                {
                                    result += start;
                                    lineNumber += buffer.AsSpan((int)start, (int)(result - start)).Count((byte)'\n');

                                    bool isStartValid = true;
                                    bool isEndValid = true;
                                    if (wholeWorld)
                                    {
                                        isStartValid = result == 0 || (!Char.IsLetterOrDigit((char)bufferPtr[result - 1]) && bufferPtr[result - 1] != '.');
                                        isEndValid = result + needle.Length == readBytes ||
                                                     (!Char.IsLetterOrDigit((char)bufferPtr[result + needle.Length]) &&
                                                      bufferPtr[result + needle.Length] != '.');
                                    }

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
                                        results.Add(new Result(lineNumber, offset + result, line));
                                        if (maxResults.HasValue && results.Count == maxResults)
                                            return;
                                    }

                                    if (reversed)
                                    {
                                        length = result + needle.Length - 1;
                                    }
                                    else
                                    {
                                        start = result + 1;
                                        length = readBytes - (int)start;
                                    }
                                }
                                else
                                {
                                    if ((int)(readBytes - overlap - start) > 0)
                                        lineNumber += buffer.AsSpan((int)start, (int)(readBytes - overlap - start)).Count((byte)'\n');
                                    break;
                                }
                            } while (true);
                        }

                        if (last)
                            break;

                        // Avoid reading past the end of the file
                        if (!reversed && offset + adjustedViewSize >= fileLength)
                        {
                            break;
                        }
                    }
                }
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


    private static void BuildPartialMatchTable2(byte[] pat, Span<int> lps)
    {
        int length = pat.Length;
        lps[0] = -1;
        int j = -1;

        for (int i = 1; i < length; i++) {
            while (j >= 0 && pat[i] != pat[j]) {
                j = lps[j];
            }
            j++;
            lps[i] = j;
        }
    }

    private static unsafe long ReverseKmpSearch(byte* haystack, int haystackLength, byte* needle, int needleLength, int* partialMatchTable, int partialMatchTableLength)
    {
        int i = haystackLength - 1; // start index for haystack from the end
        int j = needleLength - 1;   // start index for needle from the end

        while (i >= 0) {
            if (needle[j] == haystack[i]) {
                j--;
                i--;
                if (j < 0) {
                    return i + 1; // Found pattern at index (i + 1)
                }
            } else {
                if (j < needleLength - 1) {
                    if (j + 1 < partialMatchTableLength) {
                        j = partialMatchTable[j + 1] - 1;
                    } else {
                        j = needleLength - 1; // Reset to the end of the needle if out of bounds
                    }
                    if (j == -1) {
                        j = needleLength - 1;
                        i--;
                    }
                } else {
                    i--;
                }
            }
        }
        return -1; // Pattern not found
    }

    private static unsafe long KmpSearch(byte* haystack, int haystackLength, byte* needle, int needleLength, int* partialMatchTable, int partialMatchTableLength)
    {
        int i = 0; // index for txt[]
        int j = 0; // index for pat[]
        while ((haystackLength - i) >= (needleLength - j))
        {
            if (needle[j] == haystack[i])
            {
                j++;
                i++;
            }

            if (j == needleLength)
            {
                return i - j;
                //j = partialMatchTable[j - 1];
            }

            // mismatch after j matches
            else if (i < haystackLength && needle[j] != haystack[i]) {
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