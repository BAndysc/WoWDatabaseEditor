using System;
using WDE.Common.Services.HeadConnector;
using WDE.Module.Attributes;

namespace WDE.WoWHeadConnector
{
    [AutoRegister]
    [SingleInstance]
    internal class UriLocator : IUriLocator
    {
        public Uri Npc(HeadSourceType source, uint entry)
        {
            string prefix = "";
            if (source == HeadSourceType.Classic)
                prefix = "classic.";
            else if (source == HeadSourceType.Tbc)
                prefix = "tbc.";
            return new Uri($"https://{prefix}wowhead.com/npc={entry}/");
        }
    }
}