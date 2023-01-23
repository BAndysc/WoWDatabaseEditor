using System;
using Prism.Commands;

namespace WDE.Common.Types;

public class NamedDelegateCommand : DelegateCommand, INamedCommand
{
    public NamedDelegateCommand(string name, Action executeMethod) : base(executeMethod)
    {
        Name = name;
    }

    public NamedDelegateCommand(string name, Action executeMethod, Func<bool> canExecuteMethod) : base(executeMethod, canExecuteMethod)
    {
        Name = name;
    }

    public string Name { get; set; }
}

public class NamedDelegateCommand<T> : DelegateCommand<T>, INamedCommand<T>
{
    public NamedDelegateCommand(string name, Action<T> executeMethod) : base(executeMethod)
    {
        Name = name;
    }

    public NamedDelegateCommand(string name, Action<T> executeMethod, Func<T, bool> canExecuteMethod) : base(executeMethod, canExecuteMethod)
    {
        Name = name;
    }

    public string Name { get; set; }
}