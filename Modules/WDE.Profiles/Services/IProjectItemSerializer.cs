using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Profiles.Services;

[UniqueProvider]
public interface IProjectItemSerializer
{
    string Serialize(ISmartScriptProjectItem projectItem);
    bool TryDeserializeItem(string str, out ISmartScriptProjectItem item);
}