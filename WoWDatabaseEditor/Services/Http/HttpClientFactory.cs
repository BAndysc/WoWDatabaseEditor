using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using WDE.Common.Factories;
using WDE.Common.Factories.Http;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Http
{
    [AutoRegister]
    [SingleInstance]
    public class HttpClientFactory : IHttpClientFactory
    {
        private readonly IApplicationVersion applicationVersion;
        private readonly IEnumerable<IUserAgentPart> parts;

        public HttpClientFactory(IApplicationVersion applicationVersion, IEnumerable<IUserAgentPart> parts)
        {
            this.applicationVersion = applicationVersion;
            this.parts = parts;
        }

        public HttpClient Factory()
        {
            var httpClient = new HttpClient();
            var buildVersion = applicationVersion.VersionKnown ? applicationVersion.BuildVersion : -1;
            var branch = applicationVersion.VersionKnown ? applicationVersion.Branch! : "(unknown)";
            var other = string.Join(", ", parts.Select(p => p.Part));
            httpClient.DefaultRequestHeaders.Add("User-Agent", $"WoWDatabaseEditor/{buildVersion} (branch: {branch}, {other})");
            return httpClient;
        }
    }

}