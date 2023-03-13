using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.CreatureEntrySelectorService
{
    [AutoRegister]
    public abstract class GenericDatabaseProviderService<T>
    {
        private readonly Func<T, int> entryGetter;
        private readonly Func<T, string> index;
        private readonly IWindowManager windowManager;

        protected GenericDatabaseProviderService(IWindowManager windowManager, Func<T, int> entryGetter, Func<T, string> index)
        {
            this.entryGetter = entryGetter;
            this.index = index;
            this.windowManager = windowManager;
        }

        protected abstract IEnumerable<T> GetList();

        public async Task<int?> GetEntryFromService()
        {
            List<ColumnDescriptor> columns = new()
            {
                ColumnDescriptor.TextColumn("Entry", "Entry", 50),
                ColumnDescriptor.TextColumn("Name", "Name"),
                ColumnDescriptor.TextColumn("Script", "AIName")
            };

            var context = new GenericSelectorDialogViewModel<T>(columns, GetList(), entryGetter, index);

            if (await windowManager.ShowDialog(context))
                return context.GetEntry();

            return null;
        }
    }

    public class GameobjectEntryOrGuidProviderService : GenericDatabaseProviderService<IGameObjectTemplate>, IGameobjectEntryOrGuidProviderService
    {
        private readonly IDatabaseProvider database;

        public GameobjectEntryOrGuidProviderService(IWindowManager windowManager, IDatabaseProvider database) : base(windowManager,t => (int)t.Entry, t => t.Name + " " + t.Entry)
        {
            this.database = database;
        }

        protected override IEnumerable<IGameObjectTemplate> GetList()
        {
            return database.GetGameObjectTemplates().OrderBy(template => template.Entry);
        }
    }
}