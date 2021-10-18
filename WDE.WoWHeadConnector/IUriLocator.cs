using System;
using WDE.Common.Services.HeadConnector;
using WDE.Module.Attributes;

namespace WDE.WoWHeadConnector
{
    [UniqueProvider]
    internal interface IUriLocator
    {
        public Uri Npc(HeadSourceType source, uint entry);
    }
}