using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.DBC;
using WoWDatabaseEditor.Extensions;

namespace WoWDatabaseEditor.Services.CreatureEntrySelectorService
{
    public abstract class GenericDatabaseProviderService<T>
    {
        protected readonly IUnityContainer _unity;
        private readonly Func<T, uint> _entryGetter;
        private readonly Func<T, string> _index;

        protected GenericDatabaseProviderService(IUnityContainer unity, Func<T, uint> entryGetter, Func<T, string> index)
        {
            _unity = unity;
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
            
            var context = new GenericSelectorWindowViewModel<T>(_unity, columns, GetList(), _entryGetter, _index);
            window.DataContext = context;

            if (window.ShowDialog().Value)
                return context.GetEntry();

            return null;
        }
    }

    public class CreatureEntryProviderService :  GenericDatabaseProviderService<ICreatureTemplate>, ICreatureEntryProviderService
    {
        public CreatureEntryProviderService(IUnityContainer unity) : base(unity, t => t.Entry, t=> t.Name + " "+t.Entry) {  }

        protected override IEnumerable<ICreatureTemplate> GetList()
        {
            return _unity.Resolve<IDatabaseProvider>()
                            .GetCreatureTemplates()
                            .OrderBy(template => template.Entry);
        }
    }

    public class GameobjectEntryProviderService : GenericDatabaseProviderService<IGameObjectTemplate>, IGameobjectEntryProviderService
    {
        public GameobjectEntryProviderService(IUnityContainer unity) : base(unity, t => t.Entry, t => t.Name + " " + t.Entry) { }

        protected override IEnumerable<IGameObjectTemplate> GetList()
        {
            return _unity.Resolve<IDatabaseProvider>()
                            .GetGameObjectTemplates()
                            .OrderBy(template => template.Entry);
        }
    }

    public class QuestEntryProviderService : GenericDatabaseProviderService<IQuestTemplate>, IQuestEntryProviderService
    {
        public QuestEntryProviderService(IUnityContainer unity) : base(unity, t => t.Entry, t => t.Name + " " + t.Entry) { }

        protected override IEnumerable<IQuestTemplate> GetList()
        {
            return _unity.Resolve<IDatabaseProvider>()
                            .GetQuestTemplates()
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
        public SpellEntryProviderService(IUnityContainer unity) : base(unity, t => t.Entry, t => t.Name + " " + t.Entry) { }

        protected override IEnumerable<SpellMiniEntry> GetList()
        {
            var dict = _unity.Resolve<IDbcStore>().SpellStore;
            List<SpellMiniEntry> spells = new List<SpellMiniEntry>(dict.Count);

            foreach (var key in dict.Keys)
                spells.Add(new SpellMiniEntry {Entry = (uint)key, Name=dict[key]});

            return spells;
        }
    }
}
