using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.EventAiEditor.Editor;
using WDE.EventAiEditor.Editor.UserControls;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Editor
{
    [AutoRegister]
    public class MangosEventAiDatabaseProvider : IEventAiDatabaseProvider
    {
        private readonly IDatabaseProvider databaseProvider;

        public MangosEventAiDatabaseProvider(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }

        public async Task<IEnumerable<IEventAiLine>> GetScriptFor(int entry)
        {
            return await databaseProvider.GetEventAi(entry);
        }

        public async Task<IList<IEventAiLine>> FindEventAiLinesBy(IEnumerable<(EventAiPropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions)
        {
            var result = await databaseProvider.FindEventAiLinesBy(conditions
                .Select(c => (ToDatabasePropertyType(c.what), c.whatValue, c.parameterIndex, c.valueToSearch)));
            return result.ToList();
        }

        private static IDatabaseProvider.EventAiLinePropertyType ToDatabasePropertyType(EventAiPropertyType type)
        {
            switch (type)
            {
                case EventAiPropertyType.Event:
                    return IDatabaseProvider.EventAiLinePropertyType.Event;
                case EventAiPropertyType.Action1:
                    return IDatabaseProvider.EventAiLinePropertyType.Action1;
                case EventAiPropertyType.Action2:
                    return IDatabaseProvider.EventAiLinePropertyType.Action2;
                case EventAiPropertyType.Action3:
                    return IDatabaseProvider.EventAiLinePropertyType.Action3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}