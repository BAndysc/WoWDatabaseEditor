using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.DBC
{
    [UniqueProvider]
    public interface IDatabaseClientFileOpener
    {
        IDBC Open(byte[] data);
        IDBC Open(string path);
    }
}