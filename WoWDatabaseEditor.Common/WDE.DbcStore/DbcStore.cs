using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using WDBXEditor.Reader;
using WDBXEditor.Storage;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.DbcStore.Providers;
using WDE.Module.Attributes;

namespace WDE.DbcStore
{
    public enum DBCVersions
    {
        WOTLK_12340 = 12340,
        CATA_15595 = 15595,
        LEGION_26972 = 26972
    }

    [AutoRegister]
    [SingleInstance]
    public class DbcStore : IDbcStore, ISpellStore
    {
        private readonly IDbcSettingsProvider dbcSettingsProvider;
        private readonly IParameterFactory parameterFactory;

        public DbcStore(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
            dbcSettingsProvider = new DbcSettingsProvider();

            Load();
        }

        private Dictionary<int, string> SpellStore { get; } = new();
        public Dictionary<int, string> SkillStore { get; } = new();
        public Dictionary<int, string> LanguageStore { get; } = new();
        public Dictionary<int, string> PhaseStore { get; } = new();
        public Dictionary<int, string> AreaStore { get; } = new();
        public Dictionary<int, string> MapStore { get; } = new();
        public Dictionary<int, string> SoundStore { get; } = new();
        public Dictionary<int, string> MovieStore { get; } = new();
        public Dictionary<int, string> ClassStore { get; } = new();
        public Dictionary<int, string> RaceStore { get; } = new();
        public Dictionary<int, string> EmoteStore { get; } = new();
        public Dictionary<int, string> AchievementStore { get; } = new();
        public Dictionary<int, string> ItemStore { get; } = new();

        public IEnumerable<uint> Spells
        {
            get
            {
                foreach (int key in SpellStore.Keys)
                    // @TODO: get rid of this ugly cast when redesign loading dbc
                    yield return (uint) key;
            }
        }

        public bool HasSpell(uint entry)
        {
            // @TODO: get rid of this ugly cast when redesign loading dbc
            return SpellStore.ContainsKey((int) entry);
        }

        public string GetName(uint entry)
        {
            // @TODO: get rid of this ugly cast when redesign loading dbc
            return SpellStore[(int) entry];
        }

        internal void Load()
        {
            if (dbcSettingsProvider.GetSettings().SkipLoading)
                return;

            if (!Directory.Exists(dbcSettingsProvider.GetSettings().Path))
                return;

            Database.LoadDefinitions();
            Database.BuildNumber = (int) dbcSettingsProvider.GetSettings().DBCVersion;

            switch (dbcSettingsProvider.GetSettings().DBCVersion)
            {
                case DBCVersions.WOTLK_12340:
                {
                    Load("Spell.dbc", 0, 136, SpellStore);
                    Load("Achievement.dbc", 0, 4, AchievementStore);
                    Load("AreaTable.dbc", 0, 11, AreaStore);
                    Load("chrClasses.dbc", 0, 4, ClassStore);
                    Load("chrRaces.dbc", 0, 14, RaceStore);
                    Load("Emotes.dbc", 0, 1, EmoteStore);
                    Load("SoundEntries.dbc", 0, 2, SoundStore);
                    break;
                }
                case DBCVersions.CATA_15595:
                {
                    Load("Spell.dbc", 0, 21, SpellStore);
                    Load("Achievement.dbc", 0, 4, AchievementStore);
                    Load("AreaTable.dbc", 0, 11, AreaStore);
                    Load("chrClasses.dbc", 0, 3, ClassStore);
                    Load("chrRaces.dbc", 0, 14, RaceStore);
                    Load("Emotes.dbc", 0, 1, EmoteStore);
                    Load("item-sparse.db2", 0, 99, ItemStore);
                    Load("Phase.dbc", 0, 1, PhaseStore);
                    Load("SoundEntries.dbc", 0, 2, SoundStore);
                    break;
                }
                case DBCVersions.LEGION_26972:
                {
                    Load("spell.db2", 0, 1, SpellStore);
                    Load("achievement.db2", 12, 1, AchievementStore);
                    Load("AreaTable.db2", 0, 2, AreaStore);
                    Load("chrClasses.db2", 19, 1, ClassStore);
                    Load("chrRaces.db2", 34, 2, RaceStore);
                    Load("Emotes.db2", 0, 2, EmoteStore);
                    Load("ItemSparse.db2", 0, 2, ItemStore);
                    Load("Languages.db2", 1, 0, LanguageStore);
                    // Load("Phase.db2", 1, 0, PhaseStore); // no names in legion :(
                    Load("SoundKitName.db2", 0, 1, SoundStore);
                    break;
                }
                default:
                    return;
            }

            parameterFactory.Register("SpellParameter", name => new DbcParameter(name, SpellStore));
            parameterFactory.Register("ItemParameter", name => new DbcParameter(name, ItemStore));
            parameterFactory.Register("EmoteParameter", name => new DbcParameter(name, EmoteStore));
            parameterFactory.Register("SoundParameter", name => new DbcParameter(name, SoundStore));
            parameterFactory.Register("ZoneParameter", name => new DbcParameter(name, AreaStore));
            parameterFactory.Register("PhaseParameter", name => new DbcParameter(name, PhaseStore));
        }

        private void Load(string filename, int id, int nameIndex, Dictionary<int, string> dictionary)
        {
            DBReader r = new();
            var path = $"{dbcSettingsProvider.GetSettings().Path}/{filename}";

            if (!File.Exists(path))
                return;

            DBEntry dbEntry = r.Read(path);

            foreach (DataRow row in dbEntry.Data.Rows)
                dictionary.Add(Convert.ToInt32(row.ItemArray[id].ToString()), row.ItemArray[nameIndex].ToString());
        }
    }

    public class DbcParameter : Parameter
    {
        private readonly Dictionary<int, string> storage;

        public DbcParameter(string name, Dictionary<int, string> storage) : base(name)
        {
            this.storage = storage;
            Items = new Dictionary<int, SelectOption>();
            foreach (int key in storage.Keys)
                Items.Add(key, new SelectOption(storage[key]));
        }

        public override string ToString()
        {
            if (!storage.ContainsKey(GetValue()))
                return GetValue().ToString();
            return storage[GetValue()];
        }

        public override Parameter Clone()
        {
            return new DbcParameter(Name, storage) {Value = value};
        }
    }
}