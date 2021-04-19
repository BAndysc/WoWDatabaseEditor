using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using SoapHttpClient;
using WDE.RemoteSOAP.Services;
using WDE.RemoteSOAP.Services.Soap;

namespace WDE.RemoteSOAP.Test.Services
{
    public class TrinitySoapClientTest
    {
        private string tcResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tc=""urn:TC"">
        <soap:Body>
        <tc:executeCommandResponse>
        <result>This is result</result>
        </tc:executeCommandResponse>
        </soap:Body>
        </soap:Envelope>";
        
        [Test]
        public async Task Test()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent(tcResponse)};
            var soapClient = Substitute.For<ISoapClient>();
            var factory = Substitute.For<ISoapClientFactory>();
            soapClient.PostAsync(default, default, default).ReturnsForAnyArgs(Task.FromResult(response));
            factory.Factory(default, default).ReturnsForAnyArgs(soapClient);

            TrinitySoapClient client = new(factory, "127.0.0.1", 1234, "", "");

            var resp = await client.ExecuteCommand("cmd");
            
            Assert.AreEqual(true, resp.Success);
            Assert.AreEqual("This is result", resp.Message);
        }
    }
}