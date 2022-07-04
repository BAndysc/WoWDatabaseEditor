using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Prism.Events;
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
        MOP_18414 = 18414,
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
        private readonly DatabaseClientFileOpener opener;
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
            DatabaseClientFileOpener opener,
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
            this.opener = opener;
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
        public Dictionary<long, string> CurrencyTypeStore { get; internal set;} = new();
        public Dictionary<long, string> ClassStore { get; internal set;} = new();
        public Dictionary<long, string> RaceStore { get; internal set;} = new();
        public Dictionary<long, string> EmoteStore { get;internal set; } = new();
        public Dictionary<long, string> EmoteOneShotStore { get;internal set; } = new();
        public Dictionary<long, string> EmoteStateStore { get;internal set; } = new();
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
        public Dictionary<long, string> SceneStore { get; internal set; } = new();
        
        internal void Load()
        {            
            parameterFactory.Register("RaceMaskParameter", new RaceMaskParameter(currentCoreVersion.Current.GameVersionFeatures.AllRaces), QuickAccessMode.Limited);

            if (dbcSettingsProvider.GetSettings().SkipLoading ||
                !Directory.Exists(dbcSettingsProvider.GetSettings().Path))
            {
                // we create a new fake task, that will not be started, but finalized so that (empty) parameters are registered
                var fakeTask = new DbcLoadTask(parameterFactory, opener, dbcSettingsProvider, this);
                fakeTask.FinishMainThread();
                return;
            }

            IsConfigured = true;
            taskRunner.ScheduleTask(new DbcLoadTask(parameterFactory, opener, dbcSettingsProvider, this));
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
            public Dictionary<long, string> EmoteOneShotStore { get; } = new();
            public Dictionary<long, string> EmoteStateStore { get; } = new();
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
            public Dictionary<long, string> CurrencyTypeStore { get; internal set;} = new();
            public Dictionary<long, string> ExtendedCostStore { get; internal set;} = new();
            public Dictionary<long, string> TaxiNodeStore { get; internal set;} = new();
            public Dictionary<long, (int, int)> TaxiPathsStore { get; internal set;} = new();
            public Dictionary<long, string> SpellItemEnchantmentStore { get; internal set;} = new();
            public Dictionary<long, string> AreaGroupStore { get; internal set;} = new();
            public Dictionary<long, string> ItemDisplayInfoStore { get; internal set;} = new();
            public Dictionary<long, string> MailTemplateStore { get; internal set;} = new();
            public Dictionary<long, string> LFGDungeonStore { get; internal set;} = new();
            public Dictionary<long, string> ItemSetStore { get; internal set;} = new();
            public Dictionary<long, string> DungeonEncounterStore { get; internal set;} = new();
            public Dictionary<long, string> HolidayNamesStore { get; internal set;} = new();
            public Dictionary<long, string> HolidaysStore { get; internal set;} = new();
            public Dictionary<long, string> WorldSafeLocsStore { get; internal set;} = new();
            public Dictionary<long, string> BattlegroundStore { get; internal set;} = new();
            public Dictionary<long, string> AchievementCriteriaStore { get; internal set;} = new();
            public Dictionary<long, string> ItemDbcStore { get; internal set;} = new(); // item.dbc, not item-sparse.dbc
            
            public Dictionary<long, string> SceneStore { get; internal set;} = new();

            public string Name => "DBC Loading";
            public bool WaitForOtherTasks => false;
            private DatabaseClientFileOpener opener;
            
            public DbcLoadTask(IParameterFactory parameterFactory, DatabaseClientFileOpener opener, IDbcSettingsProvider settingsProvider, DbcStore store)
            {
                this.parameterFactory = parameterFactory;
                this.store = store;
                this.opener = opener;
                dbcSettingsProvider = settingsProvider;
            }

            private void LoadAndRegister(string filename, string parameter, int keyIndex, Func<IDbcIterator, string> getString)
            {
                Dictionary<long, SelectOption> dict = new();
                Load(filename, row =>
                {
                    var id = row.GetUInt(keyIndex);
                    dict[id] = new SelectOption(getString(row));
                });
                parameterFactory.Register(parameter, new Parameter()
                {
                    Items = dict
                });
            }

            private void LoadAndRegister(string filename, string parameter, int keyIndex, int nameIndex, bool withLocale = false)
            {
                int locale = (int) DBCLocales.LANG_enUS;

                if (withLocale)
                    locale = (int) dbcSettingsProvider.GetSettings().DBCLocale;
                LoadAndRegister(filename, parameter, keyIndex, r => r.GetString(nameIndex + locale));
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

            private void LoadDB2(string filename, Action<DBCDRow> doAction)
            {
                var storage = store.dbcd.Load($"{dbcSettingsProvider.GetSettings().Path}/{filename}");
                foreach (DBCDRow item in storage.Values)
                    doAction(item);
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
                store.EmoteOneShotStore = EmoteOneShotStore;
                store.EmoteStateStore = EmoteStateStore;
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
                store.SceneStore = SceneStore;
                store.CurrencyTypeStore = CurrencyTypeStore;
                
                parameterFactory.Register("AchievementParameter", new DbcParameter(AchievementStore), QuickAccessMode.Full);
                parameterFactory.Register("MovieParameter", new DbcParameter(MovieStore), QuickAccessMode.Limited);
                parameterFactory.Register("RawFactionParameter", new DbcParameter(FactionStore), QuickAccessMode.Limited);
                parameterFactory.Register("FactionParameter", new FactionParameter(FactionStore, FactionTemplateStore), QuickAccessMode.Limited);
                parameterFactory.Register("DbcSpellParameter", new DbcParameter(SpellStore));
                parameterFactory.Register("CurrencyTypeParameter", new DbcParameter(CurrencyTypeStore));
                parameterFactory.Register("ItemDbcParameter", new DbcParameter(ItemStore));
                parameterFactory.Register("EmoteParameter", new DbcParameter(EmoteStore), QuickAccessMode.Full);
                parameterFactory.Register("EmoteOneShotParameter", new DbcParameter(EmoteOneShotStore));
                parameterFactory.Register("EmoteStateParameter", new DbcParameter(EmoteStateStore));
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
                parameterFactory.Register("ExtendedCostParameter", new DbcParameter(ExtendedCostStore));
                parameterFactory.Register("CreatureModelDataParameter", new CreatureModelParameter(CreatureModelDataStore, CreatureDisplayInfoStore));
                parameterFactory.Register("GameObjectDisplayInfoParameter", new DbcFileParameter(GameObjectDisplayInfoStore));
                parameterFactory.Register("LanguageParameter", new LanguageParameter(LanguageStore), QuickAccessMode.Limited);
                parameterFactory.Register("AreaTriggerParameter", new DbcParameter(AreaTriggerStore));
                parameterFactory.Register("ZoneOrQuestSortParameter", new ZoneOrQuestSortParameter(AreaStore, QuestSortStore));
                parameterFactory.Register("TaxiPathParameter", new TaxiPathParameter(TaxiPathsStore, TaxiNodeStore));
                parameterFactory.Register("SpellItemEnchantmentParameter", new DbcParameter(SpellItemEnchantmentStore));
                parameterFactory.Register("AreaGroupParameter", new DbcParameter(AreaGroupStore));
                parameterFactory.Register("ItemDisplayInfoParameter", new DbcParameter(ItemDisplayInfoStore));
                parameterFactory.Register("MailTemplateParameter", new DbcParameter(MailTemplateStore));
                parameterFactory.Register("LFGDungeonParameter", new DbcParameter(LFGDungeonStore));
                parameterFactory.Register("ItemSetParameter", new DbcParameter(ItemSetStore));
                parameterFactory.Register("DungeonEncounterParameter", new DbcParameter(DungeonEncounterStore));
                parameterFactory.Register("HolidaysParameter", new DbcParameter(HolidaysStore));
                parameterFactory.Register("WorldSafeLocParameter", new DbcParameter(WorldSafeLocsStore));
                parameterFactory.Register("BattlegroundParameter", new DbcParameter(BattlegroundStore));
                parameterFactory.Register("AchievementCriteriaParameter", new DbcParameter(AchievementCriteriaStore));
                parameterFactory.Register("ItemVisualParameter", new DbcParameter(ItemDbcStore));
                parameterFactory.Register("SceneScriptPackage", new DbcParameter(SceneStore));

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

                switch (dbcSettingsProvider.GetSettings().DBCVersion)
                {
                    case DBCVersions.WOTLK_12340:
                    {
                        store.wrathSpellService.Load(dbcSettingsProvider.GetSettings().Path);
                        max = 43;
                        Load("AreaTrigger.dbc", row => AreaTriggerStore.Add(row.GetInt(0), $"Area trigger"));
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
                        Load("Emotes.dbc", row =>
                        {
                            var proc = row.GetUInt(4);
                            if (proc == 0)
                                EmoteOneShotStore.Add(row.GetUInt(0), row.GetString(1));
                            else if (proc == 2)
                                EmoteStateStore.Add(row.GetUInt(0), row.GetString(1));
                            EmoteStore.Add(row.GetUInt(0), row.GetString(1));
                        });
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
                        Load("ItemExtendedCost.dbc", row => ExtendedCostStore.Add(row.GetInt(0), GenerateCostDescription(row.GetInt(1), row.GetInt(2), row.GetInt(4))));
                        Load("TaxiNodes.dbc", 0, 5, TaxiNodeStore, true);
                        Load("TaxiPath.dbc",  row => TaxiPathsStore.Add(row.GetUInt(0), (row.GetInt(1), row.GetInt(2))));
                        Load("SpellItemEnchantment.dbc", 0, 14, SpellItemEnchantmentStore, true);
                        Load("AreaGroup.dbc",  row => AreaGroupStore.Add(row.GetUInt(0), BuildAreaGroupName(row, 1, 6)));
                        Load("ItemDisplayInfo.dbc", 0, 5, ItemDisplayInfoStore);
                        Load("MailTemplate.dbc", row =>
                        {
                            int locale = (int) dbcSettingsProvider.GetSettings().DBCLocale;
                            var subject = row.GetString(1 + locale);
                            var body = row.GetString(18 + locale);
                            var name = string.IsNullOrEmpty(subject) ? body.TrimToLength(50) : subject;
                            MailTemplateStore.Add(row.GetUInt(0), name.Replace("\n", ""));
                        });
                        Load("LFGDungeons.dbc", 0, 1, LFGDungeonStore, true);
                        Load("ItemSet.dbc", 0, 1, ItemSetStore, true);
                        Load("DungeonEncounter.dbc", 0, 5, DungeonEncounterStore, true);
                        Load("HolidayNames.dbc", 0, 1, HolidayNamesStore, true);
                        Load("Holidays.dbc", row =>
                        {
                            var id = row.GetUInt(0);
                            var nameId = row.GetUInt(49);
                            if (HolidayNamesStore.TryGetValue(nameId, out var name))
                                HolidaysStore[id] = name;
                            else
                                HolidaysStore[id] = "Holiday " + id;
                        });
                        Load("WorldSafeLocs.dbc", 0, 5, WorldSafeLocsStore, true);
                        Load("BattlemasterList.dbc", 0, 11, BattlegroundStore, true);
                        Load("Achievement_Criteria.dbc", 0, 9, AchievementCriteriaStore, true);
                        Load("Item.dbc", row =>
                        {
                            var id = row.GetUInt(0);
                            var displayId = row.GetUInt(5);
                            if (ItemDisplayInfoStore.TryGetValue(displayId, out var name))
                                ItemDbcStore[id] = name;
                            else
                                ItemDbcStore[id] = "Item " + id;
                        });
                        LoadAndRegister("SpellCastTimes.dbc", "SpellCastTimeParameter", 0, row => GetCastTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
                        LoadAndRegister("SpellDuration.dbc", "SpellDurationParameter", 0, row => GetDurationTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
                        LoadAndRegister("SpellRange.dbc", "SpellRangeParameter", 0, 6, true);
                        LoadAndRegister("SpellRadius.dbc", "SpellRadiusParameter", 0, row => GetRadiusDescription(row.GetFloat(1), row.GetFloat(2), row.GetFloat(3)));
                        break;
                    }
                    case DBCVersions.CATA_15595:
                    {
                        store.cataSpellService.Load(dbcSettingsProvider.GetSettings().Path);
                        max = 44;
                        Load("AreaTrigger.dbc", row => AreaTriggerStore.Add(row.GetInt(0), $"Area trigger"));
                        Load("SkillLine.dbc", 0, 2, SkillStore);
                        Load("Faction.dbc", 0, 23, FactionStore);
                        Load("FactionTemplate.dbc", 0, 1, FactionTemplateStore);
                        Load("CurrencyTypes.db2", 0, 2, CurrencyTypeStore);
                        Load("Spell.dbc", 0, 21, SpellStore);
                        Load("Movie.dbc", 0, 1, MovieStore);
                        Load("Map.dbc", 0, 6, MapStore);
                        Load("Map.dbc", 0, 1, MapDirectoryStore);
                        Load("Achievement.dbc", 0, 4, AchievementStore);
                        Load("AreaTable.dbc", 0, 11, AreaStore);
                        Load("chrClasses.dbc", 0, 3, ClassStore);
                        Load("chrRaces.dbc", 0, 14, RaceStore);
                        Load("Emotes.dbc", row =>
                        {
                            var proc = row.GetUInt(4);
                            if (proc == 0)
                                EmoteOneShotStore.Add(row.GetUInt(0), row.GetString(1));
                            else if (proc == 2)
                                EmoteStateStore.Add(row.GetUInt(0), row.GetString(1));
                            EmoteStore.Add(row.GetUInt(0), row.GetString(1));
                        });
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
                        Load("ItemExtendedCost.dbc", row => ExtendedCostStore.Add(row.GetInt(0), GenerateCostDescription(row.GetInt(1), row.GetInt(2), row.GetInt(4))));
                        Load("TaxiNodes.dbc", 0, 5, TaxiNodeStore);
                        Load("TaxiPath.dbc",  row => TaxiPathsStore.Add(row.GetUInt(0), (row.GetInt(1), row.GetInt(2))));
                        Load("SpellItemEnchantment.dbc", 0, 14, SpellItemEnchantmentStore);
                        Load("AreaGroup.dbc",  row => AreaGroupStore.Add(row.GetUInt(0), BuildAreaGroupName(row, 1, 6)));
                        Load("ItemDisplayInfo.dbc", 0, 5, ItemDisplayInfoStore);
                        Load("MailTemplate.dbc", row =>
                        {
                            var subject = row.GetString(1);
                            var body = row.GetString(2);
                            var name = string.IsNullOrEmpty(subject) ? body.TrimToLength(50) : subject;
                            MailTemplateStore.Add(row.GetUInt(0), name.Replace("\n", ""));
                        });
                        Load("LFGDungeons.dbc", 0, 1, LFGDungeonStore);
                        Load("ItemSet.dbc", 0, 1, ItemSetStore);
                        Load("DungeonEncounter.dbc", 0, 5, DungeonEncounterStore);
                        Load("HolidayNames.dbc", 0, 1, HolidayNamesStore);
                        Load("Holidays.dbc", row =>
                        {
                            var id = row.GetUInt(0);
                            var nameId = row.GetUInt(49);
                            if (HolidayNamesStore.TryGetValue(nameId, out var name))
                                HolidaysStore[id] = name;
                            else
                                HolidaysStore[id] = "Holiday " + id;
                        });
                        Load("WorldSafeLocs.dbc", 0, 5, WorldSafeLocsStore);
                        Load("BattlemasterList.dbc", 0, 11, BattlegroundStore);
                        Load("Achievement_Criteria.dbc", 0, 10, AchievementCriteriaStore);
                        Load("Item.dbc", row =>
                        {
                            var id = row.GetUInt(0);
                            var displayId = row.GetUInt(5);
                            if (ItemDisplayInfoStore.TryGetValue(displayId, out var name))
                                ItemDbcStore[id] = name;
                            else
                                ItemDbcStore[id] = "Item " + id;
                        });
                        LoadAndRegister("SpellCastTimes.dbc", "SpellCastTimeParameter", 0, row => GetCastTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
                        LoadAndRegister("SpellDuration.dbc", "SpellDurationParameter", 0, row => GetDurationTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
                        LoadAndRegister("SpellRange.dbc", "SpellRangeParameter", 0, 6);
                        LoadAndRegister("SpellRadius.dbc", "SpellRadiusParameter", 0, row => GetRadiusDescription(row.GetFloat(1), row.GetFloat(2), row.GetFloat(3)));
                        break;
                    }
                    case DBCVersions.MOP_18414:
                    {
                        store.cataSpellService.Load(dbcSettingsProvider.GetSettings().Path);
                        max = 49;
                        var fileData = new Dictionary<long, string>();
                        Load("Achievement_Criteria.dbc", 0, 10, AchievementCriteriaStore);
                        Load("FileData.dbc", 0, 1, fileData);
                        Load("AreaTrigger.dbc", row => AreaTriggerStore.Add(row.GetInt(0), $"Area trigger"));
                        Load("BattlemasterList.dbc", 0, 19, BattlegroundStore);
                        Load("SkillLine.dbc", 0, 2, SkillStore);
                        Load("Faction.dbc", 0, 23, FactionStore);
                        Load("FactionTemplate.dbc", 0, 1, FactionTemplateStore);
                        Load("CurrencyTypes.dbc", 0, 2, CurrencyTypeStore);
                        Load("Spell.dbc", 0, 1, SpellStore);
                        Load("Movie.dbc", row => MovieStore.Add(row.GetInt(0), fileData.GetValueOrDefault(row.GetInt(3)) ?? "Unknown movie"));
                        Load("Map.dbc", 0, 5, MapStore);
                        Load("Map.dbc", 0, 1, MapDirectoryStore);
                        Load("Achievement.dbc", 0, 4, AchievementStore);
                        Load("AreaTable.dbc", 0, 13, AreaStore);
                        Load("ChrClasses.dbc", 0, 3, ClassStore);
                        Load("ChrRaces.dbc", 0, 14, RaceStore);
                        Load("Emotes.dbc", row =>
                        {
                            var proc = row.GetUInt(4);
                            if (proc == 0)
                                EmoteOneShotStore.Add(row.GetUInt(0), row.GetString(1));
                            else if (proc == 2)
                                EmoteStateStore.Add(row.GetUInt(0), row.GetString(1));
                            EmoteStore.Add(row.GetUInt(0), row.GetString(1));
                        });
                        Load("EmotesText.dbc", 0, 1, TextEmoteStore);
                        Load("Item-sparse.db2", 0, 100, ItemStore);
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
                        Load("ItemExtendedCost.dbc", row => ExtendedCostStore.Add(row.GetInt(0), GenerateCostDescription(row.GetInt(1), row.GetInt(2), row.GetInt(4))));
                        Load("TaxiNodes.dbc", 0, 5, TaxiNodeStore);
                        Load("TaxiPath.dbc",  row => TaxiPathsStore.Add(row.GetUInt(0), (row.GetInt(1), row.GetInt(2))));
                        Load("SpellItemEnchantment.dbc", 0, 11, SpellItemEnchantmentStore);
                        Load("AreaGroup.dbc",  row => AreaGroupStore.Add(row.GetUInt(0), BuildAreaGroupName(row, 1, 6)));
                        Load("ItemDisplayInfo.dbc", 0, 5, ItemDisplayInfoStore);
                        Load("MailTemplate.dbc", row =>
                        {
                            var subject = row.GetString(1);
                            var body = row.GetString(2);
                            var name = string.IsNullOrEmpty(subject) ? body.TrimToLength(50) : subject;
                            MailTemplateStore.Add(row.GetUInt(0), name.Replace("\n", ""));
                        });
                        Load("LFGDungeons.dbc", 0, 1, LFGDungeonStore);
                        Load("ItemSet.dbc", 0, 1, ItemSetStore);
                        Load("DungeonEncounter.dbc", 0, 5, DungeonEncounterStore);
                        Load("HolidayNames.dbc", 0, 1, HolidayNamesStore);
                        Load("Holidays.dbc", row =>
                        {
                            var id = row.GetUInt(0);
                            var nameId = row.GetUInt(49);
                            if (HolidayNamesStore.TryGetValue(nameId, out var name))
                                HolidaysStore[id] = name;
                            else
                                HolidaysStore[id] = "Holiday " + id;
                        });
                        Load("WorldSafeLocs.dbc", 0, 6, WorldSafeLocsStore);
                        Load("Item.dbc", row =>
                        {
                            var id = row.GetUInt(0);
                            var displayId = row.GetUInt(5);
                            if (ItemDisplayInfoStore.TryGetValue(displayId, out var name))
                                ItemDbcStore[id] = name;
                            else
                                ItemDbcStore[id] = "Item " + id;
                        });
                        LoadAndRegister("SpellCastTimes.dbc", "SpellCastTimeParameter", 0, row => GetCastTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
                        LoadAndRegister("SpellDuration.dbc", "SpellDurationParameter", 0, row => GetDurationTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
                        LoadAndRegister("SpellRange.dbc", "SpellRangeParameter", 0, 6);
                        LoadAndRegister("SpellRadius.dbc", "SpellRadiusParameter", 0, row => GetRadiusDescription(row.GetFloat(1), row.GetFloat(2), row.GetFloat(4)));
                        break;
                    }
                    case DBCVersions.LEGION_26972:
                    {
                        max = 39;
                        Load("CriteriaTree.db2", 0, 1, AchievementCriteriaStore);
                        Load("AreaTrigger.db2", row => AreaTriggerStore.Add(row.GetInt(14), $"Area trigger"));
                        Load("AreaTable.db2", 0, 2, AreaStore);
                        LoadLegionAreaGroup(AreaGroupStore);
                        Load("BattlemasterList.db2", 0, 1, BattlegroundStore);
                        Load("CurrencyTypes.db2", 0, 1, CurrencyTypeStore);
                        Load("DungeonEncounter.db2", 6, 0, DungeonEncounterStore);
                        Load("ItemSparse.db2", 0, 2, ItemStore);
                        Load("ItemExtendedCost.db2", row =>
                        {
                            var id = row.GetUInt(0);
                            StringBuilder desc = new StringBuilder();
                            for (int i = 0; i < 5; ++i)
                            {
                                var count = row.GetUInt(2, i);
                                var currency = row.GetUInt(5, i);
                                var item = row.GetUInt(1, i);
                                var itemsCount = row.GetUShort(3, i);

                                if (currency != 0 && count != 0)
                                {
                                    if (CurrencyTypeStore.TryGetValue(currency, out var currencyName))
                                        desc.Append($"{count} x {currencyName}, ");
                                    else
                                        desc.Append($"{count} x Currency {currency}, ");
                                }
                                if (itemsCount != 0 && item != 0)
                                {
                                    if (!ItemStore.TryGetValue(item, out var itemName))
                                        itemName = "item " + item;
                                    
                                    if (itemsCount == 1)
                                        desc.Append($"{itemName}, ");
                                    else
                                        desc.Append($"{itemsCount} x {itemName}, ");
                                }
                            }
                            var arenaRating = row.GetUShort(4);
                            if (arenaRating != 0)
                            {
                                desc.Append($"min arena rating {arenaRating}");
                            }
                            ExtendedCostStore.Add(id, desc.ToString());
                        });
                        Load("ItemSet.db2", 0, 1, ItemSetStore);
                        Load("LFGDungeons.db2", 0, 1, LFGDungeonStore);
                        Load("chrRaces.db2", 30, 2, RaceStore);
                        Load("achievement.db2", row =>  AchievementStore.Add(row.GetInt(12), row.GetString(0)));
                        Load("spell.db2", row => SpellStore.Add(row.Key, row.GetString(1)));
                        Load("chrClasses.db2", 19, 1, ClassStore);
                        Load("Emotes.db2", 0, 2, EmoteStore);
                        Load("EmotesText.db2", 0, 1, TextEmoteStore);
                        Load("HolidayNames.db2", 0, 1, HolidayNamesStore);
                        Load("Holidays.db2", row =>
                        {
                            var id = row.GetUInt(0);
                            var nameId = row.GetUInt(9);
                            if (HolidayNamesStore.TryGetValue(nameId, out var name))
                                HolidaysStore[id] = name;
                            else
                                HolidaysStore[id] = "Holiday " + id;
                        });
                        Load("Languages.db2", 1, 0, LanguageStore);
                        Load("Map.db2", 0, 1, MapDirectoryStore);
                        Load("Map.db2", 0, 2, MapStore);
                        Load("MailTemplate.DB2", row =>
                        {
                            var body = row.GetString(1);
                            var name = body.TrimToLength(50);
                            MailTemplateStore.Add(row.GetUInt(0), name.Replace("\n", ""));
                        });
                        Load("Faction.db2", 3, 1, FactionStore);
                        Load("FactionTemplate.db2", row => FactionTemplateStore.Add(row.GetInt(0), row.GetUShort(1)));
                        // Load("Phase.db2", 1, 0, PhaseStore); // no names in legion :(
                        Load("SoundKitName.db2", 0, 1, SoundStore);
                        Load("SpellFocusObject.db2", 0, 1, SpellFocusObjectStore);
                        Load("QuestInfo.db2", 0, 1, QuestInfoStore);
                        Load("QuestSort.db2", 0, 1, QuestSortStore);
                        Load("CharTitles.db2", 0, 1, CharTitleStore);
                        Load("SkillLine.db2", 0, 1, SkillStore);
                        Load("CreatureDisplayInfo.db2", row => CreatureDisplayInfoStore.Add(row.GetInt(0), row.GetUShort(2)));
                        LoadAndRegister("SpellCastTimes.db2", "SpellCastTimeParameter", 0, row => GetCastTimeDescription(row.GetInt(1), row.GetInt(3), row.GetInt(2)));
                        LoadAndRegister("SpellDuration.db2", "SpellDurationParameter", 0, row => GetDurationTimeDescription(row.GetInt(1), row.GetInt(3), row.GetInt(2)));
                        LoadAndRegister("SpellRange.db2", "SpellRangeParameter", 0, 1);
                        LoadAndRegister("SpellRadius.db2", "SpellRadiusParameter", 0, row => GetRadiusDescription(row.GetFloat(1), row.GetFloat(2), row.GetFloat(4)));
                        Load("SpellItemEnchantment.db2", 0, 1, SpellItemEnchantmentStore);
                        Load("TaxiNodes.db2",  0, 1, TaxiNodeStore);
                        Load("TaxiPath.db2",  row => TaxiPathsStore.Add(row.GetInt(2), (row.GetUShort(0), row.GetUShort(1))));
                        break;
                    }
                    case DBCVersions.SHADOWLANDS_41079:
                    {
                        max = 19;
                        Load("AreaTrigger.db2", string.Empty, AreaTriggerStore);
                        Load("SpellName.db2", "Name_lang", SpellStore);
                        Load("Achievement.db2", "Title_lang", AchievementStore);
                        Load("AreaTable.db2", "AreaName_lang", AreaStore);
                        Load("ChrClasses.db2", "Name_lang", ClassStore);
                        Load("ChrRaces.db2", "Name_lang", RaceStore);
                        LoadDB2("Emotes.db2", row =>
                        {
                            var proc = row.FieldAs<uint>("EmoteSpecProc");
                            var name = row.FieldAs<string>("EmoteSlashCommand");
                            if (proc == 0)
                                EmoteOneShotStore.Add(row.ID, name);
                            else if (proc == 2)
                                EmoteStateStore.Add(row.ID, name);
                            EmoteStore.Add(row.ID, name);
                        });
                        Load("EmotesText.db2", "Name", TextEmoteStore);
                        Load("ItemSparse.db2", "Display_lang", ItemStore);
                        Load("Languages.db2", "Name_lang", LanguageStore);
                        Load("Map.db2", "Directory", MapDirectoryStore);
                        Load("Map.db2", "MapName_lang", MapStore);
                        Load("Faction.db2", "Name_lang", FactionStore);
                        Load("FactionTemplate.db2", "Faction", FactionTemplateStore);
                        Load("SceneScriptPackage.db2", "Name", SceneStore);
                        Load("SpellFocusObject.db2", "Name_lang", SpellFocusObjectStore);
                        Load("QuestInfo.db2", "InfoName_lang", QuestInfoStore);
                        Load("CharTitles.db2", "Name_lang", CharTitleStore);
                        Load("QuestSort.db2", "SortName_lang", QuestSortStore);
                        Load("TaxiNodes.db2",  "Name_lang", TaxiNodeStore);
                        LoadDB2("TaxiPath.db2",  row => TaxiPathsStore.Add(row.ID, (row.Field<int>("FromTaxiNode"), row.Field<int>("ToTaxiNode"))));
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

            private void LoadLegionAreaGroup(Dictionary<long, string> areaGroupStore)
            {
                Dictionary<ushort, List<ushort>> areaGroupToArea = new Dictionary<ushort, List<ushort>>();
                Load("AreaGroupMember.db2", row =>
                {
                    var areaId = row.GetUShort(1);
                    var areaGroup = row.GetUShort(2);
                    if (!areaGroupToArea.TryGetValue(areaGroup, out var list))
                        list = areaGroupToArea[areaGroup] = new();
                    list.Add(areaId);
                });
                foreach (var (group, list) in areaGroupToArea)
                    areaGroupStore.Add(group, BuildAreaGroupName(list));
            }

            private string GetRadiusDescription(float @base, float perLevel, float max)
            {
                if (perLevel == 0)
                    return @base + " yd";
                if (max == 0)
                    return $"{@base} yd + {perLevel} yd/level";
                return $"min({max} yd, {@base} yd + {perLevel} yd/level)";
            }

            private string GetCastTimeDescription(int @base, int perLevel, int min)
            {
                if (@base == 0 && perLevel == 0 && min == 0)
                    return "Instant";
                if (perLevel == 0)
                    return @base.ToPrettyDuration();
                if (min == 0)
                    return $"{@base.ToPrettyDuration()} + {perLevel.ToPrettyDuration()}/level";
                return $"max({min.ToPrettyDuration()}, {@base.ToPrettyDuration()} + {perLevel.ToPrettyDuration()}/level)";
            }

            private string GetDurationTimeDescription(int @base, int perLevel, int max)
            {
                if (@base == -1)
                    return "Infinite";
                if (perLevel == 0)
                    return @base.ToPrettyDuration();
                if (max == 0)
                    return $"{@base.ToPrettyDuration()} + {perLevel.ToPrettyDuration()}/level";
                return $"min({max.ToPrettyDuration()}, {@base.ToPrettyDuration()} + {perLevel.ToPrettyDuration()}/level)";
            }
            
            private string BuildAreaGroupName(IReadOnlyList<ushort> areas)
            {
                if (areas.Count == 1)
                {
                    if (AreaStore.TryGetValue(areas[0], out var name))
                        return name;
                    return "Area " + areas[0];
                }
                
                return string.Join(", ", areas.Select(area => AreaStore.TryGetValue(area, out var name) ? name : "Area " + area));
            }
            
            private string BuildAreaGroupName(IDbcIterator row, int start, int count)
            {
                for (int i = start; i < start + count; ++i)
                {
                    var id = row.GetUInt(i);
                    if (id == 0)
                    {
                        count = i - start;
                        break;
                    }
                }

                if (count == 1)
                {
                    if (AreaStore.TryGetValue(row.GetUInt(start), out var name))
                        return name;
                    return "Area " + row.GetUInt(start);
                }
                
                StringBuilder sb = new();
                for (int i = start; i < start + count; ++i)
                {
                    if (AreaStore.TryGetValue(row.GetUInt(start), out var name))
                        sb.Append(name);
                    else
                        sb.Append("Area " + row.GetUInt(i));
                    
                    if (i != start + count - 1)
                        sb.Append(", ");
                }

                return sb.ToString();
            }

            private string GenerateCostDescription(int honor, int arenaPoints, int item)
            {
                if (honor != 0 && arenaPoints == 0 && item == 0)
                    return honor + " honor";
                if (honor == 0 && arenaPoints != 0 && item == 0)
                    return arenaPoints + " arena points";
                if (honor == 0 && arenaPoints == 0 && item != 0)
                    return item + " item";
                if (item == 0)
                    return honor + " honor, " + arenaPoints + " arena points";
                return honor + " honor, " + arenaPoints + " arena points, " + item + " item";
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
        public uint GetSpellEffectTriggerSpell(uint spellId, int index) => spellServiceImpl.GetSpellEffectTriggerSpell(spellId, index);
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
        public DbcParameter()
        {
            Items = new();
        }
        
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

    public class TaxiPathParameter : DbcParameter
    {
        public TaxiPathParameter(Dictionary<long, (int, int)> taxiPathsStore, Dictionary<long, string> taxiNodes)
        {
            foreach (var path in taxiPathsStore)
            {
                var from = taxiNodes.TryGetValue(path.Value.Item1, out var fromName) ? fromName : "unknown";
                var to = taxiNodes.TryGetValue(path.Value.Item2, out var toName) ? toName : "unknown";
                Items.Add(path.Key, new SelectOption($"{from} -> {to}"));
            }
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
            if (alliance != CharacterRaces.None)
                Items.Add((long)alliance, new SelectOption("Alliance"));
            
            if (horde != CharacterRaces.None)
                Items.Add((long)horde, new SelectOption("Horde"));
            
            foreach (CharacterRaces race in Enum.GetValues<CharacterRaces>())
            {
                if (IsPowerOfTwo((ulong)race) && allowableRaces.HasFlagFast(race))
                {
                    Items.Add((long)race, new SelectOption(race.ToString().ToTitleCase()));
                }
            }
        }
    }
    
}