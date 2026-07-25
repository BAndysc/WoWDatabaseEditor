using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;

namespace WDE.WorldStateExpressions.Parameters
{
    /// numbered parameter with items loaded from the cmangos worldstate_name table
    /// (names for server-side variable worldstates); empty on cores without that table
    internal class WorldStateNameParameter : ParameterNumbered
    {
        public async Task Load(IMySqlExecutor executor)
        {
            if (!executor.IsConnected)
                return;

            try
            {
                var result = await executor.ExecuteSelectSql("SELECT `Id`, `Name` FROM `worldstate_name`");
                var items = new Dictionary<long, SelectOption>();
                var idColumn = result.ColumnIndex("Id");
                var nameColumn = result.ColumnIndex("Name");
                foreach (var row in result)
                {
                    var name = result.Value<string>(row, nameColumn);
                    if (!string.IsNullOrEmpty(name))
                        items[Convert.ToInt64(result.Value(row, idColumn)!)] = new SelectOption(name);
                }
                Items = items;
            }
            catch (IMySqlExecutor.DatabaseExecutorException)
            {
                // the table only exists on cmangos cores
            }
        }

        public string? NameOf(long worldStateId)
        {
            if (Items != null && Items.TryGetValue(worldStateId, out var option))
                return option.Name;
            return null;
        }
    }
}
