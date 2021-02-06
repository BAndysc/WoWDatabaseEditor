﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using WDBXEditor.Reader;
using WDBXEditor.Storage;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
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
        private readonly ITaskRunner taskRunner;

        public DbcStore(IParameterFactory parameterFactory, ITaskRunner taskRunner)
        {
            this.parameterFactory = parameterFactory;
            this.taskRunner = taskRunner;
            dbcSettingsProvider = new DbcSettingsProvider();

            Load();
        }
        
        public Dictionary<int, string> AreaTriggerStore { get; internal set; } = new();
        public Dictionary<int, string> FactionStore { get; internal set; } = new();
        public Dictionary<int, string> SpellStore { get; internal set; } = new();
        public Dictionary<int, string> SkillStore { get; internal set;} = new();
        public Dictionary<int, string> LanguageStore { get; internal set;} = new();
        public Dictionary<int, string> PhaseStore { get; internal set;} = new();
        public Dictionary<int, string> AreaStore { get; internal set;} = new();
        public Dictionary<int, string> MapStore { get; internal set;} = new();
        public Dictionary<int, string> SoundStore { get;internal set; } = new();
        public Dictionary<int, string> MovieStore { get; internal set;} = new();
        public Dictionary<int, string> ClassStore { get; internal set;} = new();
        public Dictionary<int, string> RaceStore { get; internal set;} = new();
        public Dictionary<int, string> EmoteStore { get;internal set; } = new();
        public Dictionary<int, string> AchievementStore { get; internal set;} = new();
        public Dictionary<int, string> ItemStore { get; internal set;} = new();

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

            taskRunner.ScheduleTask(new DbcLoadTask(parameterFactory, this));
        }

        private class DbcLoadTask : IThreadedTask
        {
            private readonly IDbcSettingsProvider dbcSettingsProvider;
            private readonly IParameterFactory parameterFactory;
            private readonly DbcStore store;

            private Dictionary<int, string> AreaTriggerStore { get; } = new();
            private Dictionary<int, string> FactionStore { get; } = new();
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

            public string Name => "DBC Loading";
            public bool WaitForOtherTasks => false;
            
            public DbcLoadTask(IParameterFactory parameterFactory, DbcStore store)
            {
                this.parameterFactory = parameterFactory;
                this.store = store;
                dbcSettingsProvider = new DbcSettingsProvider();
            }
            
            private void Load(string filename, int id, int nameIndex, Dictionary<int, string> dictionary)
            {
                progress.Report(now++, max, $"Loading {filename}");
                DBReader r = new();
                var path = $"{dbcSettingsProvider.GetSettings().Path}/{filename}";

                if (!File.Exists(path))
                    return;

                DBEntry dbEntry = r.Read(path);

                foreach (DataRow row in dbEntry.Data.Rows)
                    dictionary.Add(Convert.ToInt32(row.ItemArray[id].ToString()), row.ItemArray[nameIndex].ToString());
            }
            
            public void FinishMainThread()
            {
                store.AreaTriggerStore = AreaTriggerStore;
                store.FactionStore = FactionStore;
                store.SpellStore = SpellStore;
                store.SkillStore = SkillStore;
                store.LanguageStore = LanguageStore;
                store.PhaseStore = PhaseStore;
                store.AreaStore = AreaStore;
                store.MapStore = MapStore;
                store.SoundStore = SoundStore;
                store.MovieStore = MovieStore;
                store.ClassStore = ClassStore;
                store.RaceStore = RaceStore;
                store.EmoteStore = EmoteStore;
                store.AchievementStore = AchievementStore;
                store.ItemStore = ItemStore;
                
                parameterFactory.Register("MovieParameter", new DbcParameter(MovieStore));
                parameterFactory.Register("FactionParameter", new DbcParameter(FactionStore));
                parameterFactory.Register("SpellParameter", new DbcParameter(SpellStore));
                parameterFactory.Register("ItemParameter", new DbcParameter(ItemStore));
                parameterFactory.Register("EmoteParameter", new DbcParameter(EmoteStore));
                parameterFactory.Register("ClassParameter", new DbcParameter(ClassStore));
                parameterFactory.Register("RaceParameter", new DbcParameter(RaceStore));
                parameterFactory.Register("SkillParameter", new DbcParameter(SkillStore));
                parameterFactory.Register("SoundParameter", new DbcParameter(SoundStore));
                parameterFactory.Register("ZoneParameter", new DbcParameter(AreaStore));
                parameterFactory.Register("MapParameter", new DbcParameter(MapStore));
                parameterFactory.Register("PhaseParameter", new DbcParameter(PhaseStore));
            }

            private int max = 0;
            private int now = 0;
            private ITaskProgress progress;
            public void Run(ITaskProgress progress)
            {
                this.progress = progress;
                Database.LoadDefinitions();
                Database.BuildNumber = (int) dbcSettingsProvider.GetSettings().DBCVersion;

                switch (dbcSettingsProvider.GetSettings().DBCVersion)
                {
                    case DBCVersions.WOTLK_12340:
                    {
                        max = 12;
                        Load("AreaTrigger.dbc", 0, 0, AreaTriggerStore);
                        Load("SkillLine.dbc", 0, 3, SkillStore);
                        Load("Faction.dbc", 0, 23, FactionStore);
                        Load("Spell.dbc", 0, 134, SpellStore);
                        Load("Movie.dbc", 0, 1, MovieStore);
                        Load("Map.dbc", 0, 5, MapStore);
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
                        max = 14;
                        Load("AreaTrigger.dbc", 0, 0,  AreaTriggerStore);
                        Load("SkillLine.dbc", 0, 2, SkillStore);
                        Load("Faction.dbc", 0, 23, FactionStore);
                        Load("Spell.dbc", 0, 21, SpellStore);
                        Load("Movie.dbc", 0, 1, MovieStore);
                        Load("Map.dbc", 0, 6, MapStore);
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
                        max = 10;
                        Load("AreaTrigger.db2", 16, 16, AreaTriggerStore);
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
            }
        }
    }

    public class DbcParameter : Parameter
    {
        public DbcParameter(Dictionary<int, string> storage)
        {
            Items = new Dictionary<int, SelectOption>();
            foreach (int key in storage.Keys)
                Items.Add(key, new SelectOption(storage[key]));
        }
    }
}