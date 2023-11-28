using Antlr4.Runtime;
using AvaloniaEdit.Document;

namespace WDE.SqlWorkbench.Services.QueryUtils;

internal class NoCopyStringCharStream : BaseInputCharStream
{
    private string str;

    public NoCopyStringCharStream(string str)
    {
        this.str = str;
        this.n = str.Length;
    }
    
    public void Reset(string str)
    {
        base.Reset();
        this.str = str;
        this.n = str.Length;
    }
    
    protected override int ValueAt(int i)
    {
        return str[i];
    }

    protected override string ConvertDataToString(int start, int count)
    {
        return str.Substring(start, count);
    }
}