using System.Text;

namespace ProtoZeroGenerator;

public class CodeGenerator
{
    private readonly StringBuilder _sb = new StringBuilder();
    private int _indentLevel = 0;
    private const string IndentString = "    ";

    public CodeGenerator AppendLine(string line = "")
    {
        if (string.IsNullOrEmpty(line))
        {
            _sb.AppendLine();
        }
        else
        {
            _sb.AppendLine(new string(' ', _indentLevel * IndentString.Length) + line);
        }
        return this;
    }

    public CodeGenerator OpenBlock(string? entry = null)
    {
        if (entry != null)
            AppendLine(entry);
        AppendLine("{");
        _indentLevel++;
        return this;
    }

    public CodeGenerator CloseBlock()
    {
        _indentLevel--;
        AppendLine("}");
        return this;
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}