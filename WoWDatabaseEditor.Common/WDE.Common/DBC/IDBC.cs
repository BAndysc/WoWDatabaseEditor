using System.Collections.Generic;

namespace WDE.Common.DBC;

public interface IDBC : IEnumerable<IDbcIterator>
{
    uint RecordCount { get; }
}