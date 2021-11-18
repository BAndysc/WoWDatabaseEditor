using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.DBC
{
    [UniqueProvider]
    public interface IDatabaseClientFileOpener
    {
        IEnumerable<IDbcIterator> Open(byte[] data);
        IEnumerable<IDbcIterator> Open(string path);
    }
}