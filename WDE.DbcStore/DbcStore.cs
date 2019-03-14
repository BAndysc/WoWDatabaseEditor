using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Module.Attributes;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.DbcStore.Models;
using WDE.DbcStore.Providers;

namespace WDE.DbcStore
{
    [AutoRegister, SingleInstance]
    public class DbcStore : IDbcStore, ISpellStore
    {
        private readonly IParameterFactory parameterFactory;
        private readonly IDbcSettingsProvider dbcSettingsProvider;

        private Dictionary<int, string> SpellStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> SkillStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> LanguageStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> PhaseStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> AreaStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> MapStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> SoundStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> MovieStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> ClassStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> RaceStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> EmoteStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> AchievementStore { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> ItemStore { get; } = new Dictionary<int, string>();

        public DbcStore(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
            this.dbcSettingsProvider = new DbcSettingsProvider();

            Load();
        }

        internal void Load()
        {
            if (dbcSettingsProvider.GetSettings().SkipLoading)
                return;

            WDBXEditor.Storage.Database.LoadDefinitions();

            LoadLegion("spell.db2", 0, 1, SpellStore);
            LoadLegion("achievement.db2", 12, 1, AchievementStore);
            LoadLegion("AreaTable.db2", 0, 2, AreaStore);
            LoadLegion("chrClasses.db2", 19, 1, ClassStore);
            LoadLegion("chrRaces.db2", 34, 2, RaceStore);
            LoadLegion("Emotes.db2", 0, 2, EmoteStore);
            LoadLegion("ItemSparse.db2", 0, 2, ItemStore);
            LoadLegion("Languages.db2", 1, 0, LanguageStore);
            // LoadLegion("Phase.db2", 1, 0, PhaseStore); // no names in legion :(
            LoadLegion("SoundKitName.db2", 0, 1, SoundStore);

            parameterFactory.Register("SpellParameter", (name) => new DbcParameter(name, SpellStore));
            parameterFactory.Register("EmoteParameter", (name) => new DbcParameter(name, EmoteStore));
            parameterFactory.Register("SoundParameter", (name) => new DbcParameter(name, SoundStore));
            parameterFactory.Register("ZoneParameter", (name) => new DbcParameter(name, AreaStore));
            parameterFactory.Register("PhaseParameter", (name) => new DbcParameter(name, PhaseStore));
        }
        
        private void LoadLegion(string filename, int id, int index, Dictionary<int, string> dictionary)
        {
            WDBXEditor.Reader.DBReader r = new WDBXEditor.Reader.DBReader();
            var path = $"{dbcSettingsProvider.GetSettings().Path}/{filename}";

            if (!File.Exists(path))
                return;

            var dbEntry = r.Read(path);

            foreach (DataRow row in dbEntry.Data.Rows)
                dictionary.Add(Convert.ToInt32(row.ItemArray[id].ToString()), row.ItemArray[index].ToString());
        }
        
        public IEnumerable<uint> Spells
        {
            get
            {
                foreach (var key in SpellStore.Keys)
                {
                    // @TODO: get rid of this ugly cast when redesign loading dbc
                    yield return (uint)key;
                }
            }
        }

        public bool HasSpell(uint entry)
        {
            // @TODO: get rid of this ugly cast when redesign loading dbc
            return SpellStore.ContainsKey((int)entry);
        }

        public string GetName(uint entry)
        {
            // @TODO: get rid of this ugly cast when redesign loading dbc
            return SpellStore[(int)entry];
        }
    }

    public class DbcParameter : Parameter
    {
        private readonly Dictionary<int, string> _storage;

        public DbcParameter(string name, Dictionary<int, string> storage) : base(name)
        {
            _storage = storage;
            Items = new Dictionary<int, SelectOption>();
            foreach (var key in storage.Keys)
                Items.Add(key, new SelectOption(storage[key]));
        }

        public override string ToString()
        {
            if (!_storage.ContainsKey(GetValue()))
                return GetValue().ToString();
            return _storage[GetValue()];
        }
    }
}
