using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ITrinityConditionManager
{
    IReadOnlyList<(int conditionType, int parameterIndex)> GetQuestConditionParameters();
}