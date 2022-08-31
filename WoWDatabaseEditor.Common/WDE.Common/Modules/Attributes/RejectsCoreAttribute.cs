using System;

namespace WDE.Module.Attributes;

public class RejectsCoreAttribute : Attribute
{
    public string[] Tags { get; set; }

    public RejectsCoreAttribute(params string[] tags) 
    {
        Tags = tags;
    }
}