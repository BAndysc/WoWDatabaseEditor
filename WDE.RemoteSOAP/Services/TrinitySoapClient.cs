using System;
using System.Threading.Tasks;
using SoapHttpClient;
using SoapHttpClient.Enums;
using WDE.RemoteSOAP.Services.Soap;

namespace WDE.RemoteSOAP.Services
{
    public class TrinitySoapClient
    {
        private readonly Uri endpoint;
        private readonly ISoapClient soapClient;
        private readonly TrinitySoapParser soapParser;
        
        public TrinitySoapClient(ISoapClientFactory soapClientFactory,
            string host, 
            int port, 
            string userName, 
            string password)
        {
            endpoint = new Uri($"http://{host}:{port}");
            soapClient = soapClientFactory.Factory(userName, password);
            soapParser = new TrinitySoapParser();
        }
        
        public async Task<SoapResponse> ExecuteCommand(string command)
        {
            var result =
                await soapClient.PostAsync(
                    endpoint,
                    SoapVersion.Soap11,
                    soapParser.PrepareBody(command));
            
            var response = await result.Content.ReadAsStringAsync();
            return soapParser.ParseResponse(response);
        }
    }
}