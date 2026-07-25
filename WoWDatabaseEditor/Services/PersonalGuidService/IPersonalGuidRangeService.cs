using System;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.PersonalGuidService;

/// <summary>
/// The personal guid range store. Deliberately internal to the core project: everything else
/// must allocate through <see cref="WDE.Common.Services.IdGenerator.IIdGeneratorService"/>,
/// which exposes this store as the "Personal guid range" source.
/// </summary>
[UniqueProvider]
public interface IPersonalGuidRangeService
{
    bool IsConfigured { get; }
    uint GetNextGuid(GuidType type);
    uint GetNextGuidRange(GuidType type, uint count);
}

public class GuidServiceNotSetupException : Exception
{
    public GuidServiceNotSetupException() : base("GuidService is not configured")
    {
    }
}

public class NoMoreGuidsException : Exception
{
    public NoMoreGuidsException(GuidType type) : base("There is no more guid for type " + type)
    {
    }
}
