using System;

namespace WDE.SqlWorkbench.Services.SqlDump;

internal class ArgumentAttribute : Attribute
{
    public ArgumentAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

internal class SkipWhenFalseAttribute : Attribute
{
    public SkipWhenFalseAttribute()
    {
    }
}