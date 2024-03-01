using System;
using Prism.Commands;

namespace WDE.Common.Types;

public class NamedDelegateCommand : DelegateCommand, INamedCommand
{
    public NamedDelegateCommand(string name, ImageUri? icon, Action executeMethod) : base(executeMethod)
    {
        Name = name;
        Icon = icon;
    }

    public NamedDelegateCommand(string name, ImageUri? icon, Action executeMethod, Func<bool> canExecuteMethod) : base(executeMethod, canExecuteMethod)
    {
        Name = name;
        Icon = icon;
    }

    public string Name { get; set; }
    public ImageUri? Icon { get; }
}

public class NamedDelegateCommand<T> : DelegateCommand<T>, INamedCommand<T>
{
    public NamedDelegateCommand(string name, ImageUri? icon, Action<T> executeMethod) : base(executeMethod)
    {
        Name = name;
        Icon = icon;
    }

    public NamedDelegateCommand(string name, ImageUri? icon, Action<T> executeMethod, Func<T, bool> canExecuteMethod) : base(executeMethod, canExecuteMethod)
    {
        Name = name;
        Icon = icon;
    }

    public string Name { get; }
    public ImageUri? Icon { get; }
}