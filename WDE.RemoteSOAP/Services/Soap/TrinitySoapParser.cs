using System.Xml;
using System.Xml.Linq;

namespace WDE.RemoteSOAP.Services.Soap
{
    public class TrinitySoapParser
    {
        private static XNamespace soapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
        private static XNamespace trinityNamespace = "urn:TC";

        public XElement PrepareBody(string body)
        {
            return new XElement(trinityNamespace + "executeCommand", new XElement("command", body));
        }

        public SoapResponse ParseResponse(string response)
        {   
            XmlDocument xmlDoc= new();
            xmlDoc.LoadXml(response);
            
            XmlNamespaceManager namespaceManager = new(xmlDoc.NameTable);
            namespaceManager.AddNamespace("soap", soapNamespace.NamespaceName);
            namespaceManager.AddNamespace("tc", trinityNamespace.NamespaceName);

            var succ = xmlDoc.SelectSingleNode("//soap:Envelope/soap:Body/tc:executeCommandResponse/result", namespaceManager);
            var fail = xmlDoc.SelectSingleNode("//soap:Envelope/soap:Body/soap:Fault/detail", namespaceManager);
            var faultstring = xmlDoc.SelectSingleNode("//soap:Envelope/soap:Body/soap:Fault/faultstring", namespaceManager);
            
            if (succ != null)
                return new SoapResponse(true, succ.InnerText);

            var error = (faultstring?.InnerText ?? "") + "\n" + (fail?.InnerText ?? "");
            return new SoapResponse(false, error);
        }
    }
}