using System;

namespace WDE.Common.Services.IdGenerator;

/// <summary>
/// An id allocation request. The concrete type selects WHICH kind of id is allocated
/// (creature guid, condition entry, ...), the instance carries optional per-request data
/// some sources may use (e.g. the creature entry). Modules define their own implementations.
/// </summary>
public interface IIdType
{
}

/// <summary>Human readable name of an id kind, shown in settings and error messages.</summary>
[AttributeUsage(AttributeTargets.Class)]
public class IdTypeNameAttribute : Attribute
{
    public string Name { get; }

    public IdTypeNameAttribute(string name) => Name = name;
}

public static class IdTypeNames
{
    public static string Of(Type idType)
    {
        var attribute = (IdTypeNameAttribute?)Attribute.GetCustomAttribute(idType, typeof(IdTypeNameAttribute));
        return attribute?.Name ?? idType.Name;
    }
}
