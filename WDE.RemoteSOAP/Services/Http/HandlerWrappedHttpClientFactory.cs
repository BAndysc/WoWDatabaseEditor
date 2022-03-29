using System;
using System.Net.Http;

namespace WDE.RemoteSOAP.Services.Http
{
    public class HandlerWrappedHttpClientFactory : IHttpClientFactory
    {
        public DelegatingHandler Handler { get; }

        public HandlerWrappedHttpClientFactory(DelegatingHandler handler)
        {
            Handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new (Handler) {Timeout = TimeSpan.FromSeconds(3.5f)};
        }
    }
}