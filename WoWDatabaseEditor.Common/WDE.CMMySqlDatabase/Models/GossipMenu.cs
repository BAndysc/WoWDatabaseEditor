using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    public class GossipMenuWoTLK : IGossipMenu
    {
        public GossipMenuWoTLK(uint menuId, IEnumerable<INpcText>? texts)
        {
            MenuId = menuId;
            Text = texts?.ToList() ?? Enumerable.Empty<INpcText>();
        }

        public uint MenuId { get; }
        public IEnumerable<INpcText> Text { get; }
    }
}