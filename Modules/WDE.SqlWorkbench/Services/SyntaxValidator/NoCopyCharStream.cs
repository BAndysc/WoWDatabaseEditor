using Antlr4.Runtime;
using AvaloniaEdit.Document;

namespace WDE.SqlWorkbench.Services.SyntaxValidator;

internal class NoCopyCharStream : BaseInputCharStream
{
    private ITextSource view;
    private int offset;

    public NoCopyCharStream(ITextSource view, int offset, int length)
    {
        this.view = view;
        this.offset = offset;
        this.n = length;
    }
    
    public void Reset(ITextSource view, int offset, int length)
    {
        base.Reset();
        this.view = view;
        this.offset = offset;
        this.n = length;
    }
    
    protected override int ValueAt(int i)
    {
        if (i >= n)
            return 0;
        return view.GetCharAt(i + offset);
    }

    protected override string ConvertDataToString(int start, int count)
    {
        return view.GetText(start + offset, count);
    }
}