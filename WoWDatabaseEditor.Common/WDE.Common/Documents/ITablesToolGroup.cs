using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.Common.Documents;

[NonUniqueProvider]
public interface ITablesToolGroup
{
    ImageUri Icon { get; }
    string GroupName { get; }
    RgbColor? CustomColor { get; }
    int Priority { get; }
    void ToolOpened();
    void ToolClosed();
}

[NonUniqueProvider]
public interface ITablesToolGroupsProvider
{
    IEnumerable<ITablesToolGroup> GetProviders();
}