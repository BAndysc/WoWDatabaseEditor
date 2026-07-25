using System.Xml;
using System.Xml.Linq;

namespace WDE.RemoteSOAP.Services.Soap
{
    public class TrinitySoapParser
    {
        private static XNamespace soapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
        private readonly XNamespace commandNamespace;

        // commandNamespace is the XML namespace of the executeCommand method, which varies per core
        // (TrinityCore/AzerothCore use "urn:TC", CMaNGOS uses "urn:MaNGOS").
        public TrinitySoapParser(string commandNamespace = "urn:TC")
        {
            this.commandNamespace = commandNamespace;
        }

        public XElement PrepareBody(string body)
        {
            return new XElement(commandNamespace + "executeCommand", new XElement("command", body));
        }

        public SoapResponse ParseResponse(string response)
        {
            XmlDocument xmlDoc= new();
            xmlDoc.LoadXml(response);

            XmlNamespaceManager namespaceManager = new(xmlDoc.NameTable);
            namespaceManager.AddNamespace("soap", soapNamespace.NamespaceName);
            namespaceManager.AddNamespace("tc", commandNamespace.NamespaceName);

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