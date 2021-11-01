namespace WDE.Common.Services.CommandLine
{
    public interface ICommandLineArgs
    {
        bool IsArgumentSet(string argument);
        string? GetValue(string argument);
        void Init(string[] args);
    }
}