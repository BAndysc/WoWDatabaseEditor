using System;

namespace WDE.PacketViewer.PacketParserIntegration
{
    public class ParserException : Exception
    {
        public ParserException(string message) : base(message)
        {
        }
    }
}