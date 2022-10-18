using System.Collections.Generic;

namespace WDE.SmartScriptEditor.Parameters;

public interface IAffectsOtherParametersParameter
{
    public IEnumerable<int> AffectedParameters();
}