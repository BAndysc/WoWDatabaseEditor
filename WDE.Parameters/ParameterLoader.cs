using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common;
using WDE.Common.Collections;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.TableData;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;
using WDE.Parameters.Parameters;
using WDE.Parameters.Providers;
using WDE.Parameters.QuickAccess;
using WDE.Parameters.ViewModels;

namespace WDE.Parameters
{
    public class ParameterLoader
    {
        private readonly ICachedDatabaseProvider database;
        private readonly IParameterDefinitionProvider parameterDefinitionProvider;
        private readonly IServerIntegration serverIntegration;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly ILoadingEventAggregator loadingEventAggregator;
        private readonly ITabularDataPicker tabularDataPicker;
        private readonly ICreatureEntryOrGuidProviderService creaturePicker;
        private readonly IQuestEntryProviderService questEntryProviderService;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IConversationLineStore conversationLineStore;
        private readonly Lazy<IWindowManager> windowManager;
        private readonly Lazy<IParameterPickerService> parameterPickerService;
        private readonly IQuickAccessRegisteredParameters quickAccessRegisteredParameters;

        private Dictionary<Type, List<IDatabaseObserver>> reloadable = new();
        private List<LateAsyncLoadParameter> asyncDatabaseParameters = new();
        private List<LateLoadParameter> databaseParameters = new();
        internal ParameterLoader(ICachedDatabaseProvider database,
            IParameterDefinitionProvider parameterDefinitionProvider,
            IServerIntegration serverIntegration,
            IItemFromListProvider itemFromListProvider,
            IEventAggregator eventAggregator,
            ILoadingEventAggregator loadingEventAggregator,
            ITabularDataPicker tabularDataPicker,
            ICreatureEntryOrGuidProviderService creaturePicker,
            IQuestEntryProviderService questEntryProviderService,
            ICurrentCoreVersion currentCoreVersion,
            IConversationLineStore conversationLineStore,
            Lazy<IWindowManager> windowManager,
            Lazy<IParameterPickerService> parameterPickerService,
            IQuickAccessRegisteredParameters quickAccessRegisteredParameters)
        {
            this.database = database;
            this.parameterDefinitionProvider = parameterDefinitionProvider;
            this.serverIntegration = serverIntegration;
            this.itemFromListProvider = itemFromListProvider;
            this.eventAggregator = eventAggregator;
            this.loadingEventAggregator = loadingEventAggregator;
            this.tabularDataPicker = tabularDataPicker;
            this.creaturePicker = creaturePicker;
            this.questEntryProviderService = questEntryProviderService;
            this.currentCoreVersion = currentCoreVersion;
            this.conversationLineStore = conversationLineStore;
            this.windowManager = windowManager;
            this.parameterPickerService = parameterPickerService;
            this.quickAccessRegisteredParameters = quickAccessRegisteredParameters;
        }

        public void Load(ParameterFactory factory)
        {
            foreach (var pair in parameterDefinitionProvider.Parameters)
            {
                if (pair.Value.StringValues != null)
                {
                    SwitchStringParameter stringParameter = new SwitchStringParameter(pair.Value.StringValues, pair.Value.Prefix);
                    factory.Register(pair.Key, stringParameter);
                }
                else if (pair.Value.Values != null)
                {
                    Parameter p = pair.Value.IsFlag ? new FlagParameter() : new Parameter();
                    p.Items = pair.Value.Values;
                    p.Prefix = pair.Value.Prefix;
                    factory.Register(pair.Key, p);
                }
                else if (pair.Value.MaskFrom != null)
                {
                    Parameter p = new FlagParameter();
                    var other = factory.Factory(pair.Value.MaskFrom.Value.Name);
                    Debug.Assert(other.Items != null);
                    p.Items = new Dictionary<long, SelectOption>();
                    foreach (var arg in other.Items)
                    {
                        var key = 1L << (int)(arg.Key + pair.Value.MaskFrom.Value.Offset);
                        if (arg.Key == 0 && pair.Value.MaskFrom.Value.Offset < 0)
                            key = 0;
                        p.Items.Add(key, arg.Value);
                    }
                    factory.Register(pair.Key, p);
                }
                
                if (pair.Value.QuickAccess != QuickAccessMode.None)
                    quickAccessRegisteredParameters.Register(pair.Value.QuickAccess, pair.Key, pair.Value.Name);
            }

            factory.Register("InvalidParameter", new InvalidParameter<long>());
            factory.Register("UnusedParameter", UnusedParameter.Instance);
            factory.Register("StringParameter", StringParameter.Instance);
            factory.Register("FloatParameter", new FloatIntParameter(1000));
            factory.Register("DecifloatParameter", new FloatIntParameter(100));
            factory.Register("OneTenthParameter", new FloatIntParameter(10));
            factory.Register("MillisecondsParameter", new MillisecondsParameter());
            factory.Register("GameEventParameter", AddAsyncDatabaseParameter(new GameEventParameter(database)), QuickAccessMode.Limited);
            factory.Register("CreatureParameter", AddAsyncDatabaseParameter(new CreatureParameter(database, creaturePicker, serverIntegration)), QuickAccessMode.Limited);
            factory.Register("Creature(creature_text)Parameter", AddAsyncDatabaseParameter(new CreatureParameter(database, creaturePicker, serverIntegration, "creature_text")), QuickAccessMode.Limited);
            factory.Register("Creature(smart_ai_text)Parameter", AddAsyncDatabaseParameter(new CreatureParameter(database, creaturePicker, serverIntegration, "smart_ai_text")), QuickAccessMode.Limited);
            factory.Register("CreatureGameobjectNameParameter", AddAsyncDatabaseParameter(new CreatureGameobjectNameParameter(database)));
            factory.Register("CreatureGameobjectParameter", AddAsyncDatabaseParameter(new CreatureGameobjectParameter(database)));
            factory.Register("QuestParameter", AddAsyncDatabaseParameter(new QuestParameter(database, questEntryProviderService)), QuickAccessMode.Limited);
            factory.Register("PrevQuestParameter", AddAsyncDatabaseParameter(new PrevQuestParameter(database)));
            factory.Register("GameobjectParameter", AddAsyncDatabaseParameter(new GameobjectParameter(database, serverIntegration, itemFromListProvider)), QuickAccessMode.Limited);
            factory.Register("GossipMenuParameter", AddAsyncDatabaseParameter(new GossipMenuParameter(database)));
            factory.Register("NpcTextParameter", AddAsyncDatabaseParameter(new NpcTextParameter(database)));
            factory.Register("PlayerChoiceParameter", AddAsyncDatabaseParameter(new PlayerChoiceParameter(database)));
            factory.Register("PlayerChoiceResponseParameter", AddAsyncDatabaseParameter(new PlayerChoiceResponseParameter(database)));
            factory.Register("ServersideAreatriggerParameter", AddAsyncDatabaseParameter(new ServersideAreatriggerParameter(database)));
            factory.Register("DatabasePhaseParameter", AddAsyncDatabaseParameter(new DatabasePhaseParameter(database)), QuickAccessMode.Limited);
            factory.Register("ConversationParameter", AddAsyncDatabaseParameter(new ConversationParameter(conversationLineStore, database)));
            factory.RegisterDepending("SceneTemplateParameter", "SceneScriptParameter", sceneScript =>AddAsyncDatabaseParameter(new SceneTemplateParameter(database, sceneScript)));
            factory.Register("BoolParameter", new BoolParameter());
            factory.Register("FlagParameter", new FlagParameter());
            factory.Register("PercentageParameter", new PercentageParameter());
            factory.Register("MoneyParameter", new MoneyParameter());
            factory.Register("MinuteIntervalParameter", new MinuteIntervalParameter());
            factory.Register("UnixTimestampParameter", new UnixTimestampParameter(0, windowManager));
            factory.Register("UnixTimestampSince2000Parameter", new UnixTimestampParameter(946684800, windowManager));
            factory.Register("GameobjectBytes1Parameter", new GameObjectBytes1Parameter());
            
            factory.RegisterCombined("UnitBytes0Parameter", "RaceParameter",  "ClassParameter","GenderParameter", "PowerParameter", 
                (race, @class, gender, power) => currentCoreVersion.Current.Version.Build >= 17359 ? new UnitBytesPostMopParameter(race, @class, gender, power) : new UnitBytesPreMopParameter(race, @class, gender, power));
            factory.RegisterCombined("UnitBytes0PostMopParameter", "RaceParameter",  "ClassParameter","GenderParameter", "PowerParameter", 
                (race, @class, gender, power) => new UnitBytesPostMopParameter(race, @class, gender, power));
            factory.RegisterCombined("UnitBytes0PreMopParameter", "RaceParameter",  "ClassParameter","GenderParameter", "PowerParameter", 
                (race, @class, gender, power) => new UnitBytesPreMopParameter(race, @class, gender, power));
            
            factory.RegisterCombined("UnitBytes1Parameter", "StandStateParameter", "AnimTierParameter", (standState, animTier) => new UnitBytes1Parameter(standState, animTier, windowManager));
            factory.RegisterCombined("UnitBytes2Parameter", "SheathStateParameter",  "UnitPVPStateFlagParameter","UnitBytesPetFlagParameter", "ShapeshiftFormParameter", 
                (sheath, pvp, pet, shapeShift) => new UnitBytes2Parameter(sheath, pvp, pet, shapeShift, windowManager));
            factory.RegisterCombined("PhaseParameter", "DbcPhaseParameter", "DatabasePhaseParameter", (dbc, db) => new PhaseParameter(dbc, db));
            factory.RegisterDepending("MultiPhaseParameter", "PhaseParameter", phases => new MultiStringParameter(phases, parameterPickerService.Value));
            
            eventAggregator.GetEvent<DatabaseCacheReloaded>().Subscribe(type =>
            {
                if (!reloadable.TryGetValue(type, out var list))
                    return;
                foreach (var parameter in list)
                    parameter.Reload();
            }, ThreadOption.UIThread, true);

            async Task OnDatabaseLoad()
            {
                foreach (var p in databaseParameters)
                {
                    p.LateLoad();
                    factory.Updated(p);
                }

                foreach (var p in asyncDatabaseParameters)
                {
                    await p.LateLoad();
                    factory.Updated(p);
                }
            }

            loadingEventAggregator.OnEvent<DatabaseLoadedEvent>().SubscribeAction(_ => OnDatabaseLoad().ListenErrors());
        }
    
        private T AddDatabaseParameter<T>(T t) where T : LateLoadParameter
        {
            if (t is IDatabaseObserver databaseObserver)
            {
                if (!reloadable.TryGetValue(databaseObserver.ObservedType, out var list))
                    list = reloadable[databaseObserver.ObservedType] = new();
                list.Add(databaseObserver);
            }
            databaseParameters.Add(t);
            return t;
        }
        
        private T AddAsyncDatabaseParameter<T>(T t) where T : LateAsyncLoadParameter
         {
            if (t is IDatabaseObserver databaseObserver)
            {
                if (!reloadable.TryGetValue(databaseObserver.ObservedType, out var list))
                    list = reloadable[databaseObserver.ObservedType] = new();
                list.Add(databaseObserver);
            }
            asyncDatabaseParameters.Add(t);
            return t;
        }
    }

    public class PercentageParameter : Parameter
    {
        public override string ToString(long key)
        {
            return key + "%";
        }
    }

    public class MinuteIntervalParameter : Parameter, IParameterFromString<long?>
    {
        public override string ToString(long minutes)
        {
            int years = (int) (minutes / 525600);
            minutes -= years * 525600;
            int months = (int) (minutes / 43200);
            minutes -= months * 43200;
            int days = (int) (minutes / 1440);
            minutes -= days * 1440;
            int hours = (int) (minutes / 60);
            minutes -= hours * 60;
            StringBuilder sb = new();
             if (years > 0)
                sb.Append(years).Append("y ");
             if (months > 0)
                sb.Append(months).Append("m ");
             if (days > 0)
                sb.Append(days).Append("d ");
             if (hours > 0)
                sb.Append(hours).Append("h ");
             if (minutes > 0)
                sb.Append(minutes).Append("min ");
            
            return sb.ToString();
        }

        public long? FromString(string value)
        {
            if (long.TryParse(value, out var val))
                return val;
            var parts = value.Split(' ');
            long minutes = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (!(part.Length > 1 && long.TryParse(part[..^1], out var num)) &&
                    !(part.Length > 3 && long.TryParse(part[..^3], out num)))
                    return null;
                if (part.EndsWith("y"))
                    minutes += num * 525600;
                else if (part.EndsWith("m"))
                    minutes += num * 43200;
                else if (part.EndsWith("d"))
                    minutes += num * 1440;
                else if (part.EndsWith("h"))
                    minutes += num * 60;
                else if (part.EndsWith("min"))
                    minutes += num;
                else
                    return null;
            }
            return minutes;
        }
    }
    
    public class MillisecondsParameter : Parameter, IParameterFromString<long?>
    {
        public override string ToString(long milliseconds)
        {
            int days = (int) (milliseconds / (1440 * 60 * 1000));
            milliseconds -= days * 1440 * 60 * 1000;
            int hours = (int) (milliseconds / (60 * 60 * 1000));
            milliseconds -= hours * 60 * 60 * 1000;
            int minutes = (int) (milliseconds / (60 * 1000));
            milliseconds -= minutes * 60 * 1000;
            int seconds = (int) (milliseconds / 1000);
            milliseconds -= seconds * 1000;
            StringBuilder sb = new();
             if (days > 0)
                sb.Append(days).Append("d ");
             if (hours > 0)
                sb.Append(hours).Append("h ");
             if (minutes > 0)
                sb.Append(minutes).Append("min ");
             if (milliseconds > 0 && (milliseconds % 100) == 0)
             {
                 float secs = seconds + milliseconds / 1000.0f;
                 sb.Append($"{secs:0.0}s");
             }
             else
             {
                 if (seconds > 0)
                     sb.Append(seconds).Append("s ");
                 if (milliseconds > 0)
                     sb.Append(milliseconds).Append("ms ");   
             }

             return sb.ToString();
        }

        public long? FromString(string value)
        {
            if (long.TryParse(value, out var val))
                return val;
            var parts = value.Split(' ');
            long milliseconds = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (!(part.Length > 1 && float.TryParse(part[..^1], out var num)) &&
                    !(part.Length > 3 && float.TryParse(part[..^3], out num)))
                    return null;
                if (part.EndsWith("d"))
                    milliseconds += (long)(num * 1440 * 60 * 1000);
                else if (part.EndsWith("h"))
                    milliseconds += (long)(num * 60 * 60 * 1000);
                else if (part.EndsWith("min"))
                    milliseconds += (long)(num * 60 * 1000);
                else if (part.EndsWith("s"))
                    milliseconds += (long)(num * 1000);
                else
                    return null;
            }
            return milliseconds;
        }
    }

    public class InvalidParameter<T> : IParameter<T> where T : notnull
    {
        public string? Prefix => null;
        public bool HasItems => false;
        public string ToString(T value) => "INVALID VALUE";
        public Dictionary<T, SelectOption>? Items => null;
    }

    public class MoneyParameter : Parameter, IParameterFromString<long?>
    {
        public override string ToString(long key)
        {
            int gold = (int) (key / 10000);
            int silver = (int) ((key % 10000) / 100);
            int copper = (int) (key % 100);
            if (gold > 0 && silver > 0 && copper > 0)
                return $"{gold}g {silver}s {copper}c";
            if (gold > 0 && silver > 0)
                return $"{gold}g {silver}s";
            if (gold > 0 && copper > 0)
                return $"{gold}g {copper}c";
            if (gold > 0)
                return $"{gold}g";
            if (silver > 0 && copper > 0)
                return $"{silver}s {copper}c";
            if (silver > 0)
                return $"{silver}s";
            return $"{copper}c";
        }

        public long? FromString(string value)
        {
            if (long.TryParse(value, out var val))
                return val;
            long total = 0;
            var parts = value.Split(' ');
            foreach (var part in parts)
            {
                if (part.Length < 2)
                    return null;
                var unit = part[^1];
                var amount = part.Substring(0, part.Length - 1);
                if (!long.TryParse(amount, out var amountLong))
                    return null;
                switch (unit)
                {
                    case 'g':
                        total += amountLong * 10000;
                        break;
                    case 's':
                        total += amountLong * 100;
                        break;
                    case 'c':
                        total += amountLong;
                        break;
                    default:
                        return null;
                }
            }

            return total;
        }
    }

    public class UnixTimestampParameter : Parameter, ICustomPickerParameter<long>
    {
        private readonly long startOffset;
        private readonly Lazy<IWindowManager> windowManager;

        public UnixTimestampParameter(long startOffset, Lazy<IWindowManager> windowManager)
        {
            this.startOffset = startOffset;
            this.windowManager = windowManager;
        }

        public override bool HasItems => true;

        private DateTimeOffset AsDateTime(long val) => DateTimeOffset.UnixEpoch.AddSeconds(startOffset + val);
        
        public override string ToString(long key)
        {
            if (key == 0) 
                return "0000-00-00 00:00:00";
            return AsDateTime(key).ToString("yyyy-MM-dd HH:mm:ss");
        }

        public async Task<(long, bool)> PickValue(long value)
        {
            using var dialog = new UnixTimestampEditorViewModel();
            dialog.DateTime = AsDateTime(value).UtcDateTime;
            dialog.MinDate = dialog.MinDate.AddSeconds(startOffset);
            dialog.MaxDate = dialog.MaxDate.AddSeconds(-startOffset);

            if (!await windowManager.Value.ShowDialog(dialog))
                return (0, false);

            return (((DateTimeOffset)dialog.DateTime).ToUnixTimeSeconds() - startOffset, true);
        }
    }

    public class BoolParameter : Parameter
    {
        public new static BoolParameter Instance { get; } = new BoolParameter();
        
        public BoolParameter()
        {
            Items = new Dictionary<long, SelectOption> {{0, new SelectOption("No")}, {1, new SelectOption("Yes")}};
        }
    }

    public abstract class LateLoadParameter : ParameterNumbered
    {
        public abstract void LateLoad();
    }
    
    public abstract class LateAsyncLoadParameter : ParameterNumbered
    {
        public abstract Task LateLoad();
    }

    public abstract class LazyLoadParameter : ParameterNumbered
    {
        public override bool HasItems
        {
            get
            {
                if (Items == null)
                    LazyLoad();
                return Items!.Count > 0;
            }
        }

        public override string ToString(long key)
        {
            if (Items == null)
                LazyLoad();
            return base.ToString(key);
        }

        protected abstract void LazyLoad();
    }

    internal interface IDatabaseObserver
    {
        Type ObservedType { get; }
        void Reload();
    }
    
    public class CreatureParameter : LateAsyncLoadParameter, ICustomPickerParameter<long>
    {
        private readonly IDatabaseProvider database;
        private readonly ICreatureEntryOrGuidProviderService picker;
        private readonly string? customCounterTable;

        public async Task<(long, bool)> PickValue(long value)
        {
            var result = await picker.GetEntryFromService((uint)value, customCounterTable);
            return (result ?? 0, result.HasValue);
        }

        public async Task<IReadOnlyCollection<long>> PickMultipleValues()
        {
            return (await picker.GetEntriesFromService()).Select(x => (long)x).ToList();
        }

        public override Func<Task<object?>>? SpecialCommand { get; }

        public CreatureParameter(IDatabaseProvider database, 
            ICreatureEntryOrGuidProviderService picker,
            IServerIntegration serverIntegration, 
            string? customCounterTable = null)
        {
            Items = new Dictionary<long, SelectOption>();
            this.database = database;
            this.picker = picker;
            this.customCounterTable = customCounterTable;
            SpecialCommand = async () =>
            {
                var entry = await serverIntegration.GetSelectedEntry();
                if (entry.HasValue)
                    return (long?)entry;
                return null;
            };
        }

        public override async Task LateLoad()
        {
            foreach (ICreatureTemplate item in await database.GetCreatureTemplatesAsync())
                Items!.Add(item.Entry, new SelectOption(item.Name));
        }
    }

    public class CreatureGameobjectNameParameter : LateAsyncLoadParameter
    {
        private readonly IDatabaseProvider database;

        public CreatureGameobjectNameParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGameObjectTemplate item in await database.GetGameObjectTemplatesAsync())
                Items[item.Entry] = new SelectOption(item.Name);
            foreach (ICreatureTemplate item in await database.GetCreatureTemplatesAsync())
                Items[item.Entry] = new SelectOption(item.Name);
        }
    }
    
    public class CreatureGameobjectParameter : LateAsyncLoadParameter
    {
        private readonly IDatabaseProvider database;

        public CreatureGameobjectParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (ICreatureTemplate item in await database.GetCreatureTemplatesAsync())
                Items[item.Entry] = new SelectOption(item.Name);
            foreach (IGameObjectTemplate item in await database.GetGameObjectTemplatesAsync())
                Items[-item.Entry] = new SelectOption(item.Name);
        }
    }

    public class SceneTemplateParameter : LateAsyncLoadParameter
    {
        private readonly IDatabaseProvider databaseProvider;
        private readonly IParameter<long> sceneScriptParameter;

        public SceneTemplateParameter(IDatabaseProvider databaseProvider,
            IParameter<long> sceneScriptParameter)
        {
            this.databaseProvider = databaseProvider;
            this.sceneScriptParameter = sceneScriptParameter;
        }
        
        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            if (await databaseProvider.GetSceneTemplatesAsync() is { } list)
            {
                foreach (ISceneTemplate item in list)
                {
                    if (sceneScriptParameter.Items?.TryGetValue(item.ScriptPackageId, out var name) ?? false)
                        Items.Add(item.SceneId, name);
                    else
                        Items.Add(item.SceneId, new SelectOption("Unknown scene template"));
                }
            }
        }
    }
    
    public class GossipMenuParameter : LateAsyncLoadParameter, IDatabaseObserver, IDynamicParameter<long>
    {
        private readonly IDatabaseProvider database;

        public GossipMenuParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGossipMenu item in await database.GetGossipMenusAsync())
            {
                var text = item.Text
                    .Select(t => t.Text0_0 ?? t.Text0_1 ?? "")
                    .FirstOrDefault();
                if (string.IsNullOrWhiteSpace(text) && item.Text.Select(t => t.BroadcastTextId).FirstOrDefault() is { } broadcastId and > 0)
                {
                    var broadcastText = await database.GetBroadcastTextByIdAsync(broadcastId);
                    if (broadcastText != null)
                    {
                        text = broadcastText.FirstText();
                    }
                }

                text ??= "";
                Items.Add(item.MenuId, new SelectOption(text
                    .Replace("\n", "")
                    .Truncate(100)));
            }
            ItemsChanged?.Invoke(this);
        }

        public Type ObservedType => typeof(IGossipMenu);
        public void Reload() => LateLoad().ListenErrors();

        public event Action<IParameter<long>>? ItemsChanged;
    }

    internal static class StringExtensions
    {
        public static string Truncate(this string str, int length)
        {
            if (str.Length >= length + 20)
                return str.Substring(0, length) + "...";
            return str;
        }
    }

    public class NpcTextParameter : LateAsyncLoadParameter, IDatabaseObserver
    {
        private readonly IDatabaseProvider database;

        public NpcTextParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (INpcText item in await database.GetNpcTextsAsync())
            {
                var text = string.IsNullOrEmpty(item.Text0_0) ? item.Text0_1 : item.Text0_0;
                if (string.IsNullOrEmpty(text) && item.BroadcastTextId > 0)
                {
                    var broadcastText = await database.GetBroadcastTextByIdAsync(item.BroadcastTextId);
                    if (broadcastText != null)
                        text = broadcastText.FirstText();
                }
                Items.Add(item.Id, new SelectOption(text?.Replace("\n", "\\n") ?? ""));
            }
        }

        public Type ObservedType => typeof(INpcText);
        
        public void Reload() => LateLoad().ListenErrors();
    }
    
    public class QuestParameter : LateAsyncLoadParameter, ICustomPickerParameter<long>, IDynamicParameter<long>
    {
        private readonly IDatabaseProvider database;
        private readonly IQuestEntryProviderService questEntryProvider;

        public QuestParameter(IDatabaseProvider database, IQuestEntryProviderService questEntryProvider)
        {
            this.database = database;
            this.questEntryProvider = questEntryProvider;
        }

        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IQuestTemplate item in await database.GetQuestTemplatesAsync())
                Items[item.Entry] = new SelectOption(item.Name ?? "Quest " + item.Entry);
            ItemsChanged?.Invoke(this);
        }

        public async Task<(long, bool)> PickValue(long value)
        {
            var result = await questEntryProvider.GetEntryFromService((uint)value);
            return (result ?? 0, result.HasValue);
        }

        public event Action<IParameter<long>>? ItemsChanged;
    }
    
    public class PrevQuestParameter : LateAsyncLoadParameter
    {
        private readonly IDatabaseProvider database;

        public PrevQuestParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IQuestTemplate item in await database.GetQuestTemplatesAsync())
            {
                Items[-item.Entry] = new SelectOption(item.Name, "quest must be active");
                Items[item.Entry] = new SelectOption(item.Name, "quest must be completed");
            }
        }
    }

    public class GameEventParameter : LateAsyncLoadParameter
    {
        private readonly IDatabaseProvider database;

        public GameEventParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGameEvent item in await database.GetGameEventsAsync())
                Items.Add(item.Entry, new SelectOption(item.Description ?? "(null)"));
        }
    }

    public class GameobjectParameter : LateAsyncLoadParameter
    {
        private readonly IDatabaseProvider database;
        
        public override Func<Task<object?>>? SpecialCommand { get; }

        public GameobjectParameter(IDatabaseProvider database,
            IServerIntegration serverIntegration,
            IItemFromListProvider itemFromListProvider)
        {
            this.database = database;
            SpecialCommand = async () =>
            {
                var entry = await serverIntegration.GetNearestGameObjects();
                if (entry == null || entry.Count == 0)
                    return null;
                
                Dictionary<long, SelectOption> options = new Dictionary<long, SelectOption>();
                foreach (var pair in entry.GroupBy(e => e.Entry)
                             .OrderBy(group => group.Key))
                {
                    options[(long)pair.Key] = new SelectOption((await database.GetGameObjectTemplate(pair.Key))?.Name ?? "Unknown name",$"{pair.First().Distance} yd away");
                }

                if (options.Count == 1)
                    return (long)options.Keys.First();

                var pick = await itemFromListProvider.GetItemFromList(options, false);

                if (pick.HasValue)
                    return (long)pick.Value;
                
                return null;
            };
        }

        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGameObjectTemplate item in await database.GetGameObjectTemplatesAsync())
                Items.Add(item.Entry, new SelectOption(item.Name));
        }
    }

    public class PlayerChoiceParameter : LateAsyncLoadParameter
    {
        private readonly IDatabaseProvider database;

        public PlayerChoiceParameter(IDatabaseProvider database)
        {
            this.database = database;
        }
        
        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            var choices = await database.GetPlayerChoicesAsync();
            if (choices != null)
                foreach (var item in choices)
                    Items.Add(item.ChoiceId, new SelectOption(item.Question));
        }
    }

    public class PlayerChoiceResponseParameter : LateAsyncLoadParameter
    {
        private readonly IDatabaseProvider database;

        public PlayerChoiceResponseParameter(IDatabaseProvider database)
        {
            this.database = database;
        }
        
        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            var choices = await database.GetPlayerChoiceResponsesAsync();
            if (choices != null)
                foreach (var item in choices)
                    Items.Add(item.ResponseId, new SelectOption(string.IsNullOrEmpty(item.Header) ? item.Answer : item.Header));
        }
    }
    
    public class ServersideAreatriggerParameter : LateAsyncLoadParameter, IDatabaseObserver
    {
        private readonly IDatabaseProvider database;

        public ServersideAreatriggerParameter(IDatabaseProvider database)
        {
            this.database = database;
        }
        
        public override async Task LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            var templates = await database.GetAreaTriggerTemplatesAsync();
            foreach (var template in templates)
                Items.Add(template.Id, new SelectOption(template.Name ?? $"Serverside areatrigger {template.Id}"));
        }

        public Type ObservedType => typeof(IAreaTriggerTemplate);
        
        public void Reload()
        {
            LateLoad().ListenErrors();
        }
    }
    
    public class DatabasePhaseParameter : LateAsyncLoadParameter, IDatabaseObserver, IDynamicParameter<long>
    {
        private readonly IDatabaseProvider database;

        public DatabasePhaseParameter(IDatabaseProvider database)
        {
            Items = new Dictionary<long, SelectOption>();
            this.database = database;
        }
        
        public override async Task LateLoad()
        {
            var phases = await database.GetPhaseNamesAsync();
            Items!.Clear();
            if (phases != null)
                foreach (var phase in phases)
                    Items.Add(phase.Id, new SelectOption(phase.Name));
            ItemsChanged?.Invoke(this);
        }

        public Type ObservedType => typeof(IPhaseName);
        
        public void Reload()
        {
            // doing it synchronously is not the best idea
            // but some things might depend on the fact that the name will be right after
            // the Reload() method is invoked, so I couldn't make it async.
            var phases = database.GetPhaseNames();
            Items!.Clear();
            if (phases != null)
                foreach (var phase in phases)
                    Items.Add(phase.Id, new SelectOption(phase.Name));
            ItemsChanged?.Invoke(this);
        }

        public event Action<IParameter<long>>? ItemsChanged;
    }

    public class PhaseParameter : Parameter, IDynamicParameter<long>
    {
        private readonly IParameter<long> dbc;
        private readonly IParameter<long> db;
        
        public PhaseParameter(IParameter<long> dbc, IParameter<long> db)
        {
            Items = new Dictionary<long, SelectOption>();
            this.dbc = dbc;
            this.db = db;
            if (db is IDynamicParameter<long> dyn)
                dyn.ItemsChanged += Reload;
            Reload();
        }

        private void Reload()
        {
            Items!.Clear();
            if (dbc.Items != null)
                foreach (var item in dbc.Items)
                    Items.Add(item.Key, item.Value);
            if (db.Items != null)
                foreach (var item in db.Items)
                    Items[item.Key] = item.Value;
            ItemsChanged?.Invoke(this);
        }

        private void Reload(IParameter<long> obj)
        {
            Reload();
        }
        
        public override string ToString(long value, ToStringOptions options)
        {          
            if (dbc.Items != null && dbc.Items.TryGetValue(value, out var _))
                return dbc.ToString(value, options);
            return db.ToString(value, options);
        }

        public override string ToString(long value)
        {
            if (dbc.Items != null && dbc.Items.TryGetValue(value, out var _))
                return dbc.ToString(value);
            return db.ToString(value);
        }

        public event Action<IParameter<long>>? ItemsChanged;
    }

    public class ConversationParameter : LateAsyncLoadParameter
    {
        private readonly IConversationLineStore conversationLineStore;
        private readonly ICachedDatabaseProvider databaseProvider;

        public ConversationParameter(IConversationLineStore conversationLineStore,
            ICachedDatabaseProvider databaseProvider)
        {
            this.conversationLineStore = conversationLineStore;
            this.databaseProvider = databaseProvider;
        }

        private async Task<string?> FirsNonEmptyLine(uint lineId)
        {
            while (lineId != 0)
            {
                if (conversationLineStore.GetConversationLineById(lineId) is not { } line)
                    return null;

                if (line.BroadcastTextId > 0)
                {
                    var broadcastText = await databaseProvider.GetBroadcastTextByIdAsync(line.BroadcastTextId);
                    if (broadcastText != null)
                        return broadcastText.FirstText();
                }

                lineId = line.NextLineId;
            }

            return null;
        }

        // todo: it should fire only if conversationLineStore is loaded
        public override async Task LateLoad()
        {
            var conversationTemplates = await databaseProvider.GetConversationTemplatesAsync();
            var actorTemplates = (await databaseProvider.GetConversationActorTemplates())
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.ToList());
            var actors = (await databaseProvider.GetConversationActors())
                .GroupBy(x => x.ConversationId)
                .ToDictionary(x => x.Key, x => x.ToList());

            Items = new Dictionary<long, SelectOption>();
            var sb = new StringBuilder();
            foreach (var conversation in conversationTemplates)
            {
                sb.Clear();
                if (actors.TryGetValue(conversation.Id, out var actorList))
                {
                    sb.Append("Actors<");
                    bool needsComma = false;
                    for (var index = 0; index < actorList.Count; index++)
                    {
                        var actor = actorList[index];

                        actorTemplates.TryGetValue(actor.ConversationActorId, out var creatures);
                        uint creatureId = 0;
                        if (creatures != null && creatures.Count > 0)
                            creatureId = creatures[0].CreatureId;
                        if (actor.CreatureId != 0)
                            creatureId = (uint)Math.Abs(actor.CreatureId);

                        if (creatureId == 0)
                            continue;

                        if (needsComma)
                            sb.Append(", ");

                        var creatureName = databaseProvider.GetCachedCreatureTemplate(creatureId)?.Name ?? "(unknown)";
                        sb.Append(creatureName);
                        sb.Append(" (");
                        sb.Append(actor.ConversationActorId);
                        sb.Append(")");
                        needsComma = true;
                    }
                    sb.Append("> ");
                }

                var broadcastText = await FirsNonEmptyLine(conversation.FirstLineId);

                if (!string.IsNullOrEmpty(broadcastText))
                    sb.Append(broadcastText);
                else
                    sb.Append($"(first line {conversation.FirstLineId})");

                Items.Add(conversation.Id, new SelectOption(sb.ToString()));
            }
        }

        public override string ToString(long key, ToStringOptions options)
        {
            if (options.withNumber)
                return ToString(key);

            if (Items != null && Items.TryGetValue(key, out var option))
                return option.Name;
            return key.ToString();
        }

        public override string ToString(long key)
        {
            if (Items != null && Items.TryGetValue(key, out var option))
                return $"({key}) {option.Name}";
            return key.ToString();
        }
    }
}
