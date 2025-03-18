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
            string suffix = "";
            if (source == HeadSourceType.Classic)
                suffix = "classic/";
            else if (source == HeadSourceType.Tbc)
                suffix = "tbc/";
            return new Uri($"https://wowhead.com/{suffix}npc={entry}/");
        }
    }
}