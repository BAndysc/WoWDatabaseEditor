namespace WDE.SqlWorkbench.Services.SyntaxValidator;

internal readonly struct SyntaxError
{
    public readonly int Line;
    public readonly int Start;
    public readonly int Length;
    public readonly string Message;

    public SyntaxError(int line, int start, int length, string message)
    {
        Line = line;
        Start = start;
        Length = length;
        Message = message;
    }
}