using System;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.WoWHeadConnector
{
    [UniqueProvider]
    internal interface IWwwClient
    {
        Task<string> FetchContent(Uri uri);
    }
}