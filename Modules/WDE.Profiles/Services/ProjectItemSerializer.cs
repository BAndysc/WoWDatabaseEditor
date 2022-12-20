using System;
using Newtonsoft.Json;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Profiles.Services;

[SingleInstance]
[AutoRegister]
public class ProjectItemSerializer : IProjectItemSerializer
{
    public string Serialize(ISmartScriptProjectItem projectItem)
    {
        var abstractItem = new AbstractSmartScriptProjectItem(projectItem);
        var serialized = JsonConvert.SerializeObject(abstractItem);
        return serialized;
    }
    
    public bool TryDeserializeItem(string str, out ISmartScriptProjectItem item)
    {
        item = null!;
        try
        {
            var deserialized = JsonConvert.DeserializeObject<AbstractSmartScriptProjectItem>(str);
            item = deserialized!;
            return deserialized != null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}