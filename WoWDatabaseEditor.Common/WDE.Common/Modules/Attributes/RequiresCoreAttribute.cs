using System;

namespace WDE.Module.Attributes;

public class RequiresCoreAttribute : Attribute
{
    public string[] Tags { get; set; }

    public RequiresCoreAttribute(params string[] tags) 
    {
        Tags = tags;
    }
}