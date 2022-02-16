using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Prism.Events;
using WDBXEditor.Storage;
using DBCD;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DbcStore.FastReader;
using WDE.DbcStore.Providers;
using WDE.DbcStore.Spells;
using WDE.DbcStore.Spells.Cataclysm;
using WDE.DbcStore.Spells.Wrath;
using WDE.Module.Attributes;

namespace WDE.DbcStore
{
    public enum DBCVersions
    {
        WOTLK_12340 = 12340,
        CATA_15595 = 15595,
        LEGION_26972 = 26972,
        SHADOWLANDS_41079 = 41079
    }

    public enum DBCLocales
    {
        LANG_enUS = 0,
        LANG_enGB = LANG_enUS,
        LANG_koKR = 1,
        LANG_frFR = 2,
        LANG_deDE = 3,
        LANG_enCN = 4,
        LANG_zhCN = LANG_enCN,
        LANG_enTW = 5,
        LANG_zhTW = LANG_enTW,
        LANG_esES = 6,
        LANG_esMX = 7,
        LANG_ruRU = 8,
        LANG_ptPT = 10,
        LANG_ptBR = LANG_ptPT,
        LANG_itIT = 11
    }

    [AutoRegister]
    [SingleInstance]
    public class DbcStore : IDbcStore, ISpellService
    {
        private readonly IDbcSettingsProvider dbcSettingsProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IEventAggregator eventAggregator;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly NullSpellService nullSpellService;
        private readonly CataSpellService cataSpellService;
        private readonly WrathSpellService wrathSpellService;
        private readonly IParameterFactory parameterFactory;
        private readonly ITaskRunner taskRunner;
        private readonly DBCD.DBCD dbcd;

        public DbcStore(IParameterFactory parameterFactory, 
            ITaskRunner taskRunner,
            IDbcSettingsProvider settingsProvider,
            IMessageBoxService messageBoxService,
            IEventAggregator eventAggregator,
            ICurrentCoreVersion currentCoreVersion,
            NullSpellService nullSpellService,
            CataSpellService cataSpellService,
            WrathSpellService wrathSpellService,
            DBCD.DBCD dbcd)
        {
            this.parameterFactory = parameterFactory;
            this.taskRunner = taskRunner;
            dbcSettingsProvider = settingsProvider;
            this.messageBoxService = messageBoxService;
            this.eventAggregator = eventAggregator;
            this.currentCoreVersion = currentCoreVersion;
            this.nullSpellService = nullSpellService;
            this.cataSpellService = cataSpellService;
            this.wrathSpellService = wrathSpellService;
            this.dbcd = dbcd;

            spellServiceImpl = nullSpellService;
            Load();
        }
        
        public bool IsConfigured { get; private set; }
        public Dictionary<long, string> AreaTriggerStore { get; internal set; } = new();
        public Dictionary<long, long> FactionTemplateStore { get; internal set; } = new();
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
        public Dictionary<long, string> TextEmoteStore { get;internal set; } = new();
        public Dictionary<long, string> AchievementStore { get; internal set;} = new();
        public Dictionary<long, string> ItemStore { get; internal set;} = new();
        public Dictionary<long, string> SpellFocusObjectStore { get; internal set; } = new();
        public Dictionary<long, string> QuestInfoStore { get; internal set; } = new();
        public Dictionary<long, string> CharTitleStore { get; internal set; } = new();
        public Dictionary<long, string> CreatureModelDataStore {get; internal set; } = new();
        public Dictionary<long, string> GameObjectDisplayInfoStore {get; internal set; } = new();
        public Dictionary<long, string> MapDirectoryStore { get; internal set;} = new();
        public Dictionary<long, string> QuestSortStore { get; internal set;} = new();
        
        internal void Load()
        {            
            parameterFactory.Register("RaceMaskParameter", new RaceMaskParameter(currentCoreVersion.Current.GameVersionFeatures.AllRaces), QuickAccessMode.Limited);

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
            private readonly DBDProvider dbdProvider;
            private readonly DBCProvider dbcProvider;

            private Dictionary<long, string> AreaTriggerStore { get; } = new();
            private Dictionary<long, long> FactionTemplateStore { get; } = new();
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
            public Dictionary<long, string> TextEmoteStore { get; } = new();
            public Dictionary<long, string> AchievementStore { get; } = new();
            public Dictionary<long, string> ItemStore { get; } = new();
            public Dictionary<long, string> SpellFocusObjectStore { get; } = new();
            public Dictionary<long, string> QuestInfoStore { get; } = new();
            public Dictionary<long, string> CharTitleStore { get; } = new();
            private Dictionary<long, long> CreatureDisplayInfoStore { get; } = new();
            public Dictionary<long, string> CreatureModelDataStore { get; } = new();
            public Dictionary<long, string> GameObjectDisplayInfoStore { get; } = new();
            public Dictionary<long, string> MapDirectoryStore { get; internal set;} = new();
            public Dictionary<long, string> QuestSortStore { get; internal set;} = new();
            
            public string Name => "DBC Loading";
            public bool WaitForOtherTasks => false;
            private DatabaseClientFileOpener opener;
            
            public DbcLoadTask(IParameterFactory parameterFactory, IDbcSettingsProvider settingsProvider, DbcStore store)
            {
                this.parameterFactory = parameterFactory;
                this.store = store;
                opener = new DatabaseClientFileOpener();
                dbcSettingsProvider = settingsProvider;
            }

            private void Load(string filename, Action<IDbcIterator> foreachRow)
            {
                progress?.Report(now++, max, $"Loading {filename}");
                var path = $"{dbcSettingsProvider.GetSettings().Path}/{filename}";

                if (!File.Exists(path))
                    return;

                foreach (var entry in opener.Open(path))
                    foreachRow(entry);
            }
            
            private void Load(string filename, int id, int nameIndex, Dictionary<long, string> dictionary, bool useLocale = false)
            {
                int locale = (int) DBCLocales.LANG_enUS;

                if (useLocale)
                    locale = (int) dbcSettingsProvider.GetSettings().DBCLocale;

                Load(filename, row => dictionary.Add(row.GetInt(id), row.GetString(nameIndex + locale)));
            }
            
            private void Load(string filename, int id, int nameIndex, Dictionary<long, long> dictionary)
            {
                Load(filename, row => dictionary.Add(row.GetInt(id), row.GetInt(nameIndex)));
            }

            private void Load(string filename, string fieldName, Dictionary<long, string> dictionary)
            {
                var storage = store.dbcd.Load($"{dbcSettingsProvider.GetSettings().Path}/{filename}");

                if (fieldName == String.Empty)
                {
                    foreach (DBCDRow item in storage.Values)
                        dictionary.Add(item.ID, String.Empty);
                }
                else
                {
                    foreach (DBCDRow item in storage.Values)
                    {
                        if (item[fieldName] == null)
                            return;

                        dictionary.Add(item.ID, item[fieldName].ToString());
                    }
                }
            }

            private void Load(string filename, string fieldName, Dictionary<long, long> dictionary)
            {
                var storage = store.dbcd.Load($"{dbcSettingsProvider.GetSettings().Path}/{filename}");

                if (fieldName == String.Empty)
                {
                    foreach (DBCDRow item in storage.Values)
                        dictionary.Add(item.ID, 0);
                }
                else
                {
                    foreach (DBCDRow item in storage.Values)
                    {
                        if (item[fieldName] == null)
                            return;

                        dictionary.Add(item.ID, Convert.ToInt64(item[fieldName]));
                    }
                }
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
                store.TextEmoteStore = TextEmoteStore;
                store.AchievementStore = AchievementStore;
                store.ItemStore = ItemStore;
                store.SpellFocusObjectStore = SpellFocusObjectStore;
                store.QuestInfoStore = QuestInfoStore;
                store.CharTitleStore = CharTitleStore;
                store.CreatureModelDataStore = CreatureModelDataStore;
                store.GameObjectDisplayInfoStore = GameObjectDisplayInfoStore;
                store.MapDirectoryStore = MapDirectoryStore;
                store.QuestSortStore = QuestSortStore;
                
                parameterFactory.Register("AchievementParameter", new DbcParameter(AchievementStore), QuickAccessMode.Full);
                parameterFactory.Register("MovieParameter", new DbcParameter(MovieStore), QuickAccessMode.Limited);
                parameterFactory.Register("FactionParameter", new FactionParameter(FactionStore, FactionTemplateStore), QuickAccessMode.Limited);
                parameterFactory.Register("DbcSpellParameter", new DbcParameter(SpellStore));
                parameterFactory.Register("ItemParameter", new DbcParameter(ItemStore));
                parameterFactory.Register("EmoteParameter", new DbcParameter(EmoteStore), QuickAccessMode.Full);
                parameterFactory.Register("TextEmoteParameter", new DbcParameter(TextEmoteStore), QuickAccessMode.Limited);
                parameterFactory.Register("ClassParameter", new DbcParameter(ClassStore), QuickAccessMode.Limited);
                parameterFactory.Register("ClassMaskParameter", new DbcMaskParameter(ClassStore, -1));
                parameterFactory.Register("RaceParameter", new DbcParameter(RaceStore));
                parameterFactory.Register("SkillParameter", new DbcParameter(SkillStore), QuickAccessMode.Limited);
                parameterFactory.Register("SoundParameter", new DbcParameter(SoundStore), QuickAccessMode.Limited);
                parameterFactory.Register("ZoneAreaParameter", new DbcParameter(AreaStore), QuickAccessMode.Limited);
                parameterFactory.Register("MapParameter", new DbcParameter(MapStore), QuickAccessMode.Limited);
                parameterFactory.Register("PhaseParameter", new DbcParameter(PhaseStore), QuickAccessMode.Limited);
                parameterFactory.Register("SpellFocusObjectParameter", new DbcParameter(SpellFocusObjectStore), QuickAccessMode.Limited);
                parameterFactory.Register("QuestInfoParameter", new DbcParameter(QuestInfoStore));
                parameterFactory.Register("CharTitleParameter", new DbcParameter(CharTitleStore));
                parameterFactory.Register("CreatureModelDataParameter", new CreatureModelParameter(CreatureModelDataStore, CreatureDisplayInfoStore));
                parameterFactory.Register("GameObjectDisplayInfoParameter", new DbcFileParameter(GameObjectDisplayInfoStore));
                parameterFactory.Register("LanguageParameter", new LanguageParameter(LanguageStore), QuickAccessMode.Limited);
                parameterFactory.Register("AreaTriggerParameter", new LanguageParameter(AreaTriggerStore));
                parameterFactory.Register("ZoneOrQuestSortParameter", new ZoneOrQuestSortParameter(AreaStore, QuestSortStore));

                switch (dbcSettingsProvider.GetSettings().DBCVersion)
                {
                    case DBCVersions.WOTLK_12340:
                        store.spellServiceImpl = store.wrathSpellService;
                        break;
                    case DBCVersions.CATA_15595:
                        store.spellServiceImpl = store.cataSpellService;
                        break;
                    case DBCVersions.LEGION_26972:
                        break;
                }
                
                store.eventAggregator.GetEvent<DbcLoadedEvent>().Publish(store);
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
                        store.wrathSpellService.Load(dbcSettingsProvider.GetSettings().Path);
                        max = 23;
                        Load("AreaTrigger.dbc", row => AreaTriggerStore.Add(row.GetInt(0), $"Area trigger at {row.GetFloat(2)}, {row.GetFloat(3)}, {row.GetFloat(4)}"));
                        Load("SkillLine.dbc", 0, 3, SkillStore, true);
                        Load("Faction.dbc", 0, 23, FactionStore, true);
                        Load("FactionTemplate.dbc", 0, 1, FactionTemplateStore);
                        Load("Spell.dbc", 0, 136, SpellStore, true);
                        Load("Movie.dbc", 0, 1, MovieStore);
                        Load("Map.dbc", 0, 5, MapStore, true);
                        Load("Map.dbc", 0, 1, MapDirectoryStore);
                        Load("Achievement.dbc", 0, 4, AchievementStore, true);
                        Load("AreaTable.dbc", 0, 11, AreaStore, true);
                        Load("chrClasses.dbc", 0, 4, ClassStore, true);
                        Load("chrRaces.dbc", 0, 14, RaceStore, true);
                        Load("Emotes.dbc", 0, 1, EmoteStore);
                        Load("EmotesText.dbc", 0, 1, TextEmoteStore);
                        Load("SoundEntries.dbc", 0, 2, SoundStore);
                        Load("SpellFocusObject.dbc", 0, 1, SpellFocusObjectStore, true);
                        Load("QuestInfo.dbc", 0, 1, QuestInfoStore, true);
                        Load("CharTitles.dbc", 0, 2, CharTitleStore, true);
                        Load("CreatureModelData.dbc", 0, 2, CreatureModelDataStore);
                        Load("CreatureDisplayInfo.dbc", 0, 1, CreatureDisplayInfoStore);
                        Load("GameObjectDisplayInfo.dbc", 0, 1, GameObjectDisplayInfoStore);
                        Load("Languages.dbc", 0, 1, LanguageStore, true);
                        Load("QuestSort.dbc", 0, 1, QuestSortStore, true);
                        break;
                    }
                    case DBCVersions.CATA_15595:
                    {
                        store.cataSpellService.Load(dbcSettingsProvider.GetSettings().Path);
                        max = 25;
                        Load("AreaTrigger.dbc", row => AreaTriggerStore.Add(row.GetInt(0), $"Area trigger at {row.GetFloat(2)}, {row.GetFloat(3)}, {row.GetFloat(4)}"));
                        Load("SkillLine.dbc", 0, 2, SkillStore);
                        Load("Faction.dbc", 0, 23, FactionStore);
                        Load("FactionTemplate.dbc", 0, 1, FactionTemplateStore);
                        Load("Spell.dbc", 0, 21, SpellStore);
                        Load("Movie.dbc", 0, 1, MovieStore);
                        Load("Map.dbc", 0, 6, MapStore);
                        Load("Map.dbc", 0, 1, MapDirectoryStore);
                        Load("Achievement.dbc", 0, 4, AchievementStore);
                        Load("AreaTable.dbc", 0, 11, AreaStore);
                        Load("chrClasses.dbc", 0, 3, ClassStore);
                        Load("chrRaces.dbc", 0, 14, RaceStore);
                        Load("Emotes.dbc", 0, 1, EmoteStore);
                        Load("EmotesText.dbc", 0, 1, TextEmoteStore);
                        Load("item-sparse.db2", 0, 99, ItemStore);
                        Load("Phase.dbc", 0, 1, PhaseStore);
                        Load("SoundEntries.dbc", 0, 2, SoundStore);
                        Load("SpellFocusObject.dbc", 0, 1, SpellFocusObjectStore);
                        Load("QuestInfo.dbc", 0, 1, QuestInfoStore);
                        Load("CharTitles.dbc", 0, 2, CharTitleStore);
                        Load("CreatureModelData.dbc", 0, 2, CreatureModelDataStore);
                        Load("CreatureDisplayInfo.dbc", 0, 1, CreatureDisplayInfoStore);
                        Load("GameObjectDisplayInfo.dbc", 0, 1, GameObjectDisplayInfoStore);
                        Load("Languages.dbc", 0, 1, LanguageStore);
                        Load("QuestSort.dbc", 0, 1, QuestSortStore);
                        break;
                    }
                    case DBCVersions.LEGION_26972:
                    {
                        max = 18;
                        Load("AreaTrigger.db2", row => AreaTriggerStore.Add(row.GetInt(16), $"Area trigger at {row.GetFloat(0)}, {row.GetFloat(1)}, {row.GetFloat(2)}"));
                        Load("spell.db2", 0, 1, SpellStore);
                        Load("achievement.db2", 12, 1, AchievementStore);
                        Load("AreaTable.db2", 0, 2, AreaStore);
                        Load("chrClasses.db2", 19, 1, ClassStore);
                        Load("chrRaces.db2", 34, 2, RaceStore);
                        Load("Emotes.db2", 0, 2, EmoteStore);
                        Load("EmotesText.db2", 0, 1, TextEmoteStore);
                        Load("ItemSparse.db2", 0, 2, ItemStore);
                        Load("Languages.db2", 1, 0, LanguageStore);
                        Load("Map.db2", 0, 1, MapDirectoryStore);
                        Load("Faction.db2", 6, 4, FactionStore);
                        Load("FactionTemplate.db2", 0, 1, FactionTemplateStore);
                        // Load("Phase.db2", 1, 0, PhaseStore); // no names in legion :(
                        Load("SoundKitName.db2", 0, 1, SoundStore);
                        Load("SpellFocusObject.db2", 0, 1, SpellFocusObjectStore);
                        Load("QuestInfo.db2", 0, 1, QuestInfoStore);
                        Load("CharTitles.db2", 0, 1, CharTitleStore);
                        Load("CreatureDisplayInfo.db2", 0, 2, CreatureDisplayInfoStore);
                        break;
                    }
                    case DBCVersions.SHADOWLANDS_41079:
                    {
                        max = 17;
                        Load("AreaTrigger.db2", string.Empty, AreaTriggerStore);
                        Load("SpellName.db2", "Name_lang", SpellStore);
                        Load("Achievement.db2", "Title_lang", AchievementStore);
                        Load("AreaTable.db2", "AreaName_lang", AreaStore);
                        Load("ChrClasses.db2", "Name_lang", ClassStore);
                        Load("ChrRaces.db2", "Name_lang", RaceStore);
                        Load("Emotes.db2", "EmoteSlashCommand", EmoteStore);
                        Load("EmotesText.db2", "Name", TextEmoteStore);
                        Load("ItemSparse.db2", "Display_lang", ItemStore);
                        Load("Languages.db2", "Name_lang", LanguageStore);
                        Load("Map.db2", "MapName_lang", MapDirectoryStore);
                        Load("Faction.db2", "Name_lang", FactionStore);
                        Load("FactionTemplate.db2", "Faction", FactionTemplateStore);
                        Load("SpellFocusObject.db2", "Name_lang", SpellFocusObjectStore);
                        Load("QuestInfo.db2", "InfoName_lang", QuestInfoStore);
                        Load("CharTitles.db2", "Name_lang", CharTitleStore);
                        Load("QuestSort.db2", "SortName_lang", QuestSortStore);
                        break;
                    }
                    default:
                        return;
                }

                switch (dbcSettingsProvider.GetSettings().DBCLocale)
                {
                    case DBCLocales.LANG_enUS:
                        Validate(SpellStore, 1, "Word of Recall (OLD)");
                        break;
                    case DBCLocales.LANG_frFR:
                        Validate(SpellStore, 1, "Mot de rappel (OLD)");
                        break;
                    default:
                        return;
                }
            }

            private void Validate(Dictionary<long,string> dict, int id, string expectedName)
            {
                if (dict.TryGetValue(id, out var realName) && realName == expectedName)
                    return;

                var settings = dbcSettingsProvider.GetSettings();

                store.messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Error)
                    .SetTitle("Invalid DBC")
                    .SetMainInstruction("Invalid DBC path")
                    .SetContent(
                        $"In specified path, there is no DBC for version {settings.DBCVersion}. Ensure the path contains Spell.dbc or Spell.db2 file.\n\nPath: {settings.Path}")
                    .WithOkButton(false)
                    .Build());
                throw new Exception("Invalid DBC!");
            }
        }

        private ISpellService spellServiceImpl;
        public bool Exists(uint spellId) => spellServiceImpl.Exists(spellId);

        public T GetAttributes<T>(uint spellId) where T : Enum => spellServiceImpl.GetAttributes<T>(spellId);
        public uint? GetSkillLine(uint spellId) => spellServiceImpl.GetSkillLine(spellId);
        public uint? GetSpellFocus(uint spellId) => spellServiceImpl.GetSpellFocus(spellId);
        public TimeSpan? GetSpellCastingTime(uint spellId) => spellServiceImpl.GetSpellCastingTime(spellId);

        public string? GetDescription(uint spellId) => spellServiceImpl.GetDescription(spellId);

        public int GetSpellEffectsCount(uint spellId) => spellServiceImpl.GetSpellEffectsCount(spellId);
        public SpellEffectType GetSpellEffectType(uint spellId, int index) => spellServiceImpl.GetSpellEffectType(spellId, index);
        public (SpellTarget, SpellTarget) GetSpellEffectTargetType(uint spellId, int index) => spellServiceImpl.GetSpellEffectTargetType(spellId, index);

        public uint GetSpellEffectMiscValueA(uint spellId, int index) => spellServiceImpl.GetSpellEffectMiscValueA(spellId, index);
    }

    internal class FactionParameter : ParameterNumbered
    {
        public FactionParameter(Dictionary<long, string> factionStore, Dictionary<long, long> factionTemplateStore)
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (var factionTemplate in factionTemplateStore)
            {
                if (factionStore.TryGetValue(factionTemplate.Value, out var factionName))
                    Items.Add(factionTemplate.Key, new SelectOption(factionName));
                else
                    Items.Add(factionTemplate.Key, new SelectOption("unknown name"));
            }
        }
    }

    internal class CreatureModelParameter : ParameterNumbered
    {
        public CreatureModelParameter(Dictionary<long, string> creatureModelData, Dictionary<long, long> creatureDisplayInfo)
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (var displayInfo in creatureDisplayInfo)
            {
                if (creatureModelData.TryGetValue(displayInfo.Value, out var modelPath))
                    Items.Add(displayInfo.Key, new SelectOption(GetFileName(modelPath), modelPath));
                else
                    Items.Add(displayInfo.Key, new SelectOption("unknown model"));
            }
        }
        
        private string GetFileName(string s)
        {
            int indexOf = s.LastIndexOf('\\');
            return indexOf == -1 ? s : s.Substring(indexOf + 1);
        }
    }

    public class DbcParameter : ParameterNumbered
    {
        public DbcParameter(Dictionary<long, string> storage)
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (var (key, value) in storage)
                Items.Add(key, new SelectOption(value));
        }

        public bool AllowUnknownItems => true;
    }

    public class LanguageParameter : DbcParameter
    {
        public LanguageParameter(Dictionary<long, string> storage) : base(storage)
        {
            Items.Add(0, new SelectOption("Universal"));
        }
    }

    public class ZoneOrQuestSortParameter : DbcParameter
    {
        public ZoneOrQuestSortParameter(Dictionary<long, string> zones, Dictionary<long, string> questSorts) : base(zones)
        {
            foreach (var pair in questSorts)
                Items.Add(-pair.Key, new SelectOption(pair.Value));
        }
    }

    public class DbcFileParameter : Parameter
    {
        public DbcFileParameter(Dictionary<long, string> storage)
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (int key in storage.Keys)
                Items.Add(key, new SelectOption(GetFileName(storage[key]), storage[key]));
        }

        private string GetFileName(string s)
        {
            int indexOf = s.LastIndexOf('\\');
            return indexOf == -1 ? s : s.Substring(indexOf + 1);
        }
    }
    
    public class DbcMaskParameter : FlagParameter
    {
        public DbcMaskParameter(Dictionary<long, string> storage, int offset)
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (int key in storage.Keys)
                Items.Add(1L << (key + offset), new SelectOption(storage[key]));
        }
    }

    public class RaceMaskParameter : FlagParameter
    {
        private CharacterRaces alliance;
        private CharacterRaces horde;
        private CharacterRaces all;

        public override string ToString(long value)
        {
            if ((long)all == value)
                return "Any race";
            if ((long)alliance == value)
                return "Alliance";
            if ((long)horde == value)
                return "Horde";
            return base.ToString(value);
        }

        private static bool IsPowerOfTwo(ulong x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    
        public RaceMaskParameter(CharacterRaces allowableRaces)
        {
            Items = new Dictionary<long, SelectOption>();

            alliance = allowableRaces & CharacterRaces.AllAlliance;
            horde = allowableRaces & CharacterRaces.AllHorde;
            all = allowableRaces;
            Items.Add((long)alliance, new SelectOption("Alliance"));
            Items.Add((long)horde, new SelectOption("Horde"));
            
            foreach (CharacterRaces race in Enum.GetValues<CharacterRaces>())
            {
                if (IsPowerOfTwo((ulong)race) && allowableRaces.HasFlag(race))
                {
                    Items.Add((long)race, new SelectOption(race.ToString().ToTitleCase()));
                }
            }
        }
    }
    
}