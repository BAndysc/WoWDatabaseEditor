using System;
using System.Net.Http;
using System.Threading.Tasks;
using SoapHttpClient;
using SoapHttpClient.Enums;
using WDE.Common.Services;
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
            HttpResponseMessage result;
            try
            {
                result = await soapClient.PostAsync(
                    endpoint,
                    SoapVersion.Soap11,
                    soapParser.PrepareBody(command));
            }
            catch (Exception e)
            {
                throw new CouldNotConnectToRemoteServer(e);
            }

            var response = await result.Content.ReadAsStringAsync();
            return soapParser.ParseResponse(response);
        }
    }
}