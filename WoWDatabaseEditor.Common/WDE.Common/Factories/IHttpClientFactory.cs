using System.Net.Http;
using WDE.Module.Attributes;

namespace WDE.Common.Factories
{
    [UniqueProvider]
    public interface IHttpClientFactory
    {
        HttpClient Factory();
    }
}