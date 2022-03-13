using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;
using WDE.Parameters.Parameters;
using WDE.Parameters.Providers;
using WDE.Parameters.QuickAccess;

namespace WDE.Parameters
{
    public class ParameterLoader
    {
        private readonly IDatabaseProvider database;
        private readonly IParameterDefinitionProvider parameterDefinitionProvider;
        private readonly IServerIntegration serverIntegration;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly ILoadingEventAggregator loadingEventAggregator;
        private readonly IQuickAccessRegisteredParameters quickAccessRegisteredParameters;

        private Dictionary<Type, List<IDatabaseObserver>> reloadable = new();
        private List<LateLoadParameter> databaseParameters = new();
        internal ParameterLoader(IDatabaseProvider database, 
            IParameterDefinitionProvider parameterDefinitionProvider,
            IServerIntegration serverIntegration,
            IItemFromListProvider itemFromListProvider,
            IEventAggregator eventAggregator,
            ILoadingEventAggregator loadingEventAggregator,
            IQuickAccessRegisteredParameters quickAccessRegisteredParameters)
        {
            this.database = database;
            this.parameterDefinitionProvider = parameterDefinitionProvider;
            this.serverIntegration = serverIntegration;
            this.itemFromListProvider = itemFromListProvider;
            this.eventAggregator = eventAggregator;
            this.loadingEventAggregator = loadingEventAggregator;
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
                
                if (pair.Value.QuickAccess != QuickAccessMode.None)
                    quickAccessRegisteredParameters.Register(pair.Value.QuickAccess, pair.Key, pair.Value.Name);
            }

            factory.Register("FloatParameter", new FloatIntParameter(1000));
            factory.Register("DecifloatParameter", new FloatIntParameter(100));
            factory.Register("GameEventParameter", AddDatabaseParameter(new GameEventParameter(database)), QuickAccessMode.Limited);
            factory.Register("CreatureParameter", AddDatabaseParameter(new CreatureParameter(database, serverIntegration)), QuickAccessMode.Limited);
            factory.Register("CreatureGameobjectNameParameter", AddDatabaseParameter(new CreatureGameobjectNameParameter(database)));
            factory.Register("CreatureGameobjectParameter", AddDatabaseParameter(new CreatureGameobjectParameter(database)));
            factory.Register("QuestParameter", AddDatabaseParameter(new QuestParameter(database)), QuickAccessMode.Limited);
            factory.Register("PrevQuestParameter", AddDatabaseParameter(new PrevQuestParameter(database)));
            factory.Register("GameobjectParameter", AddDatabaseParameter(new GameobjectParameter(database, serverIntegration, itemFromListProvider)), QuickAccessMode.Limited);
            factory.Register("GossipMenuParameter", AddDatabaseParameter(new GossipMenuParameter(database)));
            factory.Register("NpcTextParameter", AddDatabaseParameter(new NpcTextParameter(database)));
            factory.Register("ConversationTemplateParameter", new ConversationTemplateParameter(database));
            factory.Register("BoolParameter", new BoolParameter());
            factory.Register("FlagParameter", new FlagParameter());
            factory.Register("PercentageParameter", new PercentageParameter());
            factory.Register("MoneyParameter", new MoneyParameter());
            factory.Register("MinuteIntervalParameter", new MinuteIntervalParameter());
            factory.Register("UnixTimestampParameter", new UnixTimestampParameter(0));
            factory.Register("UnixTimestampSince2000Parameter", new UnixTimestampParameter(946681200));
            factory.Register("GameobjectBytes1Parameter", new GameObjectBytes1Parameter());
            factory.RegisterCombined("UnitBytes0Parameter", "RaceParameter",  "ClassParameter","GenderParameter", "PowerParameter", 
                (race, @class, gender, power) => new UnitBytesParameter(race, @class, gender, power));
            factory.RegisterCombined("UnitBytes1Parameter", "StandStateParameter", "AnimTierParameter", (standState, animTier) => new UnitBytes1Parameter(standState, animTier));
            factory.RegisterCombined("UnitBytes2Parameter", "SheathStateParameter",  "UnitPVPStateFlagParameter","UnitBytesPetFlagParameter", "ShapeshiftFormParameter", 
                (sheath, pvp, pet, shapeShift) => new UnitBytes2Parameter(sheath, pvp, pet, shapeShift));
            
            eventAggregator.GetEvent<DatabaseCacheReloaded>().Subscribe(type =>
            {
                if (!reloadable.TryGetValue(type, out var list))
                    return;
                foreach (var parameter in list)
                    parameter.Reload();
            }, ThreadOption.UIThread, true);
            
            loadingEventAggregator.OnEvent<DatabaseLoadedEvent>().SubscribeAction(_ =>
            {
                foreach (var p in databaseParameters)
                {
                    p.LateLoad();
                    factory.Updated(p);
                }
            });
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
    }

    public class PercentageParameter : Parameter
    {
        public override string ToString(long key)
        {
            return key + "%";
        }
    }

    public class MinuteIntervalParameter : Parameter
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
    }

    public class MoneyParameter : Parameter
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
    }

    public class UnixTimestampParameter : Parameter
    {
        private readonly long startOffset;

        public UnixTimestampParameter(long startOffset)
        {
            this.startOffset = startOffset;
        }
        
        public override string ToString(long key)
        {
            return DateTime.UnixEpoch.AddSeconds(startOffset + key).ToString("yyyy-MM-dd HH:mm:ss");
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
    
    public class CreatureParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;
        
        public override Func<Task<object?>>? SpecialCommand { get; }

        public CreatureParameter(IDatabaseProvider database, IServerIntegration serverIntegration)
        {
            Items = new Dictionary<long, SelectOption>();
            this.database = database;
            SpecialCommand = async () =>
            {
                var entry = await serverIntegration.GetSelectedEntry();
                if (entry.HasValue)
                    return entry;
                return null;
            };
        }

        public override void LateLoad()
        {
            foreach (ICreatureTemplate item in database.GetCreatureTemplates())
                Items!.Add(item.Entry, new SelectOption(item.Name));
        }
    }

    public class CreatureGameobjectNameParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public CreatureGameobjectNameParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGameObjectTemplate item in database.GetGameObjectTemplates())
                Items[item.Entry] = new SelectOption(item.Name);
            foreach (ICreatureTemplate item in database.GetCreatureTemplates())
                Items[item.Entry] = new SelectOption(item.Name);
        }
    }
    
    public class CreatureGameobjectParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public CreatureGameobjectParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (ICreatureTemplate item in database.GetCreatureTemplates())
                Items.Add(item.Entry, new SelectOption(item.Name));
            foreach (IGameObjectTemplate item in database.GetGameObjectTemplates())
                Items.Add(-item.Entry, new SelectOption(item.Name));
        }
    }
    
    public class GossipMenuParameter : LateLoadParameter, IDatabaseObserver
    {
        private readonly IDatabaseProvider database;

        public GossipMenuParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGossipMenu item in database.GetGossipMenus())
                Items.Add(item.MenuId, new SelectOption((item.Text
                        .Select(t => t.Text0_0 ?? t.Text0_1 ?? "")
                        .FirstOrDefault() ?? "")
                        .Replace("\n", "")
                        .Truncate(100)));
        }

        public Type ObservedType => typeof(IGossipMenu);
        public void Reload() => LateLoad();
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

    public class NpcTextParameter : LateLoadParameter, IDatabaseObserver
    {
        private readonly IDatabaseProvider database;

        public NpcTextParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (INpcText item in database.GetNpcTexts())
                Items.Add(item.Id, new SelectOption(item.Text0_0 ?? item.Text0_1 ?? ""));
        }

        public Type ObservedType => typeof(INpcText);
        
        public void Reload() => LateLoad();
    }
    
    public class QuestParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public QuestParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IQuestTemplate item in database.GetQuestTemplates())
                Items.Add(item.Entry, new SelectOption(item.Name));
        }
    }
    
    public class PrevQuestParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public PrevQuestParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IQuestTemplate item in database.GetQuestTemplates())
            {
                Items.Add(-item.Entry, new SelectOption(item.Name, "quest must be active"));
                Items.Add(item.Entry, new SelectOption(item.Name, "quest must be completed"));
            }
        }
    }

    public class GameEventParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public GameEventParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGameEvent item in database.GetGameEvents())
                Items.Add(item.Entry, new SelectOption(item.Description ?? "(null)"));
        }
    }

    public class GameobjectParameter : LateLoadParameter
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
                
                var options = entry.GroupBy(e => e.Entry)
                    .OrderBy(group => group.Key)
                    .ToDictionary(g => (long)g.Key,
                        g => new SelectOption(database.GetGameObjectTemplate(g.Key)?.Name ?? "Unknown name",
                            $"{g.First().Distance} yd away"));

                if (options.Count == 1)
                    return (uint)options.Keys.First();

                var pick = await itemFromListProvider.GetItemFromList(options, false);

                if (pick.HasValue)
                    return (uint)pick.Value;
                
                return null;
            };
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGameObjectTemplate item in database.GetGameObjectTemplates())
                Items.Add(item.Entry, new SelectOption(item.Name));
        }
    }
    
    
    public class ConversationTemplateParameter : LazyLoadParameter
    {
        private readonly IDatabaseProvider database;

        public ConversationTemplateParameter(IDatabaseProvider database)
        {
            this.database = database;
        }
        
        protected override void LazyLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IConversationTemplate item in database.GetConversationTemplates())
            {
                var name = $"conversation with first line id: {item.FirstLineId}";
                if (!string.IsNullOrEmpty(item.ScriptName))
                    name += $", script name: {item.ScriptName}";
                Items.Add(item.Id, new SelectOption(name));
            }
        }
    }
    
}