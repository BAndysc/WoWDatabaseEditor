using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Collections;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Database.Counters;
using WDE.Common.DBC;
using WDE.Common.DBC.Structs;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.TableData;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DbcStore.FastReader;
using WDE.DbcStore.Loaders;
using WDE.DbcStore.Providers;
using WDE.DbcStore.Spells;
using WDE.DbcStore.Spells.Cataclysm;
using WDE.DbcStore.Spells.Legion;
using WDE.DbcStore.Spells.Wrath;
using WDE.DbcStore.Structs;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.DbcStore
{
    public enum DBCVersions
    {
        TBC_8606 = 8606,
        WOTLK_12340 = 12340,
        CATA_15595 = 15595,
        MOP_18414 = 18414,
        LEGION_26972 = 26972,
        SHADOWLANDS_41079 = 41079,
        DRAGONFLIGHT_49444 = 49444
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
    public class DbcStore : IDbcStore, IDbcSpellService, IMapAreaStore, IFactionTemplateStore
    {
        private readonly IDbcSettingsProvider dbcSettingsProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IEventAggregator eventAggregator;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly ITabularDataPicker dataPicker;
        private readonly IWindowManager windowManager;
        private readonly IDatabaseRowsCountProvider databaseRowsCountProvider;
        private readonly NullSpellService nullSpellService;
        private readonly IParameterFactory parameterFactory;
        private readonly ITaskRunner taskRunner;
        private readonly DBCD.DBCD dbcd;
        private readonly IEnumerable<IDbcLoader> dbcLoaders;
        private readonly IEnumerable<IDbcSpellLoader> spellLoaders;

        internal DbcStore(IParameterFactory parameterFactory, 
            ITaskRunner taskRunner,
            IDbcSettingsProvider settingsProvider,
            IMessageBoxService messageBoxService,
            IEventAggregator eventAggregator,
            ICurrentCoreVersion currentCoreVersion,
            ITabularDataPicker dataPicker,
            IWindowManager windowManager,
            IDatabaseRowsCountProvider databaseRowsCountProvider,
            NullSpellService nullSpellService,
            DBCD.DBCD dbcd,
            IEnumerable<IDbcLoader> dbcLoaders,
            IEnumerable<IDbcSpellLoader> spellLoaders)
        {
            this.parameterFactory = parameterFactory;
            this.taskRunner = taskRunner;
            dbcSettingsProvider = settingsProvider;
            this.messageBoxService = messageBoxService;
            this.eventAggregator = eventAggregator;
            this.currentCoreVersion = currentCoreVersion;
            this.dataPicker = dataPicker;
            this.windowManager = windowManager;
            this.databaseRowsCountProvider = databaseRowsCountProvider;
            this.nullSpellService = nullSpellService;
            this.dbcd = dbcd;
            this.dbcLoaders = dbcLoaders;
            this.spellLoaders = spellLoaders;

            spellServiceImpl = nullSpellService;
            Load();
        }
        
        public bool IsConfigured { get; private set; }
        public Dictionary<long, string> AreaTriggerStore { get; internal set; } = new();
        public Dictionary<long, string> PhaseStore { get; internal set; } = new();
        public Dictionary<long, string> MapStore { get; internal set; } = new();
        public Dictionary<long, string> SoundStore { get; internal set; } = new();
        public Dictionary<long, string> ClassStore { get; internal set; } = new();
        public Dictionary<long, string> RaceStore { get; internal set; } = new();
        public Dictionary<long, string> EmoteStore { get; internal set; } = new();
        public Dictionary<long, string> MapDirectoryStore { get; internal set; } = new();
        public Dictionary<long, string> SceneStore { get; internal set; } = new();
        public Dictionary<long, string> ScenarioStepStore { get; internal set; } = new();
        public Dictionary<long, long> BattlePetSpeciesIdStore { get; internal set; } = new();
        public Dictionary<long, Dictionary<long, long>> ScenarioToStepStore { get; internal set; } = new();

        public IReadOnlyList<IArea> Areas { get; internal set; } = Array.Empty<IArea>();
        public Dictionary<uint, IArea> AreaById { get; internal set; } = new();

        public IReadOnlyList<IMap> Maps { get; internal set; } = Array.Empty<IMap>();
        public Dictionary<uint, IMap> MapById { get; internal set; } = new();
        
        public IReadOnlyList<FactionTemplate> FactionTemplates { get; internal set; } = Array.Empty<FactionTemplate>();
        public Dictionary<uint, FactionTemplate> FactionTemplateById { get; internal set; } = new();
        
        public IReadOnlyList<Faction> Factions { get; internal set; } = Array.Empty<Faction>();
        public Dictionary<ushort, Faction> FactionsById { get; internal set; } = new();

        public IReadOnlyList<ICharShipmentContainer> CharShipmentContainers { get; internal set; } = Array.Empty<ICharShipmentContainer>();
        public Dictionary<ushort, ICharShipmentContainer> CharShipmentContainerById { get; internal set; } = new();

        public IArea? GetAreaById(uint id) => AreaById.TryGetValue(id, out var area) ? area : null;
        public IMap? GetMapById(uint id) => MapById.TryGetValue(id, out var map) ? map : null;
        public FactionTemplate? GetFactionTemplate(uint templateId) => FactionTemplateById.TryGetValue(templateId, out var faction) ? faction : null;
        public Faction? GetFaction(ushort factionId) => FactionsById.TryGetValue(factionId, out var faction) ? faction : null;
        
        internal void Load()
        {            
            parameterFactory.Register("RaceMaskParameter", new RaceMaskParameter(currentCoreVersion.Current.GameVersionFeatures.AllRaces), QuickAccessMode.Limited);

            if (dbcSettingsProvider.GetSettings().SkipLoading ||
                !Directory.Exists(dbcSettingsProvider.GetSettings().Path))
            {
                // we create a new fake task, that will not be started, but finalized so that (empty) parameters are registered
                var fakeTask = new DbcLoadTask(parameterFactory, dataPicker, dbcSettingsProvider, this);
                fakeTask.FinishMainThread();
                return;
            }

            IsConfigured = true;
            taskRunner.ScheduleTask(new DbcLoadTask(parameterFactory, dataPicker, dbcSettingsProvider, this));
        }

        private class DbcLoadTask : IThreadedTask
        {
            private readonly IDbcSettingsProvider dbcSettingsProvider;
            private readonly IParameterFactory parameterFactory;
            private readonly ITabularDataPicker dataPicker;
            private readonly DbcStore store;
            private readonly DBDProvider dbdProvider = null!;
            private readonly DBCProvider dbcProvider = null!;
            
            public string Name => "DBC Loading";
            public bool WaitForOtherTasks => false;
            private IDbcSpellLoader spellLoader;

            private readonly DbcData data = new();
            
            public DbcLoadTask(IParameterFactory parameterFactory,
                ITabularDataPicker dataPicker,
                IDbcSettingsProvider settingsProvider, 
                DbcStore store)
            {
                this.parameterFactory = parameterFactory;
                this.dataPicker = dataPicker;
                this.store = store;
                dbcSettingsProvider = settingsProvider;
                
                spellLoader = store.spellLoaders.FirstOrDefault(x => x.Version == settingsProvider.GetSettings().DBCVersion) ?? new NullSpellService();
            }

            public void FinishMainThread()
            {
                store.AreaTriggerStore = data.AreaTriggerStore;
                store.PhaseStore = data.PhaseStore;
                store.MapStore = data.MapStore;
                store.SoundStore = data.SoundStore;
                store.ClassStore = data.ClassStore;
                store.RaceStore = data.RaceStore;
                store.EmoteStore = data.EmoteStore;
                store.MapDirectoryStore = data.MapDirectoryStore;
                store.SceneStore = data.SceneStore;
                store.ScenarioStepStore = data.ScenarioStepStore;
                store.ScenarioToStepStore = data.ScenarioToStepStore;
                store.BattlePetSpeciesIdStore = data.BattlePetSpeciesIdStore;
                store.Areas = data.Areas;
                store.AreaById = data.Areas.ToDictionary(a => a.Id, a => (IArea)a);
                store.Maps = data.Maps;
                store.MapById = data.Maps.ToDictionary(a => a.Id, a => (IMap)a);
                store.FactionTemplates = data.FactionTemplates;
                store.FactionTemplateById = data.FactionTemplates.ToDictionary(a => a.TemplateId, a => (FactionTemplate)a);
                store.Factions = data.Factions;
                store.FactionsById = data.Factions.ToDictionary(a => a.FactionId, a => a);

                foreach (var (parameterName, options) in data.parametersToRegister)
                {
                    parameterFactory.Register(parameterName, new Parameter()
                    {
                        Items = options
                    });
                }
                data.parametersToRegister.Clear();
                
                parameterFactory.Register("AchievementParameter", new DbcParameter(data.AchievementStore), QuickAccessMode.Full);
                parameterFactory.Register("MovieParameter", new DbcParameter(data.MovieStore), QuickAccessMode.Limited);
                parameterFactory.Register("FactionParameter", new DbcParameter(data.FactionStore), QuickAccessMode.Limited);
                parameterFactory.Register("FactionTemplateParameter", new FactionTemplateParameter(data.FactionStore, data.FactionTemplateStore), QuickAccessMode.Limited);
                parameterFactory.Register("DbcSpellParameter", new DbcParameter(data.SpellStore));
                parameterFactory.Register("CurrencyTypeParameter", new DbcParameter(data.CurrencyTypeStore));
                parameterFactory.Register("ItemDbcParameter", new DbcParameter(data.ItemStore));
                parameterFactory.Register("EmoteParameter", new DbcParameter(data.EmoteStore), QuickAccessMode.Full);
                parameterFactory.Register("EmoteOneShotParameter", new DbcParameter(data.EmoteOneShotStore));
                parameterFactory.Register("EmoteStateParameter", new DbcParameter(data.EmoteStateStore));
                parameterFactory.Register("TextEmoteParameter", new DbcParameter(data.TextEmoteStore), QuickAccessMode.Limited);
                parameterFactory.Register("ClassParameter", new DbcParameter(data.ClassStore), QuickAccessMode.Limited);
                parameterFactory.Register("ClassMaskParameter", new DbcMaskParameter(data.ClassStore, -1));
                parameterFactory.Register("RaceParameter", new DbcParameter(data.RaceStore));
                parameterFactory.Register("SkillParameter", new DbcParameter(data.SkillStore), QuickAccessMode.Limited);
                parameterFactory.Register("SoundParameter", new DbcParameter(data.SoundStore), QuickAccessMode.Limited);
                parameterFactory.Register("MapParameter", new DbcParameter(data.MapStore), QuickAccessMode.Limited);
                parameterFactory.Register("DbcPhaseParameter", new DbcParameter(data.PhaseStore), QuickAccessMode.Limited);
                parameterFactory.Register("SpellFocusObjectParameter", new DbcParameter(data.SpellFocusObjectStore), QuickAccessMode.Limited);
                parameterFactory.Register("QuestInfoParameter", new DbcParameter(data.QuestInfoStore));
                parameterFactory.Register("CharTitleParameter", new DbcParameter(data.CharTitleStore));
                parameterFactory.Register("ExtendedCostParameter", new DbcParameter(data.ExtendedCostStore));
                parameterFactory.Register("CreatureModelDataParameter", new CreatureModelParameter(data.CreatureModelDataStore, data.CreatureDisplayInfoStore));
                parameterFactory.Register("GameObjectDisplayInfoParameter", new DbcFileParameter(data.GameObjectDisplayInfoStore));
                parameterFactory.Register("LanguageParameter", new LanguageParameter(data.LanguageStore), QuickAccessMode.Limited);
                parameterFactory.Register("AreaTriggerParameter", new DbcParameter(data.AreaTriggerStore));
                parameterFactory.Register("ZoneOrQuestSortParameter", new ZoneOrQuestSortParameter(data.AreaStore, data.QuestSortStore));
                parameterFactory.Register("TaxiPathParameter", new TaxiPathParameter(data.TaxiPathsStore, data.TaxiNodeStore));
                parameterFactory.Register("TaxiNodeParameter", new DbcParameter(data.TaxiNodeStore));
                parameterFactory.Register("SpellItemEnchantmentParameter", new DbcParameter(data.SpellItemEnchantmentStore));
                parameterFactory.Register("AreaGroupParameter", new DbcParameter(data.AreaGroupStore));
                parameterFactory.Register("ItemDisplayInfoParameter", new DbcParameter(data.ItemDisplayInfoStore));
                parameterFactory.Register("MailTemplateParameter", new DbcParameter(data.MailTemplateStore));
                parameterFactory.Register("LFGDungeonParameter", new DbcParameter(data.LFGDungeonStore));
                parameterFactory.Register("ItemSetParameter", new DbcParameter(data.ItemSetStore));
                parameterFactory.Register("DungeonEncounterParameter", new DbcParameter(data.DungeonEncounterStore));
                parameterFactory.Register("HolidaysParameter", new DbcParameter(data.HolidaysStore));
                parameterFactory.Register("WorldSafeLocParameter", new DbcParameter(data.WorldSafeLocsStore));
                parameterFactory.Register("BattlegroundParameter", new DbcParameter(data.BattlegroundStore));
                parameterFactory.Register("AchievementCriteriaParameter", new DbcParameter(data.AchievementCriteriaStore));
                parameterFactory.Register("ItemVisualParameter", new DbcParameter(data.ItemDbcStore));
                parameterFactory.Register("SceneScriptParameter", new DbcParameter(data.SceneStore));
                parameterFactory.Register("ScenarioParameter", new DbcParameter(data.ScenarioStore));
                parameterFactory.Register("ScenarioStepParameter", new DbcParameter(data.ScenarioStepStore));
                parameterFactory.Register("BattlePetAbilityParameter", new DbcParameter(data.BattlePetAbilityStore));
                parameterFactory.Register("CharSpecializationParameter", new DbcParameter(data.CharSpecializationStore));
                parameterFactory.Register("GarrisonClassSpecParameter", new DbcParameter(data.GarrisonClassSpecStore));
                parameterFactory.Register("GarrisonBuildingParameter", new DbcParameter(data.GarrisonBuildingStore));
                parameterFactory.Register("GarrisonTalentParameter", new DbcParameter(data.GarrisonTalentStore));
                parameterFactory.Register("GarrisonMissionParameter", new DbcParameter(data.GarrisonMissionStore));
                parameterFactory.Register("DifficultyParameter", new DbcParameter(data.DifficultyStore));
                parameterFactory.Register("LockTypeParameter", new DbcParameter(data.LockTypeStore));
                parameterFactory.Register("AdventureJournalParameter", new DbcParameter(data.AdventureJournalStore));
                parameterFactory.Register("VignetteParameter", new DbcParameterWowTools(data.VignetteStore, "vignette", store.currentCoreVersion, store.windowManager));
                parameterFactory.Register("VehicleParameter", new WoWToolsParameter("vehicle", store.currentCoreVersion, store.windowManager));
                parameterFactory.Register("LockParameter", new WoWToolsParameter("lock", store.currentCoreVersion, store.windowManager));
                parameterFactory.Register("WorldMapAreaParameter", new DbcParameter(data.WorldMapAreaStore));
                parameterFactory.Register("ConversationLineParameter", new DbcParameter(data.ConversationLineStore));

                void RegisterCharShipmentContainerParameter(string key, TabularDataAsyncColumn<uint>? counterColumn = null)
                {
                    parameterFactory.Register(key,
                        new DbcParameterWithPicker<ICharShipmentContainer>(dataPicker, data.CharShipmentContainerStore, "shipment container", container => container.Id,
                        () => store.CharShipmentContainers,
                        (container, text) => container.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
                            container.Id.Contains(text) ||
                            container.Description.Contains(text, StringComparison.InvariantCultureIgnoreCase),
                        (container, text) => container.Id.Is(text),
                        (text) =>
                        {
                            if (!uint.TryParse(text, out var id))
                                return null;
                            return new CharShipmentContainerEntry() { Id = id, Name = "Pick non existing" };
                        },
                        new TabularDataColumn(nameof(ICharShipmentContainer.Id), "Entry", 60),
                        new TabularDataColumn(nameof(ICharShipmentContainer.Name), "Name", 160),
                        new TabularDataColumn(nameof(ICharShipmentContainer.Description), "Description", 200),
                        counterColumn), QuickAccessMode.Full);
                }
                RegisterCharShipmentContainerParameter("CharShipmentContainerParameter");

                void RegisterZoneAreParameter(string key, TabularDataAsyncColumn<uint>? counterColumn = null)
                {
                    parameterFactory.Register(key, 
                        new DbcParameterWithPicker<IArea>(dataPicker, data.AreaStore, "zone or area", area => area.Id,
                            () => store.Areas,
                            (area, text) => area.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase) || area.Id.Contains(text) || 
                                area.MapId.Contains(text) || (area.Map != null && area.Map.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase)) ||
                                area.ParentAreaId.Contains(text) || (area.ParentArea != null && area.ParentArea.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase)),
                            (area, text) => area.Id.Is(text) || area.MapId.Is(text) || area.ParentAreaId.Is(text),
                            (text) =>
                            {
                                if (!uint.TryParse(text, out var id))
                                    return null;
                                return new AreaEntry() { Id = id, Name = "Pick non existing"};
                            },
                            new TabularDataColumn(nameof(IArea.Id), "Entry", 60), 
                            new TabularDataColumn(nameof(IArea.Name), "Name", 160), 
                            new TabularDataColumn(nameof(IArea.ParentArea) + "." + nameof(IArea.Name), "Parent", 160), 
                            new TabularDataColumn(nameof(IArea.Map) + "." + nameof(IMap.Id), "Map", 40),
                            new TabularDataColumn(nameof(IArea.Map) + "." + nameof(IMap.Name), "Map name", 120),
                            counterColumn), QuickAccessMode.Limited);
                }
                RegisterZoneAreParameter("ZoneAreaParameter");
                RegisterZoneAreParameter("ZoneArea(spell_area)Parameter", 
                    new TabularDataAsyncColumn<uint>(nameof(IArea.Id), "Count", async (zoneId, token) =>
                {
                    if (zoneId == 0)
                        return "0";
                    return (await store.databaseRowsCountProvider.GetRowsCountByPrimaryKey("spell_area", zoneId, token)).ToString();
                }, 50));
                RegisterZoneAreParameter("ZoneArea(phase_definitions)Parameter", 
                    new TabularDataAsyncColumn<uint>(nameof(IArea.Id), "Count", async (zoneId, token) =>
                {
                    if (zoneId == 0)
                        return "0";
                    return (await store.databaseRowsCountProvider.GetRowsCountByPrimaryKey("phase_definitions", zoneId, token)).ToString();
                }, 50));

                parameterFactory.RegisterDepending("BattlePetSpeciesParameter", "CreatureParameter", (creature) => new BattlePetSpeciesParameter(store, parameterFactory, creature));

                store.spellServiceImpl = spellLoader;

                store.spellServiceImpl.Changed += _ => store.InvokeChangedSpells();
                store.InvokeChangedSpells();
                
                store.eventAggregator.GetEvent<DbcLoadedEvent>().Publish(store);
            }
            
            public void Run(ITaskProgress progress)
            {
                var loader = store.dbcLoaders.FirstOrDefault(x => x.Version == dbcSettingsProvider.GetSettings().DBCVersion);

                if (loader == null)
                    throw new Exception("Couldn't find loader for DBC version. If you are a developer, there is some bug in new code. If you are a user, please report this issue.");

                spellLoader.Load(dbcSettingsProvider.GetSettings().Path);
                
                loader.LoadDbc(data, progress);
                
                switch (dbcSettingsProvider.GetSettings().DBCLocale)
                {
                    case DBCLocales.LANG_enUS:
                        Validate(data.SpellStore, 1, "Word of Recall (OLD)");
                        break;
                    case DBCLocales.LANG_frFR:
                        Validate(data.SpellStore, 1, "Mot de rappel (OLD)");
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

        private void InvokeChangedSpells()
        {
            Changed?.Invoke(this);
        }

        private IDbcSpellService spellServiceImpl;
        public bool Exists(uint spellId) => spellServiceImpl.Exists(spellId);
        public int SpellCount => spellServiceImpl.SpellCount;
        public uint GetSpellId(int index) => spellServiceImpl.GetSpellId(index);
        public T GetAttributes<T>(uint spellId) where T : unmanaged, Enum => spellServiceImpl.GetAttributes<T>(spellId);
        public uint? GetSkillLine(uint spellId) => spellServiceImpl.GetSkillLine(spellId);
        public uint? GetSpellFocus(uint spellId) => spellServiceImpl.GetSpellFocus(spellId);
        public TimeSpan? GetSpellCastingTime(uint spellId) => spellServiceImpl.GetSpellCastingTime(spellId);
        public TimeSpan? GetSpellDuration(uint spellId) => spellServiceImpl.GetSpellDuration(spellId);
        public TimeSpan? GetSpellCategoryRecoveryTime(uint spellId) => spellServiceImpl.GetSpellCategoryRecoveryTime(spellId);
        public string GetName(uint spellId) => spellServiceImpl.GetName(spellId);
        public event Action<ISpellService>? Changed;
        public string? GetDescription(uint spellId) => spellServiceImpl.GetDescription(spellId);
        public int GetSpellEffectsCount(uint spellId) => spellServiceImpl.GetSpellEffectsCount(spellId);
        public SpellAuraType GetSpellAuraType(uint spellId, int effectIndex) => spellServiceImpl.GetSpellAuraType(spellId, effectIndex);
        public SpellEffectType GetSpellEffectType(uint spellId, int index) => spellServiceImpl.GetSpellEffectType(spellId, index);
        public SpellTargetFlags GetSpellTargetFlags(uint spellId) => spellServiceImpl.GetSpellTargetFlags(spellId);
        public (SpellTarget, SpellTarget) GetSpellEffectTargetType(uint spellId, int index) => spellServiceImpl.GetSpellEffectTargetType(spellId, index);
        public uint GetSpellEffectMiscValueA(uint spellId, int index) => spellServiceImpl.GetSpellEffectMiscValueA(spellId, index);
        public uint GetSpellEffectTriggerSpell(uint spellId, int index) => spellServiceImpl.GetSpellEffectTriggerSpell(spellId, index);
    }

    internal class FactionTemplateParameter : ParameterNumbered
    {
        public FactionTemplateParameter(Dictionary<long, string> factionStore, Dictionary<long, long> factionTemplateStore)
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
            int indexOf = Math.Max(s.LastIndexOf('\\'), s.LastIndexOf('/'));
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

    public class WoWToolsParameter : Parameter, ICustomPickerParameter<long>
    {
        private readonly string dbcName;
        private readonly IWindowManager windowManager;
        private readonly string buildString;

        public override bool HasItems => true;
        public bool AllowUnknownItems => true;

        public WoWToolsParameter(string dbcName, 
            ICurrentCoreVersion currentCoreVersion, 
            IWindowManager windowManager)
        {
            this.dbcName = dbcName;
            this.windowManager = windowManager;
            var version = currentCoreVersion.Current.Version;
            var build = version.Build == 18414 ? 18273 : version.Build;
            buildString = $"{version.Major}.{version.Minor}.{version.Patch}.{build}";
        }
        
        public Task<(long, bool)> PickValue(long value)
        {
            windowManager.OpenUrl($"https://wow.tools/dbc/?dbc={dbcName}&build={buildString}#page=1&colFilter[0]=exact%3A{value}");
            return Task.FromResult((0L, false));
        }
    }

    public class DbcParameterWowTools : DbcParameter, ICustomPickerParameter<long>
    {
        private readonly string dbcName;
        private readonly IWindowManager windowManager;
        private readonly string buildString;
        
        public override bool HasItems => true;
        
        public DbcParameterWowTools(Dictionary<long, string> storage, 
            string dbcName, 
            ICurrentCoreVersion currentCoreVersion, 
            IWindowManager windowManager) : base(storage)
        {
            this.dbcName = dbcName;
            this.windowManager = windowManager;
            var version = currentCoreVersion.Current.Version;
            var build = version.Build == 18414 ? 18273 : version.Build;
            buildString = $"{version.Major}.{version.Minor}.{version.Patch}.{build}";
        }
        
        public Task<(long, bool)> PickValue(long value)
        {
            windowManager.OpenUrl($"https://wow.tools/dbc/?dbc={dbcName}&build={buildString}#page=1&colFilter[0]=exact%3A{value}");
            return Task.FromResult((0L, false));
        }
    }
    
    public class DbcParameterWithPicker<T> : DbcParameter, ICustomPickerParameter<long> where T : class
    {
        private readonly ITabularDataPicker dataPicker;
        private readonly string dbc;
        private readonly Func<T, long> getId;
        private readonly Func<IReadOnlyList<T>> getListOf;
        private readonly Func<T, string, bool> filter;
        private readonly Func<T, string, bool> isExactMatch;
        private readonly Func<string, T?> exactMatchCreator;
        private readonly ITabularDataColumn[] columns;

        public bool NeverUseComboBoxPicker => true;

        public DbcParameterWithPicker(ITabularDataPicker dataPicker,
            Dictionary<long, string> storage,
            string dbc,
            Func<T, long> getId,
            Func<IReadOnlyList<T>> getListOf,
            Func<T, string, bool> filter,
            Func<T, string, bool> isExactMatch,
            Func<string, T?> exactMatchCreator,
            params ITabularDataColumn?[] columns) : base(storage)
        {
            this.dataPicker = dataPicker;
            this.dbc = dbc;
            this.getId = getId;
            this.getListOf = getListOf;
            this.filter = filter;
            this.isExactMatch = isExactMatch;
            this.exactMatchCreator = exactMatchCreator;
            this.columns = columns.Where(c => c != null).Cast<ITabularDataColumn>().ToArray();
        }
        
        public async Task<(long, bool)> PickValue(long value)
        {
            var result = await dataPicker.PickRow(BuildTable(), defaultSearchText: value > 0 ? value.ToString() : null);

            if (result == null)
                return (0, false);
            
            return (getId(result), true);
        }

        public async Task<IReadOnlyCollection<long>> PickMultipleValues()
        {
            var result = await dataPicker.PickRows(BuildTable());
            
            return result == null ? Array.Empty<long>() : result.Select(getId).ToArray();
        }
        
        private ITabularDataArgs<T> BuildTable()
        {
            return new TabularDataBuilder<T>()
                .SetData(getListOf().AsIndexedCollection())
                .SetTitle($"Pick {dbc}")
                .SetFilter(filter)
                .SetExactMatchPredicate(isExactMatch)
                .SetExactMatchCreator(exactMatchCreator)
                .SetColumns(columns)
                .Build();
        }
    }
    
    public class BattlePetSpeciesParameter : ParameterNumbered
    {
        private readonly DbcStore dbcStore;
        private readonly IParameterFactory parameterFactory;
        private readonly IParameter<long> creatures;

        public BattlePetSpeciesParameter(DbcStore dbcStore, IParameterFactory parameterFactory, IParameter<long> creatures)
        {
            this.dbcStore = dbcStore;
            this.parameterFactory = parameterFactory;
            this.creatures = creatures;
            Items = new Dictionary<long, SelectOption>();
            Refresh();

            parameterFactory.OnRegister().SubscribeAction(p =>
            {
                if (p == creatures)
                    Refresh();
            });
        }

        private void Refresh()
        {
            Items!.Clear();
            foreach (var (key, value) in dbcStore.BattlePetSpeciesIdStore)
            {
                if (creatures.Items != null && creatures.Items.TryGetValue(value, out var petName))
                    Items!.Add(key, new SelectOption(petName.Name + " (" + value + ")"));
                else
                    Items!.Add(key, new SelectOption("Creature " + value));
            }
        }
    }

    public class LanguageParameter : DbcParameter
    {
        public LanguageParameter(Dictionary<long, string> storage) : base(storage)
        {
            Items!.Add(0, new SelectOption("Universal"));
        }
    }

    public class ZoneOrQuestSortParameter : DbcParameter
    {
        public ZoneOrQuestSortParameter(Dictionary<long, string> zones, Dictionary<long, string> questSorts) : base(zones)
        {
            foreach (var pair in questSorts)
                Items!.Add(-pair.Key, new SelectOption(pair.Value));
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
                Items!.Add(path.Key, new SelectOption($"{from} -> {to}"));
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