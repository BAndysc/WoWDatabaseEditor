using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WoWDatabaseEditor.Extensions;

namespace WoWDatabaseEditor.Services.CreatureEntrySelectorService
{
    [AutoRegister]
    public abstract class GenericDatabaseProviderService<T>
    {
        private readonly Func<T, uint> entryGetter;
        private readonly Func<T, string> index;
        private readonly IWindowManager windowManager;

        protected GenericDatabaseProviderService(IWindowManager windowManager, Func<T, uint> entryGetter, Func<T, string> index)
        {
            this.entryGetter = entryGetter;
            this.index = index;
            this.windowManager = windowManager;
        }

        protected abstract IEnumerable<T> GetList();

        public uint? GetEntryFromService()
        {
            List<ColumnDescriptor> columns = new()
            {
                new ColumnDescriptor("Entry", "Entry", 50),
                new ColumnDescriptor("Name", "Name"),
                new ColumnDescriptor("Script", "AIName")
            };

            var context = new GenericSelectorDialogViewModel<T>(columns, GetList(), entryGetter, index);

            if (windowManager.ShowDialog(context))
                return context.GetEntry();

            return null;
        }
    }

    public class CreatureEntryProviderService : GenericDatabaseProviderService<ICreatureTemplate>, ICreatureEntryProviderService
    {
        private readonly IDatabaseProvider database;

        public CreatureEntryProviderService(IWindowManager windowManager, IDatabaseProvider database) : base(windowManager, t => t.Entry, t => t.Name + " " + t.Entry)
        {
            this.database = database;
        }

        protected override IEnumerable<ICreatureTemplate> GetList()
        {
            return database.GetCreatureTemplates().OrderBy(template => template.Entry);
        }
    }

    public class GameobjectEntryProviderService : GenericDatabaseProviderService<IGameObjectTemplate>, IGameobjectEntryProviderService
    {
        private readonly IDatabaseProvider database;

        public GameobjectEntryProviderService(IWindowManager windowManager, IDatabaseProvider database) : base(windowManager,t => t.Entry, t => t.Name + " " + t.Entry)
        {
            this.database = database;
        }

        protected override IEnumerable<IGameObjectTemplate> GetList()
        {
            return database.GetGameObjectTemplates().OrderBy(template => template.Entry);
        }
    }

    public class QuestEntryProviderService : GenericDatabaseProviderService<IQuestTemplate>, IQuestEntryProviderService
    {
        private readonly IDatabaseProvider database;

        public QuestEntryProviderService(IWindowManager windowManager, IDatabaseProvider database) : base(windowManager, t => t.Entry, t => t.Name + " " + t.Entry)
        {
            this.database = database;
        }

        protected override IEnumerable<IQuestTemplate> GetList()
        {
            return database.GetQuestTemplates().OrderBy(template => template.Entry);
        }
    }

    public class SpellMiniEntry
    {
        public SpellMiniEntry(uint entry, string name)
        {
            Entry = entry;
            Name = name;
        }

        public uint Entry { get; set; }
        public string Name { get; set; }
    }

    public class SpellEntryProviderService : GenericDatabaseProviderService<SpellMiniEntry>, ISpellEntryProviderService
    {
        private readonly ISpellStore spellStore;

        public SpellEntryProviderService(IWindowManager windowManager, ISpellStore spellStore) : base(windowManager, t => t.Entry, t => t.Name + " " + t.Entry)
        {
            this.spellStore = spellStore;
        }

        protected override IEnumerable<SpellMiniEntry> GetList()
        {
            List<SpellMiniEntry> spells = new();

            foreach (uint spellId in spellStore.Spells)
                spells.Add(new SpellMiniEntry(spellId, spellStore.GetName(spellId)));

            return spells;
        }
    }
}