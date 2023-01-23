using System.Windows.Input;

namespace WDE.Common.Types;

public interface INamedCommand : ICommand
{
    string Name { get; }
}

public interface INamedCommand<T> : INamedCommand
{
}