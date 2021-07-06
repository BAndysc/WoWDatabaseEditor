using System.Collections.Generic;

namespace WDE.SourceCodeIntegrationEditor.CoreSourceIntegration.SourceParsers
{
    public interface IRbacSourceParser
    {
        bool CoreSupportsRbac();
        Dictionary<uint, string> ParseRbacEnum();
    }
}