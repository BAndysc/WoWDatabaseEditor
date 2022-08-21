using System;
using System.Collections.Generic;
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

        public Task<IList<IEventAiLine>> FindEventAiLinesBy(IEnumerable<(EventAiPropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions)
        {
            throw new NotImplementedException();
            //return databaseProvider.FindEventAiLinesBy(conditions);
        }
    }
}