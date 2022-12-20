using System.Collections.Generic;

namespace WDE.Conditions.Shared;

public interface IAffectsOtherParametersParameter
{
    public IEnumerable<int> AffectedParameters();
}

public interface IAffectedByOtherParametersParameter
{
    public IEnumerable<int> AffectedByParameters();
}