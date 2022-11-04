using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.Settings;

[NonUniqueProvider]
public interface IGeneralSettingsGroup
{
    string Name { get; }
    IReadOnlyList<IGenericSetting> Settings { get; }
    void Save();
}