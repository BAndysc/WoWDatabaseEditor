using SoapHttpClient;
using WDE.Module.Attributes;
using WDE.RemoteSOAP.Helpers;
using WDE.RemoteSOAP.Services.Http;

namespace WDE.RemoteSOAP.Services.Soap
{
    [AutoRegister]
    public class SoapClientFactory : ISoapClientFactory
    {
        public ISoapClient Factory(string userName, string password)
        {
            var auth = HttpBasicAuthorizer.Encode(userName, password);
            return new SoapClient(new HandlerWrappedHttpClientFactory(new HttpAuthorizationHandler(auth)));
        }
    }
}