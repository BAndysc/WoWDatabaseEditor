using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace WDE.SourceCodeIntegrationEditor.Utils;

public static class FileHelper
{
    public static string? ExtractNthLine(string filename, int n, int maxLength)
    {
        if (n < 1) throw new ArgumentOutOfRangeException(nameof(n), "Line number must be greater than 0.");

        long fileLength = new FileInfo(filename).Length;
        using var mmf =
            MemoryMappedFile.CreateFromFile(filename, FileMode.Open, null, fileLength, MemoryMappedFileAccess.Read);
        long offset = 0;
        long lineStartOffset = 0;
        long lineEndOffset = 0;
        const int bufferSize = 1024;
        long bytesLeft = fileLength;
        int lineNumber = 1;
        bool isLineStartFound = n == 1;
        bool isLineEndFound = false;

        Span<byte> buffer = stackalloc byte[bufferSize];

        while (!isLineStartFound || !isLineEndFound)
        {
            using var accessor = mmf.CreateViewStream(offset, Math.Min(bufferSize, bytesLeft), MemoryMappedFileAccess.Read);
            var readBytes = accessor.Read(buffer);
            bytesLeft -= readBytes;

            for (int i = 0; i < readBytes; i++)
            {
                char c = (char)buffer[i];
                if (c == '\n')
                {
                    lineNumber++;
                    if (lineNumber == n && !isLineStartFound)
                    {
                        lineStartOffset = offset + i + 1;
                        isLineStartFound = true;
                    }
                    else if (isLineStartFound)
                    {
                        lineEndOffset = offset + i;
                        isLineEndFound = true;
                        break;
                    }
                }
            }

            offset += readBytes;

            if (readBytes < buffer.Length)
                break;
        }

        if (!isLineEndFound)
        {
            lineEndOffset = fileLength;
        }

        if (isLineStartFound)
        {
            var lineLength = Math.Clamp((int)(lineEndOffset - lineStartOffset), 0, maxLength);
            Span<byte> lineBuffer = stackalloc byte[lineLength];
            using var accessor = mmf.CreateViewStream(lineStartOffset, lineLength, MemoryMappedFileAccess.Read);
            lineLength = accessor.Read(lineBuffer);
            if (lineLength == 0)
                return null;
            if (lineBuffer[lineLength - 1] == '\r')
                lineLength--;
            return Encoding.UTF8.GetString(lineBuffer.Slice(0, lineLength));
        }

        return null;
    }
}