namespace WDE.Common.Services;

public readonly record struct FileHandle
{
    private readonly int index;

    public FileHandle(int index) => this.index = index + 1;

    public int Index => index - 1;

    public override string ToString() => $"FH[{index}]";

    public static FileHandle Invalid => new(-1);
}