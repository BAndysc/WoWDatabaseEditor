using System.Collections.Generic;
using WDE.Common.Services.HeadConnector;
using WDE.Module.Attributes;

namespace WDE.WoWHeadConnector
{
    [UniqueProvider]
    internal interface IHeadParser
    {
        IList<Ability> ParseAbilities(string source);
    }
}