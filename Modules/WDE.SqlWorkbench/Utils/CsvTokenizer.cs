using System.Text;
using MySqlConnector;

namespace WDE.SqlWorkbench.Utils;

public class CsvWriter
{
    private readonly StringBuilder sb = new StringBuilder();
    private bool first = true;
    private bool pendingLine = false;

    public void Write(string? value)
    {
        if (pendingLine)
        {
            sb.Append('\n');
            pendingLine = false;
        }
        if (!first)
            sb.Append('\t');
        first = false;
        
        if (value == null)
        {
            sb.Append("NULL");
            return;
        }
        
        if (value == "NULL" || value.Contains('\n') || value.Contains('\t') || value.Contains('"'))
        {
            sb.Append('"');
            sb.Append(MySqlHelper.EscapeString(value));
            sb.Append('"');
        }
        else
        {
            sb.Append(value);
        }
    }

    public void WriteLine()
    {
        pendingLine = true;
        first = true;
    }

    public override string ToString()
    {
        return sb.ToString();
    }
}

public class CsvTokenizer
{
    private readonly string source;
    private int startTokenIndex;

    public CsvTokenizer(string source)
    {
        this.source = source;
    }

    public bool HasNextLine()
    {
        return startTokenIndex < source.Length;
    }

    public void NextLine()
    {
        if (startTokenIndex < source.Length && source[startTokenIndex] == '\n')
            startTokenIndex++;
    }

    public bool HasNextToken()
    {
        return startTokenIndex < source.Length && source[startTokenIndex] != '\n';
    }
    
    public string? NextToken()
    {
        int start = startTokenIndex;
        var end = ScanNextToken(startTokenIndex);
        
        if (end < source.Length && source[end] == '\n')
            startTokenIndex = end;
        else
            startTokenIndex = end + 1;
        
        if (start == end)
        {
            return "";
        }
        
        if (source[start] == '"')
        {
            start++;
            end--;

            return source.UnescapeString(start, end);
        }
        
        var token = source[start..end];
        if (token == "NULL")
            return null;
        
        return token;
    }
    
    private int ScanNextToken(int start)
    {
        int i = start;
        var acceptEscape = source[start] == '"';
        if (acceptEscape)
            i++;
        while (i < source.Length)
        {
            if (acceptEscape && source[i] == '\\')
            {
                i++;
                continue;
            }
            if (source[i] == '"')
            {
                acceptEscape = false;
            }
            if (!acceptEscape && (source[i] == '\t' || source[i] == '\n'))
                return i;
            i++;
        }
        return source.Length;
    }
}