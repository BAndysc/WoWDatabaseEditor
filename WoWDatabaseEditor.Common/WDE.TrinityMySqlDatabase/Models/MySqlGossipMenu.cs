using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    public class MySqlGossipMenu : IGossipMenu
    {
        public MySqlGossipMenu(uint menuId, IEnumerable<INpcText>? texts)
        {
            MenuId = menuId;
            Text = texts?.ToList() ?? Enumerable.Empty<INpcText>();
        }

        public uint MenuId { get; }
        public IEnumerable<INpcText> Text { get; }
    }
}