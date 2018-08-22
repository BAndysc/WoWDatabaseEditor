using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBFilesClient.NET;

using Prism.Ioc;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.DbcStore.DbcReader;
using WDE.DbcStore.Models;

namespace WDE.DbcStore
{
    public class DbcStore : IDbcStore, ISpellStore
    {
        private IContainerProvider _container;
        
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
            Load("spell.dbc", 20, SpellStore);
            Load("AreaTable.dbc", 10, AreaStore);
            Load("item-sparse.db2", 98, ItemStore);
            Load("SoundEntries.dbc", 1, SoundStore);
            Load("movie.dbc", 0, MovieStore);
            Load("chrClasses.dbc", 2, ClassStore);
            Load("chrRaces.dbc", 10, RaceStore);
            Load("achievement.dbc", 3, AchievementStore);
            Load("Phase.dbc", 0, PhaseStore);
            Load("Emotes.dbc", 0, EmoteStore);
            Load("SkillLine.dbc", 1, SkillStore);
            Load("Languages.dbc", 0, LanguageStore);

            parameterFactory.Register("SpellParameter", (name) => new DbcParameter(name, SpellStore));
            parameterFactory.Register("EmoteParameter", (name) => new DbcParameter(name, EmoteStore));
            parameterFactory.Register("SoundParameter", (name) => new DbcParameter(name, SoundStore));
            parameterFactory.Register("MovieParameter", (name) => new DbcParameter(name, MovieStore));
            parameterFactory.Register("ZoneParameter", (name) => new DbcParameter(name, AreaStore));
            parameterFactory.Register("PhaseParameter", (name) => new DbcParameter(name, PhaseStore));
        }
        
        private void Load(string filename, int fields_to_skip, Dictionary<int, string> dictionary)
        {
            IWowClientDBReader mReader;
            if (filename.Contains(".dbc"))
                mReader = new DBCReader("dbc/" + filename);
            else
                mReader = new DB2Reader("dbc/" + filename);

            for (int i = 0; i < mReader.RecordsCount; ++i)
            {
                BinaryReader br = mReader[i];
                int id = br.ReadInt32();
                for (int j = 0; j < fields_to_skip; ++j)
                    br.ReadInt32();
                string name = mReader.StringTable[br.ReadInt32()] ?? filename.Replace(".dbc", "") + " " + id;
                dictionary[id] = name;
            }
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
