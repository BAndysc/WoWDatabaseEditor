using System.Collections.Generic;

namespace WDE.SourceCodeIntegrationEditor.CoreSourceIntegration.SourceParsers
{
    public interface ITrinityStringsSourceParser
    {
        Dictionary<uint, string> ParseTrinityStringsEnum();
    }
}