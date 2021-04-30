using System.Collections.Generic;

namespace WDE.Common.Database
{
    public interface IGossipMenu
    {
        uint MenuId { get; }
        IEnumerable<INpcText> Text { get; }
    }
}