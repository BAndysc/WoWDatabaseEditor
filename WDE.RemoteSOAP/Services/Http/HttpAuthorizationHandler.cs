using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.RemoteSOAP.Services.Http
{
    public class HttpAuthorizationHandler : DelegatingHandler
    {
        private readonly AuthenticationHeaderValue authorizationHeader;

        public HttpAuthorizationHandler(AuthenticationHeaderValue authorizationHeader)
        {
            this.authorizationHeader = authorizationHeader;
            InnerHandler = new HttpClientHandler();
        }
            
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = authorizationHeader;
            return await base.SendAsync(request, cancellationToken);
        }
    }
}