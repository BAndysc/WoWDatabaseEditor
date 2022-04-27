using System;
using System.Buffers;
using System.Text;

namespace WDE.Common.Utils;

public struct Tokenizer
{
    private readonly string source;
    private int index;

    public Tokenizer(string source)
    {
        this.source = source;
        index = 0;
        SkipSpaces();
    }

    private void SkipSpaces()
    {
        while (index < source.Length && char.IsWhiteSpace(source[index]))
            index++;
    }
    
    public ReadOnlySpan<char> Next()
    {
        if (index >= source.Length)
            return ReadOnlySpan<char>.Empty;

        SkipSpaces();
        
        ReadOnlySpan<char> result = ReadOnlySpan<char>.Empty;
        if (source[index] == '"')
        {
            result = ReadQuotedString();
        }
        else
        {
            result = ReadUnquotedString();
        }
        SkipSpaces();
        return result;
    }

    private ReadOnlySpan<char> ReadUnquotedString()
    {
        int indexOfSpace = source.IndexOf(' ', index);
        if (indexOfSpace == -1)
            indexOfSpace = source.Length;
        int start = index;
        index = indexOfSpace + 1;
        return source.AsSpan(start, indexOfSpace - start);
    }

    private ReadOnlySpan<char> ReadQuotedString()
    {
        int start = index;
        int nextQuote = source.IndexOf('"', index + 1);
        if (nextQuote == -1)
        {
            index = source.Length;
            return source.AsSpan(start + 1);
        }
        
        // we have escaping, so we need to analyse the string
        if (source[nextQuote - 1] == '\\')
        {
            return ReadEscapedString();
        }
        else
        {
            var result = source.AsSpan(start + 1, nextQuote - start - 1);
            var indexOfSlash = result.IndexOf('\\');
            if (indexOfSlash == -1) // simple, no escaping
            {
                index = nextQuote + 1;
                return result;
            }
            else
                return ReadEscapedString();
        }
    }

    private ReadOnlySpan<char> ReadEscapedString()
    {
        StringBuilder sb = new();
        index++;
        bool isEscaping = false;
        while (index < source.Length)
        {
            if (!isEscaping && source[index] == '\\')
            {
                isEscaping = true;
                index++;
            }
            else if (isEscaping)
            {
                sb.Append(source[index]);
                index++;
                isEscaping = false;
            }
            else if (!isEscaping && source[index] == '"')
            {
                index++;
                break;
            }
            else
            {
                sb.Append(source[index]);
                index++;
            }
        }

        return sb.ToString();
    }

    public string Remaining()
    {
        SkipSpaces();
        if (index >= source.Length)
            return "";
        var start = index;
        index = source.Length;
        return source.Substring(start);
    }
}