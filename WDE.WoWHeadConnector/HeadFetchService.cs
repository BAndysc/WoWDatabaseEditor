using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Services.HeadConnector;
using WDE.Module.Attributes;

namespace WDE.WoWHeadConnector
{
    [SingleInstance]
    [AutoRegister]
    internal class HeadFetchService : IHeadFetchService
    {
        private readonly IUriLocator uriLocator;
        private readonly IWwwClient webClient;
        private readonly IHeadParser headParser;

        public HeadFetchService(IUriLocator uriLocator,
            IWwwClient webClient,
            IHeadParser headParser)
        {
            this.uriLocator = uriLocator;
            this.webClient = webClient;
            this.headParser = headParser;
        }
        
        public async Task<IList<Ability>> FetchNpcAbilities(HeadSourceType source, uint entry)
        {
            var uri = uriLocator.Npc(source, entry);
            var content = await webClient.FetchContent(uri);
            return headParser.ParseAbilities(content);
        }
    }
}