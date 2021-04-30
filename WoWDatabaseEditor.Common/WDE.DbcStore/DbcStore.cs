using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using WDBXEditor.Reader;
using WDBXEditor.Storage;
using WDE.Common;
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
        private readonly ISolutionManager solutionManager;
        private readonly IParameterFactory parameterFactory;
        private readonly ITaskRunner taskRunner;

        public DbcStore(IParameterFactory parameterFactory, ITaskRunner taskRunner, IDbcSettingsProvider settingsProvider, ISolutionManager solutionManager)
        {
            this.parameterFactory = parameterFactory;
            this.taskRunner = taskRunner;
            dbcSettingsProvider = settingsProvider;
            this.solutionManager = solutionManager;

            Load();
        }
        
        public bool IsConfigured { get; private set; }
        public Dictionary<long, string> AreaTriggerStore { get; internal set; } = new();
        public Dictionary<long, string> FactionStore { get; internal set; } = new();
        public Dictionary<long, string> SpellStore { get; internal set; } = new();
        public Dictionary<long, string> SkillStore { get; internal set;} = new();
        public Dictionary<long, string> LanguageStore { get; internal set;} = new();
        public Dictionary<long, string> PhaseStore { get; internal set;} = new();
        public Dictionary<long, string> AreaStore { get; internal set;} = new();
        public Dictionary<long, string> MapStore { get; internal set;} = new();
        public Dictionary<long, string> SoundStore { get;internal set; } = new();
        public Dictionary<long, string> MovieStore { get; internal set;} = new();
        public Dictionary<long, string> ClassStore { get; internal set;} = new();
        public Dictionary<long, string> RaceStore { get; internal set;} = new();
        public Dictionary<long, string> EmoteStore { get;internal set; } = new();
        public Dictionary<long, string> AchievementStore { get; internal set;} = new();
        public Dictionary<long, string> ItemStore { get; internal set;} = new();
        public Dictionary<long, string> SpellFocusObjectStore { get; internal set; } = new();
        public Dictionary<long, string> QuestInfoStore { get; internal set; } = new();
        public Dictionary<long, string> CharTitleStore { get; internal set; } = new();

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

            IsConfigured = true;
            taskRunner.ScheduleTask(new DbcLoadTask(parameterFactory, dbcSettingsProvider, this));
        }

        private class DbcLoadTask : IThreadedTask
        {
            private readonly IDbcSettingsProvider dbcSettingsProvider;
            private readonly IParameterFactory parameterFactory;
            private readonly DbcStore store;

            private Dictionary<long, string> AreaTriggerStore { get; } = new();
            private Dictionary<long, string> FactionStore { get; } = new();
            private Dictionary<long, string> SpellStore { get; } = new();
            public Dictionary<long, string> SkillStore { get; } = new();
            public Dictionary<long, string> LanguageStore { get; } = new();
            public Dictionary<long, string> PhaseStore { get; } = new();
            public Dictionary<long, string> AreaStore { get; } = new();
            public Dictionary<long, string> MapStore { get; } = new();
            public Dictionary<long, string> SoundStore { get; } = new();
            public Dictionary<long, string> MovieStore { get; } = new();
            public Dictionary<long, string> ClassStore { get; } = new();
            public Dictionary<long, string> RaceStore { get; } = new();
            public Dictionary<long, string> EmoteStore { get; } = new();
            public Dictionary<long, string> AchievementStore { get; } = new();
            public Dictionary<long, string> ItemStore { get; } = new();
            public Dictionary<long, string> SpellFocusObjectStore { get; } = new();
            public Dictionary<long, string> QuestInfoStore { get; } = new();
            public Dictionary<long, string> CharTitleStore { get; } = new();
            
            public string Name => "DBC Loading";
            public bool WaitForOtherTasks => false;
            
            public DbcLoadTask(IParameterFactory parameterFactory, IDbcSettingsProvider settingsProvider, DbcStore store)
            {
                this.parameterFactory = parameterFactory;
                this.store = store;
                dbcSettingsProvider = settingsProvider;
            }
            
            private void Load(string filename, int id, int nameIndex, Dictionary<long, string> dictionary)
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
                store.SpellFocusObjectStore = SpellFocusObjectStore;
                store.QuestInfoStore = QuestInfoStore;
                store.CharTitleStore = CharTitleStore;
                
                parameterFactory.Register("MovieParameter", new DbcParameter(MovieStore));
                parameterFactory.Register("FactionParameter", new DbcParameter(FactionStore));
                parameterFactory.Register("SpellParameter", new DbcParameter(SpellStore));
                parameterFactory.Register("SpellAreaSpellParameter", new SpellAreaSpellParameter(SpellStore));
                parameterFactory.Register("ItemParameter", new DbcParameter(ItemStore));
                parameterFactory.Register("EmoteParameter", new DbcParameter(EmoteStore));
                parameterFactory.Register("ClassParameter", new DbcParameter(ClassStore));
                parameterFactory.Register("ClassMaskParameter", new DbcMaskParameter(ClassStore, -1));
                parameterFactory.Register("RaceParameter", new DbcParameter(RaceStore));
                parameterFactory.Register("SkillParameter", new DbcParameter(SkillStore));
                parameterFactory.Register("SoundParameter", new DbcParameter(SoundStore));
                parameterFactory.Register("ZoneAreaParameter", new DbcParameter(AreaStore));
                parameterFactory.Register("MapParameter", new DbcParameter(MapStore));
                parameterFactory.Register("PhaseParameter", new DbcParameter(PhaseStore));
                parameterFactory.Register("SpellFocusObjectParameter", new DbcParameter(SpellFocusObjectStore));
                parameterFactory.Register("QuestInfoParameter", new DbcParameter(QuestInfoStore));
                parameterFactory.Register("CharTitleParameter", new DbcParameter(CharTitleStore));
                
                store.solutionManager.RefreshAll();
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
                        max = 15;
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
                        Load("SpellFocusObject.dbc", 0, 1, SpellFocusObjectStore);
                        Load("QuestInfo.dbc", 0, 1, QuestInfoStore);
                        Load("CharTitles.dbc", 0, 2, CharTitleStore);
                        break;
                    }
                    case DBCVersions.CATA_15595:
                    {
                        max = 17;
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
                        Load("SpellFocusObject.dbc", 0, 1, SpellFocusObjectStore);
                        Load("QuestInfo.dbc", 0, 1, QuestInfoStore);
                        Load("CharTitles.dbc", 0, 2, CharTitleStore);
                        break;
                    }
                    case DBCVersions.LEGION_26972:
                    {
                        max = 13;
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
                        Load("SpellFocusObject.db2", 0, 1, SpellFocusObjectStore);
                        Load("QuestInfo.db2", 0, 1, QuestInfoStore);
                        Load("CharTitles.db2", 0, 1, CharTitleStore);
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
        public DbcParameter(Dictionary<long, string> storage)
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (int key in storage.Keys)
                Items.Add(key, new SelectOption(storage[key]));
        }
    }

    public class SpellAreaSpellParameter : Parameter
    {
        public SpellAreaSpellParameter(Dictionary<long, string> storage)
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (int key in storage.Keys)
            {
                Items.Add(-key, new SelectOption(storage[key], "If the player HAS aura, then the spell will not be activated"));
                Items.Add(key, new SelectOption(storage[key], "If the player has NO aura, then the spell will not be activated"));
            }
        }
    }
    
    public class DbcMaskParameter : FlagParameter
    {
        public DbcMaskParameter(Dictionary<long, string> storage, int offset)
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (int key in storage.Keys)
                Items.Add(1 << (key + offset), new SelectOption(storage[key]));
        }
    }
    
}