using SoapHttpClient;

namespace WDE.RemoteSOAP.Services.Soap
{
    public interface ISoapClientFactory
    {
        ISoapClient Factory(string username, string password);
    }
}