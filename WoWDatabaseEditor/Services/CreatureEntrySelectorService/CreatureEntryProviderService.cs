using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WDE.Common;
using WDE.Common.Database;
using WDE.Common.DBC;
using WoWDatabaseEditor.Extensions;
using Prism.Ioc;

namespace WoWDatabaseEditor.Services.CreatureEntrySelectorService
{
    [WDE.Common.Attributes.AutoRegister]
    public abstract class GenericDatabaseProviderService<T>
    {
        private readonly Func<T, uint> _entryGetter;
        private readonly Func<T, string> _index;

        protected GenericDatabaseProviderService(Func<T, uint> entryGetter, Func<T, string> index)
        {
            _entryGetter = entryGetter;
            _index = index;
        }

        protected abstract IEnumerable<T> GetList();

        public uint? GetEntryFromService()
        {
            CreatureEntrySelectorWindow window = new CreatureEntrySelectorWindow();

            List<ColumnDescriptor> columns = new List<ColumnDescriptor>()
            {
                new ColumnDescriptor { HeaderText = "Entry", DisplayMember = "Entry" },
                new ColumnDescriptor { HeaderText = "Name", DisplayMember = "Name" },
            };
            
            var context = new GenericSelectorWindowViewModel<T>(columns, GetList(), _entryGetter, _index);
            window.DataContext = context;

            if (window.ShowDialog().Value)
                return context.GetEntry();

            return null;
        }
    }

    public class CreatureEntryProviderService :  GenericDatabaseProviderService<ICreatureTemplate>, ICreatureEntryProviderService
    {
        private readonly IDatabaseProvider database;
        public CreatureEntryProviderService(IDatabaseProvider database) : base(t => t.Entry, t=> t.Name + " "+t.Entry)
        {
            this.database = database;
        }

        protected override IEnumerable<ICreatureTemplate> GetList()
        {
            return database.GetCreatureTemplates()
                            .OrderBy(template => template.Entry);
        }
    }

    public class GameobjectEntryProviderService : GenericDatabaseProviderService<IGameObjectTemplate>, IGameobjectEntryProviderService
    {
        private readonly IDatabaseProvider database;
        public GameobjectEntryProviderService(IDatabaseProvider database) : base(t => t.Entry, t => t.Name + " " + t.Entry)
        {
            this.database = database;
        }

        protected override IEnumerable<IGameObjectTemplate> GetList()
        {
            return database.GetGameObjectTemplates()
                            .OrderBy(template => template.Entry);
        }
    }

    public class QuestEntryProviderService : GenericDatabaseProviderService<IQuestTemplate>, IQuestEntryProviderService
    {
        private readonly IDatabaseProvider database;

        public QuestEntryProviderService(IDatabaseProvider database) : base(t => t.Entry, t => t.Name + " " + t.Entry) {
            this.database = database;
        }

        protected override IEnumerable<IQuestTemplate> GetList()
        {
            return database.GetQuestTemplates()
                            .OrderBy(template => template.Entry);
        }
    }

    public class SpellMiniEntry
    {
        public uint Entry { get; set; }
        public string Name { get; set; }
    }

    public class SpellEntryProviderService : GenericDatabaseProviderService<SpellMiniEntry>, ISpellEntryProviderService
    {
        private readonly ISpellStore spellStore;

        public SpellEntryProviderService(ISpellStore spellStore) : base(t => t.Entry, t => t.Name + " " + t.Entry) {
            this.spellStore = spellStore;
        }

        protected override IEnumerable<SpellMiniEntry> GetList()
        {
            List<SpellMiniEntry> spells = new List<SpellMiniEntry>();

            foreach (var spellId in spellStore.Spells)
                spells.Add(new SpellMiniEntry {Entry = spellId, Name= spellStore.GetName(spellId)});

            return spells;
        }
    }
}
